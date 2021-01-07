﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using Serilog;
using Silk.Core.Database.Models;
using Silk.Core.Services;
using Silk.Core.Services.Interfaces;

namespace Silk.Core.AutoMod
{
    public class AutoModMessageHandler
    {
        private static readonly RegexOptions flags = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase;

        /*
         * To those unacquainted to Regex, or simply too lazy to plug it into regex101.com,
         * these two Regexes match Discord invites. The reason we don't simply do something like Message.Contains("discord.gg/") || Message.Contains("discord.com/inv..
         * is because that's not only bulky, but its also ugly, and *possibly* slightly slower thanks to extra if-statements. Granted, still probably blazing fast, but
         * I can't be asked to implement that abomination of a pattern when we can just use a regex, and conveniently get what we want out of it without any extra work.
         *
         * And again, for the curious ones, the former regex will match anything that resembles an invite.
         * For instance, discord.gg/HZfZb95, discord.com/invite/HZfZb95, discordapp.com/invite/HZfZb95
         */
        private static readonly Regex AggressiveRegexPattern = new(@"(discord((app\.com|.com)\/invite|\.gg)\/[A-z]+)", flags);

        private static readonly Regex LenientRegexPattern = new(@"discord.gg\/invite\/.+", flags);

        private readonly IInfractionService _infractionService; // I'll implement this soon. //
        private readonly ConfigService _configService; // Pretty self-explanatory; used for caching the guild configs to make sure they've enabled AutoMod //

        private readonly HashSet<string> _blacklistedLinkCache = new();

        public AutoModMessageHandler(ConfigService configService, IInfractionService infractionService) =>
            (_configService, _infractionService) = (configService, infractionService);


        public Task CheckForInvites(DiscordClient c, MessageCreateEventArgs e)
        {
            if (e.Channel.IsPrivate) return Task.CompletedTask;
            
            _ = Task.Run(async () => //Before you ask: Task.Run() in event handlers because await = block
            {
                GuildConfigModel config = await _configService.GetConfigAsync(e.Guild.Id);
                if (e.Message.MentionedUsers.Distinct().Except(e.MentionedUsers.Where(u => u == e.Author)).Count() >=
                    config.MaxUserMentions) await e.Message.DeleteAsync();
                
                if (!config.BlacklistInvites) return;

                Regex matchingPattern = config.UseAggressiveRegex ? AggressiveRegexPattern : LenientRegexPattern;

                Match match = matchingPattern.Match(e.Message.Content);
                if (match.Success)
                {
                    int codeStart = match.Value.LastIndexOf('/');
                    string code = match.Value[(codeStart + 1)..];
                    if (_blacklistedLinkCache.Contains(code))
                    {
                        AutoModMatchedInviteProcedureAsync(config, e.Message, code).GetAwaiter();
                        return;
                    }

                    if (config.ScanInvites)
                    {
                        DiscordInvite invite = await c.GetInviteByCodeAsync(code);
                        // Vanity invite and no vanity URL matches //
                        if (invite.Inviter is null && config.AllowedInvites.All(i => i.VanityURL != invite.Code))
                            AutoModMatchedInviteProcedureAsync(config, e.Message, code).GetAwaiter();

                        if (config.AllowedInvites.All(inv => inv.GuildName != invite.Guild.Name))
                            AutoModMatchedInviteProcedureAsync(config, e.Message, code).GetAwaiter();
                    }
                    else
                    {
                        AutoModMatchedInviteProcedureAsync(config, e.Message, code)
                            .GetAwaiter(); // I can't think of a better name. //
                    }
                }
            });
            
            return Task.CompletedTask;
        }
        
        private async Task AutoModMatchedInviteProcedureAsync(GuildConfigModel config, DiscordMessage message, string invite)
        {
            if (!_blacklistedLinkCache.Contains(invite)) _blacklistedLinkCache.Add(invite);
            if (config.DeleteMessageOnMatchedInvite) await message.DeleteAsync();
            if (config.WarnOnMatchedInvite) return; // Coming Soon™️ //
        }
    }
}