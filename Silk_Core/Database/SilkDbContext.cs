﻿using Microsoft.EntityFrameworkCore;
using SilkBot.Database.Models;
using SilkBot.Database.Models.Items;
using SilkBot.Models;

namespace SilkBot
{
    public class SilkDbContext : DbContext
    {

        public DbSet<GuildModel> Guilds { get; set; }
        public DbSet<TicketModel> Tickets { get; set; }
        //public DbSet<BaseShop> Shops { get; set; }

        public DbSet<Foobar> Foobars { get; set; }
        public DbSet<ChangelogModel> ChangeLogs { get; set; }

        /// <summary>
        /// Users on a guild level; holds information and states that reflect such.
        /// </summary>P
        public DbSet<UserModel> Users { get; set; }


        /// <summary>
        /// Users on a bot level; contains information that should have a globally persisted state.
        /// </summary>
        public DbSet<GlobalUserModel> GlobalUsers { get; set; }

        public SilkDbContext(DbContextOptions<SilkDbContext> options) : base(options) { }


        protected override void OnModelCreating(ModelBuilder builder)
        {

            builder.Entity<UserModel>().HasOne(g => g.Guild);
            
            builder.Entity<UserInfractionModel>().HasIndex(a => new {a.GuildId, a.UserId});

            builder.Entity<GuildModel>().Property(g => g.Id).ValueGeneratedNever();
            builder.Entity<GuildModel>().HasMany(u => u.Users);
            builder.Entity<Ban>().HasOne(b => b.UserInfo);
            builder.Entity<WhiteListedLink>().HasOne(w => w.Guild).WithMany(a => a.WhiteListedLinks);
            builder.Entity<BlackListedWord>().HasOne(_ => _.Guild).WithMany(g => g.BlackListedWords);
            //builder.Entity<UserInfractionModel>().HasOne(i => i.User);
            builder.Entity<TicketResponderModel>().HasNoKey();
            builder.Entity<TicketMessageHistoryModel>().HasOne(ticket => ticket.TicketModel).WithMany(ticket => ticket.History);
            base.OnModelCreating(builder);
        }
    }
    
}