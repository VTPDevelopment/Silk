﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Serilog;
using Silk.Core.Database.Models;
using Silk.Core.Services;
using Silk.Core.Services.Interfaces;

namespace Silk.Core.Tools.EventHelpers
{
    public class MemberRemovedHandler
    {
        private readonly IDatabaseService _dbService;
        private readonly ILogger<MemberRemovedHandler> _logger;

        public MemberRemovedHandler(IDatabaseService dbService, ILogger<MemberRemovedHandler> logger) =>
            (_dbService, _logger) = (dbService, logger);

        /// <summary>
        /// Event handler responsible for removing user from the database if they leave a guild.
        /// However a member with a case history will not be removed from the database.
        /// </summary>
        public Task OnMemberRemoved(DiscordClient c, GuildMemberRemoveEventArgs e)
        {
            // This will be handled if a ban has been added, in which case we don't do anything //
            try
            {
            }
            catch (KeyNotFoundException)
            {
            } // DSP didn't cache this member //

            if (e.Handled || e.Member.IsBot) return Task.CompletedTask;
            
            e.Handled = true;
            
            _ = Task.Run(async () =>
            {
                UserModel? user = await _dbService.GetGuildUserAsync(e.Guild.Id, e.Member.Id);
                if (user is null) return; // Doesn't exist in the DB. No point in continuing. //
                if (user.Infractions.Any()) return; // They have infractions, and shouldn't be removed from the DB. //

                await _dbService.RemoveUserAsync(user);
                _logger.LogInformation($"{e.Member.Username} was removed from {e.Guild.Name}!");
            });
            
            return Task.CompletedTask;
        }
    }
}