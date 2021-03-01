﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Silk.Core.Services.Interfaces;
using Silk.Core.Utilities;
using Silk.Data;
using Silk.Data.MediatR;
using Silk.Data.Models;

namespace Silk.Core.Services
{
    /// <inheritdoc cref="IInfractionService"/>
    public sealed class InfractionService : IInfractionService
    {
        private readonly ILogger<InfractionService> _logger;
        private readonly DiscordShardedClient _client;
        private readonly IMediator _mediator;
        private readonly SilkDbContext _db;
        
        private readonly List<Infraction> _tempInfractions = new();

        public InfractionService(ILogger<InfractionService> logger, DiscordShardedClient client, IMediator mediator, SilkDbContext db)
        {
            _logger = logger;
            _client = client;
            _mediator = mediator;
            _db = db;
            Timer timer = new(TimeSpan.FromSeconds(30).TotalMilliseconds);
            timer.Elapsed += async (_, _) => await OnTick();
            timer.Start();
        }

        public async Task KickAsync(DiscordMember member, DiscordChannel channel, Infraction infraction, DiscordEmbed embed)
        {
            // Validation is handled by the command class. //
            _ = member.RemoveAsync(infraction.Reason);
            GuildConfig config = await _mediator.Send(new GuildConfigRequest.Get(member.Guild.Id));

            if (config.LoggingChannel is 0)
                _logger.LogTrace($"No available log channel for guild! | {member.Guild.Id}");
            else
                _ = channel.Guild.Channels[config.LoggingChannel].SendMessageAsync(embed);

            _ = channel.SendMessageAsync($":boot: Kicked **{member.Username}#{member.Discriminator}**!");
        }

        public async Task BanAsync(DiscordMember member, DiscordChannel channel, Infraction infraction)
        {
            await member.BanAsync(0, infraction.Reason);
            GuildConfig config = await _mediator.Send(new GuildConfigRequest.Get(member.Guild.Id));
            await ApplyInfractionAsync(infraction.Guild, infraction.User, infraction);
            if (config.LoggingChannel is 0)
            {
                _logger.LogWarning($"No available logging channel for guild! | {member.Guild.Id}");
                
            }
            else
            {
                Guild guild = await _mediator.Send(new GuildRequest.Get(member.Guild.Id));
                int infractions = guild.Infractions.Count + 1;

                DiscordEmbedBuilder embed = EmbedHelper.CreateEmbed($"Case #{infractions} | User {member.Username}",
                        $"{member.Mention} was banned from the server by for ```{infraction.Reason}```", DiscordColor.IndianRed)
                    .WithFooter($"Staff member: {infraction.Enforcer}");

                await channel.SendMessageAsync($":hammer: Banned **{member.Username}#{member.Discriminator}**!");
                await channel.Guild.Channels[config.LoggingChannel].SendMessageAsync(embed);
            }
            await channel.SendMessageAsync($":hammer: Banned **{member.Username}#{member.Discriminator}**!");
        }

        public async Task TempBanAsync(DiscordMember member, DiscordChannel channel, Infraction infraction) { }

