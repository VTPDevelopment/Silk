﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Silk.Core.Utilities;
using Silk.Extensions;

namespace Silk.Core.Commands.General
{
    [Category(Categories.Mod)]
    public class ClearCommand : BaseCommandModule
    {
        [Command]
        [RequireUserPermissions(Permissions.ManageMessages)]
        [Description("Cleans all messages from all users.")]
        public async Task Clear(CommandContext ctx, int messages = 5)
        {
            // Anyone who's got permission to manage channels might not be staff, so adding [RequireFlag(UserFlag.Staff)] needlessly permwalls it. //
            if (!ctx.Member.HasPermission(Permissions.ManageMessages))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                    .WithAuthor(ctx.Member.DisplayName, null, ctx.Member.AvatarUrl)
                    .WithColor(DiscordColor.Red)
                    .WithDescription("Sorry, but you need to be able to manage messages to use this command!"));
                
                return;
            }

            IReadOnlyList<DiscordMessage> queriedMessages = await ctx.Channel.GetMessagesAsync(messages + 1);
            await ctx.Channel.DeleteMessagesAsync(queriedMessages,$"{ctx.User.Username}#{ctx.User.Discriminator} called clear command.");

            DiscordMessage deleteConfirmationMessage = await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                .WithAuthor(ctx.Member.DisplayName, null, ctx.Member.AvatarUrl)
                .WithColor(DiscordColor.SpringGreen)
                .WithDescription($"Cleared {messages} messages!"));
            
            //Change to whatever.//
            await Task.Delay(5000);
            if (deleteConfirmationMessage is not null)
                await ctx.Channel.DeleteMessageAsync(deleteConfirmationMessage);
        }
    }
}