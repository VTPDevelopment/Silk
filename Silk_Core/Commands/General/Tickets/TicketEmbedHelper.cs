﻿using System;
using DSharpPlus.Entities;
using SilkBot.Database.Models;
using SilkBot.Extensions;

namespace SilkBot.Commands.General.Tickets
{
    public static class TicketEmbedHelper
    {
        public static DiscordEmbed GenerateOutboundEmbed(string message, DiscordUser responder) => 
            new DiscordEmbedBuilder()
                    .WithTitle("Ticket response:")
                    .WithAuthor(responder.Username, responder.GetUrl(), responder.AvatarUrl)
                    .WithDescription(message)
                    .WithFooter("Ticket history is saved for security purposes")
                    .WithTimestamp(DateTime.Now)
                    .WithColor(DiscordColor.Goldenrod)
                    .Build();

        public static DiscordEmbed GenerateInboundEmbed(string message, DiscordUser ticketOpener, TicketModel ticket) =>
            new DiscordEmbedBuilder()
                .WithAuthor(ticketOpener.Username, null, ticketOpener.AvatarUrl)
                .WithColor(DiscordColor.DarkBlue)
                .WithDescription(message)
                .WithFooter($"Silk! | Ticket Id: {ticket.Id}")
                .WithTimestamp(DateTime.Now);

        public static DiscordEmbed GenerateTicketClosedEmbed() =>
            new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Goldenrod)
                .WithTitle("Your ticket has been closed.")
                .WithDescription("Your ticket has been manually closed. If you have any futher issues, feel free to open a new ticket via `ticket create [message]`")
                .Build();
    }
}