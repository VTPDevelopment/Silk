﻿using Microsoft.EntityFrameworkCore;
using Silk.Core.Data.Models;

namespace Silk.Core.Data
{
    public class GuildContext : DbContext
    {
        public GuildContext(DbContextOptions<GuildContext> options) : base(options) { }
        public DbSet<Guild> Guilds { get; set; } = null!;

        /// <summary>
        ///     Users on a guild level; holds information and states that reflect such.
        /// </summary>
        public DbSet<User> Users { get; set; } = null!;

        public DbSet<Infraction> Infractions { get; set; } = null!;

        /// <summary>
        ///     Users on a bot level; contains information that should have a globally persisted state.
        /// </summary>
        public DbSet<GlobalUser> GlobalUsers { get; set; } = null!;

        public DbSet<CommandInvocation> CommandInvocations { get; set; } = null!;

        public DbSet<GuildConfig> GuildConfigs { get; set; } = null!;

        public DbSet<GuildModConfig> GuildModConfigs { get; set; } = null!;
        
        public DbSet<Tag> Tags { get; set; } = null!;

        public DbSet<Reminder> Reminders { get; set; } = null!;
        

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(typeof(GuildContext).Assembly);
            base.OnModelCreating(builder);
        }
    }
}