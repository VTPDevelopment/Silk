﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SilkBot.Utilities;

namespace SilkBot.Commands.Bot
{

    [Category(Categories.Bot)]
    public class Restart : BaseCommandModule
    {
        [RequireOwner]
        [Command("restart")]
        public async Task RestartBot(CommandContext ctx)
        {
            await ctx.Client.UpdateStatusAsync(userStatus: UserStatus.DoNotDisturb);
            await ctx.RespondAsync(embed:
                new DiscordEmbedBuilder()
                .WithTitle("Restart command recieved!")
                .WithDescription("Restarting... Commands will be processed when status is green.")
                .WithColor(new DiscordColor("#29ff29"))
                .WithFooter("Silk!", ctx.Client.CurrentUser.AvatarUrl)
                .WithTimestamp(DateTime.Now)
                );
            SilkBot.Bot.Instance.SilkDBContext.SaveChanges();

            Process.Start(@"C:\Users\Cinnamon\Desktop\Restart Bot.bat");
        }
    }
}
