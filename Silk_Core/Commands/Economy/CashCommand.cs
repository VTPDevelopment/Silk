﻿using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.EntityFrameworkCore;
using SilkBot.Extensions;
using SilkBot.Utilities;

namespace SilkBot.Commands.Economy
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
        [Aliases("Money")]
        public async Task Cash(CommandContext ctx)
        {
            using var db = _dbFactory.CreateDbContext();
            var account = db.GlobalUsers.FirstOrDefault(u => u.Id == ctx.User.Id);
            if (account is null) { await ctx.RespondAsync($"Seems you don't have an account. Use `{ctx.Prefix}daily` and I'll set one up for you *:)*"); return; }

            var eb = EmbedHelper.CreateEmbed(ctx, "Account balance:", $"You have {account.Cash} dollars!").WithAuthor(name: ctx.User.Username, iconUrl: ctx.User.AvatarUrl);
            await ctx.RespondAsync(embed: eb);
        }
    }
}
