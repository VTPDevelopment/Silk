﻿using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SilkBot.Database.Models;
using SilkBot.Utilities;

namespace SilkBot.Commands.General.Tickets
{
    /// <summary>
    /// Class responsible for the creation of tickets.
    /// </summary>
    [Expiremental]
    [Group("ticket")]
    [Category(Categories.Bot)]
    [Description("Commands related to tickets; opening tickets can only be performed in DMs.")]
    public class TicketCommands : BaseCommandModule
    {
        private readonly TicketHandlerService _ticketService;
        public TicketCommands(TicketHandlerService ticketService) => _ticketService = ticketService;
        
        [Command]
        [RequireDirectMessage]
        public async Task Create(CommandContext ctx, string message = "No message provided")
        {
            TicketCreationResult? result = await _ticketService.CreateAsync(ctx.User, message).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                await ctx.RespondAsync(result.Reason).ConfigureAwait(false);
                return;
            }
            TicketModel ticket = result.Ticket;
            ulong channelId = TicketHandlerService.GetTicketChannel(ticket!.Opener); // If it succeeded, it's not null. //
            DiscordChannel ticketChannel = ctx.Client.Guilds[721518523704410202].GetChannel(channelId);
            DiscordEmbed embed = TicketEmbedHelper.GenerateInboundEmbed(message, ctx.User, ticket);
            await ticketChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        }

        [Command]
        [RequireGuild]
        [Description("Close the ticket the current channel corresponds to.")]
        public async Task Close(CommandContext ctx)
        {
            try { await _ticketService.CloseTicket(ctx.Channel); }
            catch (InvalidOperationException e) { await ctx.RespondAsync(e.Message).ConfigureAwait(false); }
        }

        [Command]
        [RequireGuild]
        public async Task Close(CommandContext ctx, ulong userId)
        {
            try { await _ticketService.CloseTicket(userId); }
            catch (InvalidOperationException e) { await ctx.RespondAsync(e.Message).ConfigureAwait(false); }
        }
        
        
        
    }
}