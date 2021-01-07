﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.EntityFrameworkCore;
using Silk.Core.Database;
using Silk.Core.Database.Models;
using Silk.Core.Services.Interfaces;
using Silk.Extensions;

namespace Silk.Core.Utilities
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class RequireFlagAttribute : CheckBaseAttribute
    {
        public bool RequireGuild { get; }
        public UserFlag RequisiteUserFlag { get; }
        private static readonly HashSet<ulong> _cachedStaff = new();

        /// <summary>
        /// Check for a requisite flag from the database, and execute if check passes.
        /// </summary>
        /// <param name="RequisiteUserFlag">The required flag for the command to run; this flag is ignored when run in a help context</param>
        /// <param name="RequireGuild">Restrict command usage to guild as well as requisite flag. Defaults to false.</param>
        public RequireFlagAttribute(UserFlag RequisiteUserFlag, bool RequireGuild = false)
        {
            this.RequisiteUserFlag = RequisiteUserFlag;
            this.RequireGuild = RequireGuild;
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (help) return help;
            if (ctx.Guild is null && RequireGuild) return false; //Is a private channel and requires a Guild//
            if (_cachedStaff.Contains(ctx.User.Id) && RequireGuild) return true;

            IDatabaseService db = ctx.Services.Get<IDatabaseService>();
            UserModel? member = await db.GetGuildUserAsync(ctx.Guild!.Id, ctx.User.Id);

            if (member is null) return false;
            if (member.Flags.HasFlag(UserFlag.Staff)) _cachedStaff.Add(member.Id);
            
            return member.Flags.HasFlag(UserFlag.Staff);
        }
    }
}