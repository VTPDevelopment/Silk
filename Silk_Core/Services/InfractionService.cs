﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SilkBot.Extensions;
using SilkBot.Models;

namespace SilkBot.Services
{
    public class InfractionService
    {
        private readonly ILogger<InfractionService> _logger;
        private readonly IDbContextFactory<SilkDbContext> _dbFactory;
        private readonly ConcurrentQueue<UserInfractionModel> _infractionQueue = new ConcurrentQueue<UserInfractionModel>();
        private readonly Timer _queueDrainTimer = new Timer(30000);

        public InfractionService(ILogger<InfractionService> logger, IDbContextFactory<SilkDbContext> dbFactory)
        {
            _logger = logger;
            _dbFactory = dbFactory;
            _queueDrainTimer.Elapsed += (_, _) => _ = DrainTimerElapsed();
            _queueDrainTimer.Start();
        }

        private async Task DrainTimerElapsed()
        {
            if (_infractionQueue.IsEmpty)
                _logger.LogInformation("Infraction queue empty! Skipping this round.");
            else
            {

                await using var db = _dbFactory.CreateDbContext();

                while (_infractionQueue.TryDequeue(out var infraction))
                {
                    GuildModel guild = db.Guilds.First(g => g.Id == infraction.GuildId);
                    UserModel user = guild.Users.FirstOrDefault(u => u.Id == infraction.UserId); 
                    if(user is null)
                    {
                        user = new UserModel { Flags = UserFlag.KickedPrior, Id = infraction.UserId, Guild = guild};
                        user.Infractions.Add(infraction);
                        await db.Users.AddAsync(user);
                        int changed = await db.SaveChangesAsync();
                        if (changed is 0) _logger.LogWarning("Expected to log [1] entity, but saved [0]");
                    }
                    else
                    {
                        user.Flags.Add(UserFlag.KickedPrior);
                        user.Infractions.Add(infraction);
                        int changed = await db.SaveChangesAsync();
                        if (changed is 0) _logger.LogWarning("Expected to log [1] entity, but saved [0]");
                    }
                   
                }
                _logger.LogDebug("Drained infraction queue.");  
            }
        }

        public void QueueInfraction(UserInfractionModel infraction) => _infractionQueue.Enqueue(infraction);


        public IEnumerable<UserInfractionModel> GetInfractions(ulong userId)
        {
            var db = _dbFactory.CreateDbContext();
            UserModel user = db.Users.Include(u => u.Infractions).FirstOrDefault(u => u.Id == userId);
            return user?.Infractions;
        }



    }
}
