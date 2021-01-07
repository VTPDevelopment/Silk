﻿using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Silk.Core.Database;
using Silk.Core.Utilities;

namespace Silk.Core.Commands.General
{
    [Category(Categories.General)]
    public class PingCommand : BaseCommandModule
    {
        private readonly IDbContextFactory<SilkDbContext> _dbFactory;

        public PingCommand(IDbContextFactory<SilkDbContext> dbFactory) => _dbFactory = dbFactory;

        [Command("ping")]
        public async Task Ping(CommandContext ctx)
        {
            Core.Bot.CommandTimer.Stop();
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Blue);
            
            var sw = Stopwatch.StartNew();
            DiscordMessage message = await ctx.RespondAsync(embed: embed);
            sw.Stop();
            
            await Task.Delay(100);
            var silkAPIResponse = await new Ping().SendPingAsync("velvetthepanda.dev");
            embed
                .AddField("→ Message Latency ←", "```cs\n" + $"{sw.ElapsedMilliseconds} ms".PadLeft(10, '⠀') + "```",
                    true)
                .AddField("→ Discord API latency ←", "```cs\n" + $"{ctx.Client.Ping} ms".PadLeft(10, '⠀') + "```", true)
                .AddField("→ Silk! API Latency ←",
                    "```cs\n" + $"{silkAPIResponse.RoundtripTime} ms".PadLeft(9, '⠀') + "```", true)
                // Make the databse latency centered. //
                .AddField("​", "​", true)
                .AddField("→ Database Latency ←", "```cs\n" + $"{GetDbLatency()} ms".PadLeft(10, '⠀') + "```", true)
                .AddField("​", "​", true)
                .WithFooter($"Silk! | Requested by {ctx.User.Id}", ctx.User.AvatarUrl);
            
            await message.ModifyAsync(embed: embed.Build());
        }

        private int GetDbLatency()
        {
            SilkDbContext db = _dbFactory.CreateDbContext();
            //_ = db.Guilds.First(_ => _.DiscordGuildId == guildId);
            db.Database.BeginTransaction();
            var sw = Stopwatch.StartNew();
            db.Database.ExecuteSqlRaw("SELECT first_value(\"Id\") over () FROM \"Guilds\"");
            sw.Stop();
            return (int) sw.ElapsedMilliseconds;
        }
    }
}