        public async Task MuteAsync(DiscordMember member, DiscordChannel channel, Infraction infraction)
        {
            GuildConfig config = await _mediator.Send(new GuildConfigRequest.Get(member.Guild.Id));

            if (!channel.Guild.Roles.TryGetValue(config.MuteRoleId, out DiscordRole? muteRole))
            {
                await channel.SendMessageAsync("Mute role doesn't exist on server!");
                return;
            }
            
            User user = await _mediator.Send(new UserRequest.GetOrCreate(member.Guild.Id, member.Id));
            Guild guild = await _mediator.Send(new GuildRequest.Get(member.Guild.Id));
            if (user.Flags.HasFlag(UserFlag.ActivelyMuted))
            {
                Infraction? inf = user.Guild
                    .Infractions
                    .LastOrDefault(i => 
                        i.HeldAgainstUser &&
                        i.InfractionType is (InfractionType.AutoModMute or InfractionType.Mute) /* &&
                        i.Expiration > DateTime.Now*/ /* I don't think this is needed. */);
                if (inf is null) return;
                inf.Expiration = infraction.Expiration;
                await _mediator.Send(new GuildRequest.Update(guild.Id) { Infractions = user.Guild.Infractions });
                _logger.LogTrace($"Updated mute for {member.Id}!");
                return;
            }

            if (!member.Roles.Contains(muteRole))
                await member.GrantRoleAsync(muteRole);
            
            await ApplyInfractionAsync(guild, user, infraction);

            if (infraction.Expiration is not null)
            {
                if (!_tempInfractions.Contains(infraction))
                {
                    _tempInfractions.Add(infraction);
                    _logger.LogTrace($"Added temporary infraction to {member.Id}!");
                }
                else
                {
                    var inf = _tempInfractions.Single(m => m.InfractionType is InfractionType.Mute && m.UserId == member.Id);
                    var index = _tempInfractions.IndexOf(inf);
                    _tempInfractions[index] = infraction;
                }
            }
            else
            {
                _logger.LogTrace($"Applied indefinite mute to {member.Id}!");
            }
            
        }

        /// <summary>
        /// Creates an infraction that's marked as temporary. Only <see cref="InfractionType.SoftBan"/> and <see cref="InfractionType.Mute"/> can be passed.
        /// </summary>
        /// <returns>The infraction that was created.</returns>
        /// <remarks>
        ///     <para>
        ///     Remarks: Temporary infractions are not passed into the infraction queue, and it is up to
        ///     the delegated methods that handle infractions to add them to the queue.
        ///     </para>
        /// </remarks>
        public async Task<Infraction> CreateInfractionAsync(DiscordMember member, DiscordMember enforcer, InfractionType type, string reason = "Not given.")
        {
            //Ensure the user will exist in the DB so we don't hit FK violations
            _ = await _mediator.Send(new UserRequest.GetOrCreate(member.Guild.Id, member.Id));
            
            Infraction infraction = new()
            {
                Enforcer = enforcer.Id,
                Reason = reason,
                InfractionTime = DateTime.Now,
                UserId = member.Id,
                InfractionType = type,
            };
            
            return infraction;
        }

        public async Task<Infraction> CreateTemporaryInfractionAsync(DiscordMember member, DiscordMember enforcer, InfractionType type, string reason = "Not given.", DateTime? expiration = null)
        {
            if (type is not (InfractionType.SoftBan or InfractionType.Mute))
                throw new ArgumentException("Is not a temporary infraction type!", nameof(type));

            Infraction infraction = await CreateInfractionAsync(member, enforcer, type, reason);
            infraction.Expiration = expiration;
            return infraction;
        }

        public async Task<bool> ShouldAddInfractionAsync(DiscordMember member)
        {
            User? user = await _mediator.Send(new UserRequest.Get(member.Guild.Id, member.Id));
            return !user?.Flags.HasFlag(UserFlag.InfractionExemption) ?? true;
        }

        public async Task<bool> HasActiveMuteAsync(DiscordMember member)
        {
            User? user = await _mediator.Send(new UserRequest.Get(member.Guild.Id, member.Id));
            return user?.Flags.HasFlag(UserFlag.ActivelyMuted) ?? false;
        }

        private async Task ProcessSoftBanAsync(DiscordGuild guild, GuildConfig config, Infraction inf)
        {
            await guild.UnbanMemberAsync(inf.UserId);
            _logger.LogTrace($"Unbanned {inf.UserId} | SoftBan expired.");
            if (config.LoggingChannel is 0)
            {
                _logger.LogTrace($"Logging channel not configured for {guild.Id}.");
                return;
            }

            DiscordEmbed embed = EmbedHelper.CreateEmbed("Ban expired!", $"<@{inf.UserId}>'s ban has expired.", DiscordColor.Goldenrod);
            await guild.Channels[config.LoggingChannel].SendMessageAsync(embed);
        }

