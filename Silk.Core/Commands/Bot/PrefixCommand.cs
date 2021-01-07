﻿using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.EntityFrameworkCore;
using Silk.Core.Database;
using Silk.Core.Database.Models;
using Silk.Core.Services;
using Silk.Core.Utilities;

namespace Silk.Core.Commands.Bot
{
    [Category(Categories.Bot)]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class PrefixCommand : BaseCommandModule
    {
        private const int PrefixMaxLength = 5;
        private readonly PrefixCacheService _prefixCache;
        private readonly IDbContextFactory<SilkDbContext> _dbFactory;

        private record PrefixValidationResult(bool Valid, string Reason);

        public PrefixCommand(PrefixCacheService prefixCache, IDbContextFactory<SilkDbContext> dbFactory)
        {
            _prefixCache = prefixCache;
            _dbFactory = dbFactory;
        }

        [Command("setprefix")]
        [RequireFlag(UserFlag.Staff)]
        public async Task SetPrefix(CommandContext ctx, string prefix)
        {
            SilkDbContext db = _dbFactory.CreateDbContext();
            (bool valid, string reason) = IsValidPrefix(prefix);
            if (!valid)
            {
                await ctx.RespondAsync(reason);
                return;
            }

            GuildModel guild = db.Guilds.First(g => g.Id == ctx.Guild.Id);
            guild.Prefix = prefix;
            _prefixCache.UpdatePrefix(ctx.Guild.Id, prefix);
            await db.SaveChangesAsync();
            await ctx.RespondAsync($"Done! I'll respond to `{prefix}` from now on.");
        }

        private PrefixValidationResult IsValidPrefix(string prefix)
        {
            if (prefix.Length > PrefixMaxLength)
                return new(false, $"Prefix cannot be more than {PrefixMaxLength} characters!");

            if (!Regex.IsMatch(prefix, "[A-Z!@#$%^&*<>?.]+", RegexOptions.IgnoreCase))
                return new(false, "Invalid prefix! `[Valid symbols: ! @ # $ % ^ & * < > ? / and A-Z (Case insensitive)]`");

            return new(true, "");
        }

        [Command("Prefix")]
        public async Task SetPrefix(CommandContext ctx)
        {
            string? prefix = _prefixCache.RetrievePrefix(ctx.Guild?.Id);
            await ctx.RespondAsync($"My prefix is `{prefix}`, but you can always use commands by mentioning me! ({ctx.Client.CurrentUser.Mention})");
        }
    }
}