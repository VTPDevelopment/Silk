﻿using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Silk.Core.Utilities.HelpFormatter;

namespace Silk.Core.Commands.Bot
{
    [Category(Categories.Bot)]
    public class SudoCommand : BaseCommandModule
    {
        [Command]
        [RequireOwner]
        [Description("Run a command as a different user! \"I'm trusting you. ~Velvet\"")]
        public async Task Sudo(CommandContext ctx, DiscordUser user, string command, [RemainingText] string? parameters)
        {
            bool exists = ctx.CommandsNext.RegisteredCommands.TryGetValue(command, out Command? cmd);
            if (!exists)
            {
                await ctx.RespondAsync("That isn't a valid command!");
                return;
            }

            CommandContext context = ctx.CommandsNext.CreateFakeContext(user, ctx.Channel, ctx.Message.Content, ctx.Prefix, cmd, parameters);
            await ctx.CommandsNext.ExecuteCommandAsync(context).ConfigureAwait(false);
        }
    }
}