        private async Task ProcessTempMuteAsync(DiscordGuild guild, GuildConfig config, Infraction inf)
        {
            if (!guild.Members.TryGetValue(inf.UserId, out DiscordMember? mutedMember))
            {
                _logger.LogTrace("Cannot unmute member outside of guild!");
                //Infractions are removed outside of these methods.
            }
            else
            {
                await mutedMember.RevokeRoleAsync(guild.Roles[config.MuteRoleId], "Temporary mute expired.");
                inf.HeldAgainstUser = false;
                _logger.LogTrace($"Unmuted {inf.UserId} | TempMute expired.");
            }
        }

        private async Task ApplyInfractionAsync(Guild guild, User user, Infraction infraction)
        {
            guild.Infractions.Add(infraction);
            user.Flags = infraction.InfractionType switch
            {
                InfractionType.AutoModMute or InfractionType.Mute => user.Flags | UserFlag.ActivelyMuted,
                InfractionType.Ban => user.Flags | UserFlag.BannedPrior,
                InfractionType.Kick => user.Flags | UserFlag.KickedPrior,
                _ => user.Flags
            };
            await _mediator.Send(new UserRequest.Update(guild.Id, user.Id, user.Flags));
        }

        private async Task OnTick()
        {
            if (_tempInfractions.Count is 0) return;
            var infractions = _tempInfractions
                .Where(i => ((DateTime) i.Expiration!).Subtract(DateTime.Now).Seconds < 0)
                .GroupBy(x => x.User.Guild.Id);
            
            foreach (var inf in infractions)
            {
                _logger.LogTrace($"Processing infraction in guild {inf.Key}");
                DiscordGuild? guild = _client.ShardClients.Values.SelectMany(s => s.Guilds.Values).FirstOrDefault(g => g.Id == inf.Key);

                if (guild is null)
                {
                    _logger.LogWarning($"Guild was removed from cache! Dropping infractions from guild {inf.Key}");
                    DropInfractions(inf.Key);
                    return;
                }
                
                GuildConfig config = await _mediator.Send(new GuildConfigRequest.Get(inf.Key));
                
                foreach (Infraction infraction in inf)
                {
                    _logger.LogTrace($"Infraction {infraction.Id} | User {infraction.UserId}");
                    Task task = infraction.InfractionType switch
                    {
                        InfractionType.SoftBan => ProcessSoftBanAsync(guild!, config, infraction),
                        InfractionType.AutoModMute or InfractionType.Mute => ProcessTempMuteAsync(guild!, config, infraction),
                        _ => throw new ArgumentException("Type is not temporary infraction!")
                    };
                    await task;
                    _tempInfractions.Remove(infraction);
                }
            }
        }
        private void DropInfractions(ulong guildId)
        {
            _tempInfractions.RemoveAll(i => i.User.GuildId == guildId);
        }

        private async Task LoadInfractionsAsync()
        {
            IEnumerable<Infraction> infractions = new List<Infraction>();
            foreach (var guild in _db.Guilds)
            {
                infractions = infractions
                    .Union(guild
                        .Infractions
                        .Where(g => 
                            g.InfractionType is 
                                InfractionType.SoftBan or 
                                InfractionType.Mute or 
                                InfractionType.AutoModMute &&
                            g.HeldAgainstUser && 
                            (g.Expiration is null || g.Expiration > DateTime.Now)
                            ));
            }
            
        }

        /// <summary>
        /// Used to perform the correct step depending on guild settings.
        /// </summary>
        /// <param name="member">The member in question</param>
        /// <param name="infraction">The infraction object to be attached to the member</param>
        public async Task ProgressInfractionStepAsync(DiscordMember member, Infraction infraction) => throw new NotImplementedException();

    }
}