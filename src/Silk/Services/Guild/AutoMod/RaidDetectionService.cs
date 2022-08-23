using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;
using Silk.Services.Data;
using Silk.Services.Interfaces;
using Silk.Shared.Types.Collections;

namespace Silk.Services.Guild;

public class RaidDetectionService : BackgroundService
{
    private readonly IInfractionService      _infractions;
    private readonly IDiscordRestUserAPI     _users;
    private readonly GuildConfigCacheService _config;
    
    private record Raider(Snowflake GuildID, Snowflake RaiderID, string  Reason);
    
    private record MessageDTO(Snowflake GuildID, Snowflake ChannelID, Snowflake MessageID, Snowflake AuthorID, int HashCode)
    {
        public bool IsFlagged { get; set; }
    }
    
    private readonly Channel<Raider> _raiders = Channel.CreateUnbounded<Raider>();
    
    private readonly ConcurrentDictionary<Snowflake, List<JoinRange>>                      _joinRanges     = new();
    private readonly ConcurrentDictionary<Snowflake, List<MessageDTO>>                     _messageBuckets = new();
    private readonly ConcurrentDictionary<Snowflake, (DateTimeOffset LastJoin, int Count)> _joins          = new();

    
    public RaidDetectionService(IInfractionService infractions, IDiscordRestUserAPI users, GuildConfigCacheService config)
    {
        _infractions = infractions;
        _users       = users;
        _config      = config;
    }

    public async Task<Result> HandleMessageAsync(Snowflake guildID, Snowflake channelID, Snowflake messageID, Snowflake authorID, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result.FromSuccess();
        
        TrimMessageBuckets(); // Tidy up our buckets before we check if the server even has raid detection enabled.

        var config = await _config.GetConfigAsync(guildID);
        
        if (!config.EnableRaidDetection)
            return Result.FromSuccess();
        
        var bucket = _messageBuckets.GetOrAdd(channelID, _ => new());
        
        bucket.Add(new MessageDTO(guildID, channelID, messageID, authorID, content.GetHashCode()));
        
        var groups = bucket.GroupBy(x => x.HashCode);

        foreach (var group in groups)
        {
            if (group.DistinctBy(g => g.AuthorID).Count() < 2)
                continue; // Let anti-spam handle this; it's just one person spamming the same thing.
            
            if (group.Count() < config.RaidDetectionThreshold)
                continue;

            if (DateTimeOffset.UtcNow - group.Last().MessageID.Timestamp > TimeSpan.FromSeconds(config.RaidCooldownSeconds))
                continue; // Given up? Or just not a raid. This is an abitrary decision.
            
            foreach (var raider in group)
            {
                // Instead of removing buckets, only flag the messages so it doesn't reset the state since the raid might be continuing after this message
                if (!raider.IsFlagged)
                {
                    raider.IsFlagged = true;
                    await _raiders.Writer.WriteAsync(new Raider(raider.GuildID, raider.AuthorID, "Suspected raid: Coordinated message raid detected."));
                }
            }
        }
        
        return Result.FromSuccess();
    }

    public async Task<Result> HandleJoinAsync(Snowflake guildID, Snowflake userID, DateTimeOffset joinedAt, bool hasAvatar)
    {
        TrimBuckets(guildID);

        var config = await _config.GetConfigAsync(guildID);
        
        if (!config.EnableRaidDetection)
            return Result.FromSuccess();

        var joinCounter = _joins.GetOrAdd(guildID, _ => new());

        if (DateTimeOffset.UtcNow - joinCounter.LastJoin > TimeSpan.FromSeconds(config.RaidCooldownSeconds))
        {
            joinCounter.Count = 0;
        }
        
        if (joinCounter.Count++ >= config.RaidDetectionThreshold)
        {
            await _raiders.Writer.WriteAsync(new Raider(guildID, userID, "Raid Detection: Join velocity check exceeds threshold."));

            return Result.FromSuccess();
        }
        
        joinCounter.LastJoin = joinedAt;
        _joins[guildID] = joinCounter;
        
        // Join velocity check failed. Check for chunked raid activity.
        var joinRanges = _joinRanges.GetOrAdd(guildID, new List<JoinRange>());

        var created = userID.Timestamp;
        
        var range = joinRanges.FirstOrDefault(r => r.InRange(created));

        if (range is null)
        {
            range = new(created.AddHours(-2), created.AddHours(.5), config.RaidDetectionThreshold);
            joinRanges.Add(range);
        }

        if (range.IsSuspect(created))
        {
            await _raiders.Writer.WriteAsync(new Raider(guildID, userID, "Raid Detection: Join cluster check failed."));
            return Result.FromSuccess();
        }
        
        //TODO: Check for avatar-less users & calculate risk factor based on account date
        
        return Result.FromSuccess();
    }

    private void TrimBuckets(Snowflake guildID)
    {
        if (!_joinRanges.TryGetValue(guildID, out var ranges))
            return;

        var expiration = DateTimeOffset.UtcNow.AddMinutes(-2);

        for (var i = ranges.Count - 1; i >= 0; i--)
        {
            var range = ranges[i];
            if (range.Added > expiration)
                continue;

            ranges.Remove(range);
        }
    }

    private void TrimMessageBuckets()
    {
        // Modifying concurrent collections while using foreach on them is allowed.
        foreach (var bucket in _messageBuckets)
        {
            if (!bucket.Value?.Any() ?? true)
                continue; // Skip empty buckets, nothing to remove.

            var expiration = DateTimeOffset.UtcNow.AddSeconds(-30);

            if (bucket.Value.Last().MessageID.Timestamp < expiration)
            {
                _messageBuckets.TryRemove(bucket.Key, out _);
                continue;
            }
            
            for (var i = bucket.Value.Count - 1; i >= 0; i--)
            {
                var message = bucket.Value[i];
                if (message?.MessageID.Timestamp > expiration)
                    continue;

                bucket.Value.Remove(message);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var self = (await _users.GetCurrentUserAsync(stoppingToken)).Entity;

        try
        {
            await foreach (var raider in _raiders.Reader.ReadAllAsync(stoppingToken))
            {
                // We don't *particularly* care to await this, as this slows down the entire loop if we do once we
                // start hitting ratelimits.
                _ = _infractions.BanAsync(raider.GuildID, raider.RaiderID, self.ID, 1, raider.Reason, null, false);
            }
        }
        finally
        {
            _raiders.Writer.Complete();
        }
    }
}

public sealed class JoinRange
{
    private int _count = 1;
    
    private readonly int            _max;
    private readonly DateTimeOffset _from;
    private readonly DateTimeOffset _to;
    
    public DateTimeOffset Added { get; }
    
    public JoinRange(DateTimeOffset from, DateTimeOffset to, int max)
    {
        _from = from;
        _to = to;
        _max = max;
        
        Added = DateTimeOffset.UtcNow;
    }

    public bool InRange(DateTimeOffset join) => join >= _from && join <= _to;

    public bool IsSuspect(DateTimeOffset time)
    {
        var suspicious = InRange(time);
        
        if (suspicious)
        {
            _count++;
        }

        return _count > _max;
    }
}