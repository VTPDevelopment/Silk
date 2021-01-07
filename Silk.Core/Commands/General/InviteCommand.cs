﻿using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Silk.Core.Utilities;

namespace Silk.Core.Commands.General
{
    [Category(Categories.General)]
    public class InviteCommand : BaseCommandModule
    {
        [Command("invite")]
        [HelpDescription("Gives you the Oauth2 code to invite me to your server!")]
        public async Task Invite(CommandContext ctx)
        {
            var oauth2 = $"https://discord.com/api/oauth2/authorize?client_id={ctx.Client.CurrentUser.Id}&permissions=502656214&scope=bot";
            
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Gold)
                .WithDescription($"You can invite me with [this Oauth2]({oauth2}) Link!"));
        }
    }
}