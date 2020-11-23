﻿using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SilkBot.Commands.Moderation.Utilities;
using SilkBot.Extensions;
using SilkBot.Models;
using SilkBot.Services;
using SilkBot.Utilities;

namespace SilkBot.Commands.Moderation
{
    [Category(Categories.Mod), UsedImplicitly]
    public class KickCommand : BaseCommandModule
    {

        private readonly ILogger<KickCommand> _logger;
        private readonly IDbContextFactory<SilkDbContext> _dbFactory;
        private readonly InfractionService _infractionService;

        public KickCommand(ILogger<KickCommand> logger, IDbContextFactory<SilkDbContext> dbFactory, InfractionService infractionService)
        {
            _logger = logger;
            _dbFactory = dbFactory;
            _infractionService = infractionService;
        }

        [Command, RequireBotPermissions(Permissions.KickMembers), /* RequireFlag(UserFlag.Staff), RequireGuild(), */ Description("Boot someone from the guild! Caller must have kick members permission.")]
        public async Task Kick(CommandContext ctx, [Description("The person to kick.")] DiscordMember user,
            [RemainingText, Description("The reason the user is to be kicked from the guild")]
            string reason = null)
        {
            var bot = ctx.Guild.CurrentMember;


            if (!ctx.Guild.CurrentMember.HasPermission(Permissions.KickMembers))
            {
                await ctx.RespondAsync(embed: EmbedHelper.CreateEmbed(ctx, "I don't have permission to kick members!", DiscordColor.Red));
                return;
            }
            if (user.IsAbove(bot))
            {
                var isBot = user == bot;
                var isOwner = user == ctx.Guild.Owner;
                var isMod = user.HasPermission(Permissions.KickMembers);
                var isAdmin = user.HasPermission(Permissions.Administrator);
                string errorReason = user.IsAbove(bot) switch
                {
                    true when isBot     =>  "I wish I could kick myself, but I sadly cannot.",
                    true when isOwner   => $"I can't kick the owner ({user.Mention}) out of their own server!",
                    true when isMod     => $"I can't kick {user.Mention}! They're a moderator! ({user.Roles.Last().Mention})",
                    true when isAdmin   => $"I can't kick {user.Mention}! They're an admin! ({user.Roles.Last().Mention})",

                    _ => errorReason = "`UNCAUGHT_CASE_FAILSAFE` That's all I know. Translation: Something has gone wrong, and it's not for any reason I'm aware of."
                };

                var embed = new DiscordEmbedBuilder()
                    .WithAuthor(ctx.Member.Username, ctx.Member.GetUrl(), ctx.Member.AvatarUrl)
                    .WithDescription(errorReason)
                    .WithColor(DiscordColor.Red);

                await ctx.RespondAsync(embed: embed);
            }
            else
            {
                var embed = new DiscordEmbedBuilder()
                        .WithAuthor(ctx.Member.Username, ctx.Member.GetUrl(), ctx.Member.AvatarUrl)
                        .WithColor(DiscordColor.Blurple)
                        .WithThumbnail(ctx.Guild.IconUrl)
                        .WithDescription($"You've been kicked from `{ctx.Guild.Name}`!")
                        .AddField("Reason:", reason ?? "No reason has been attached to this infraction.");
                _infractionService.QueueInfraction(new UserInfractionModel { Enforcer = ctx.User.Id, Reason = reason, InfractionType = InfractionType.Kick, InfractionTime = DateTime.Now, GuildId = ctx.Guild.Id});
                try { await user.SendMessageAsync(embed: embed); }
                catch (InvalidOperationException) { _logger.LogWarning("Couldn't DM member when notifying kick."); }

                await user.RemoveAsync(reason);

                var guildConfig = _dbFactory.CreateDbContext().Guilds.First(g => g.Id == ctx.Guild.Id);
                var logChannelID = guildConfig.GeneralLoggingChannel;
                var logChannelValue = logChannelID == default ? ctx.Channel.Id : logChannelID;
                await ctx.Client.SendMessageAsync(await ctx.Client.GetChannelAsync(logChannelValue),
                    embed: new DiscordEmbedBuilder()
                    .WithAuthor(ctx.Member.DisplayName, "", ctx.Member.AvatarUrl)
                    .WithColor(DiscordColor.SpringGreen)
                    .WithDescription($":boot: Kicked {user.Mention}! (User notified with direct message)")
                    .WithFooter("Silk!")
                    .WithTimestamp(DateTime.Now));
            }
        }
    }
}
