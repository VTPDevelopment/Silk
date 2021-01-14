﻿using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Silk.Core.Database;
using Silk.Core.Database.Models;
using Silk.Core.Utilities;

namespace Silk.Core.Commands.Economy
{
    [Category(Categories.Economy)]
    public class CashCommand : BaseCommandModule
    {
        private readonly IDbContextFactory<SilkDbContext> _dbFactory;

        public CashCommand(IDbContextFactory<SilkDbContext> dbContextFactory)
        {
            _dbFactory = dbContextFactory;
        }

        [Command("Cash")]
        [Aliases("Money", "bal", "balance", "bank")]
        public async Task Cash(CommandContext ctx)
        {
            SilkDbContext db = _dbFactory.CreateDbContext();
            GlobalUserModel? account = db.GlobalUsers.FirstOrDefault(u => u.Id == ctx.User.Id);
            if (account is null)
            {
                await ctx.RespondAsync(
                    $"Seems you don't have an account. Use `{ctx.Prefix}daily` and I'll set one up for you *:)*");
                return;
            }

            DiscordEmbedBuilder eb = EmbedHelper
                .CreateEmbed(ctx, "Account balance:", $"You have {account.Cash} dollars!")
                .WithAuthor(ctx.User.Username, iconUrl: ctx.User.AvatarUrl);
            await ctx.RespondAsync(embed: eb);
        }
    }
}