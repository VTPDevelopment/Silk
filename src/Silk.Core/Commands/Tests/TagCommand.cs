﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using MediatR;
using Silk.Core.Services;
using Silk.Core.Utilities.HelpFormatter;
using Silk.Data.MediatR;
using Silk.Data.Models;
using Silk.Extensions;

namespace Silk.Core.Commands.Tests
{
    [RequireGuild]
    [Group("tag")]
    [Category(Categories.Server)]
    public class TagCommand : BaseCommandModule
    {
        private readonly IMediator _mediator;
        private readonly TagService _tagService;
        private readonly string[] _reservedWords = new[]
        {
            "create", "update", "delete",
            "alias", "info", "claim"
        };
        public TagCommand(IMediator mediator, TagService tagService)
        {
            _mediator = mediator;
            _tagService = tagService;
        }

        [GroupCommand]
        public async Task Tag(CommandContext ctx, [RemainingText] string tag)
        {
            Tag? dbTag = await _tagService.GetTagAsync(tag, ctx.Guild.Id);
            
            if (dbTag is null)
            {
                await ctx.RespondAsync("Tag not found!");
            }
            else
            {
                await ctx.RespondAsync(dbTag.Content);
                await _mediator.Send(new TagRequest.Update(tag, ctx.Guild.Id) {Uses = dbTag.Uses + 1});
            }
        }

        [Command]
        public async Task Info(CommandContext ctx, [RemainingText] string tag)
        {
            Tag? dbTag = await _tagService.GetTagAsync(tag, ctx.Guild.Id);
            
            if (dbTag is null)
            {
                await ctx.RespondAsync("Tag not found! :(");
            }
            else
            {
                DiscordUser tagOwner = await ctx.Client.GetUserAsync(dbTag.OwnerId);
                var builder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Blurple)
                    .WithAuthor(tagOwner.Username, iconUrl: tagOwner.AvatarUrl)
                    .WithTitle(dbTag.Name);
                
                builder
                    .AddField("Uses:", dbTag.Uses.ToString())
                    .AddField("Created At:", dbTag.CreatedAt.ToUniversalTime().ToString("MM/dd/yyyy - h:mm UTC"));

                if (dbTag.OriginalTag is not null)
                {
                    builder.AddField("Original:", dbTag.OriginalTag.Name);
                }
                
                await ctx.RespondAsync(builder);
            }
        }

        [Command]
        public async Task Alias(CommandContext ctx, string aliasName, [RemainingText] string orignalTag)
        {
            var couldCreateAlias = await _tagService.AliasTagAsync(orignalTag, aliasName, ctx.Guild.Id, ctx.User.Id);
            if (!couldCreateAlias.success)
            {
                await ctx.RespondAsync(couldCreateAlias.reason);
            }
            else
            {
                await ctx.RespondAsync($"Alias `{aliasName}` that points to tag `{orignalTag}` successfuly created.");
            }
        }
        
        [Command]
        public async Task Create(CommandContext ctx, string tagName, [RemainingText] string content)
        {
            Tag? tag = await _tagService.GetTagAsync(tagName, ctx.Guild.Id);
            if (tag is not null)
            {
                await ctx.RespondAsync("Tag with that name already exists!");
            }
            if (_reservedWords.Any(tagName.StartsWith))
            {
                await ctx.RespondAsync("Tag name begins with reserved keyword!");
                return;
            }
            if (string.IsNullOrEmpty(content))
            {
                await ctx.RespondAsync("Missing tag content!");
                return;
            }

            var couldCreateTag = await _tagService.CreateTagAsync(tagName, content, ctx.Guild.Id, ctx.User.Id);
            if (!couldCreateTag.success)
            {
                await ctx.RespondAsync(couldCreateTag.reason);
            }
            else
            {
                await ctx.RespondAsync($"Successfully created tag **{tagName}**.");
            }
        }

        [Command]
        public async Task Delete(CommandContext ctx, [RemainingText] string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                await ctx.RespondAsync("Must specify a tag to delete!");
                return;
            }
            
            Tag? tag = await _tagService.GetTagAsync(tagName, ctx.Guild.Id);
            if (tag is null)
            {
                await ctx.RespondAsync("Tag not found!");
                return;
            }
            User? user = await _mediator.Send(new UserRequest.Get(ctx.Guild.Id, ctx.User.Id));

            if (tag.OwnerId != ctx.User.Id && (!user?.Flags.Has(UserFlag.Staff) ?? true))
            {
                await ctx.RespondAsync("You either do not own this tag, or are not staff!");
                return;
            }

            await _tagService.RemoveTagAsync(tagName, ctx.Guild.Id);
            
            string message = 
                tag.OriginalTag is not null ?
                "Alias successfully deleted." :
                tag.Aliases?.Any() ?? false ?
                    "Tag and all corresponded aliases successfully deleted." :
                    "Tag successfully deleted.";
            
            await ctx.RespondAsync(message);

        }

        [Command]
        public async Task Edit(CommandContext ctx, string tagName, [RemainingText] string content)
        {
            var couldEditTag = await _tagService.UpdateTagContentAsync(tagName, content, ctx.Guild.Id, ctx.User.Id);
            if (!couldEditTag.success)
            {
                await ctx.RespondAsync(couldEditTag.reason);
            }
            else
            {
                await ctx.RespondAsync("Sucessfully edited tag!");
            }
        }
        
        [Command]
        public async Task Search(CommandContext ctx, string tagName)
        {
            var tags = await _mediator.Send(new TagRequest.GetByName(tagName, ctx.Guild.Id));
            if (tags is null)
            {
                await ctx.RespondAsync("No tags found :c");
                return;
            }
            
            string allTags = string.Join("\n\n", tags!
                .Select(t =>
                {
                    var s = $"`{t.Name}`";
                    if (t.OriginalTagId is not null)
                    {
                        s += $" → `{t.OriginalTag!.Name}`";
                    }
                    s += $" - <@{t.OwnerId}>";
                    return s;
                }));
            var builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Blurple)
                .WithTitle($"Result for {tagName}:")
                .WithFooter($"Silk! | Requested by {ctx.User.Id}");
            
            if (tags.Count() < 10)
            {
                builder.WithDescription(allTags);
                await ctx.RespondAsync(builder);
            }
            else
            {
                var interactivity = ctx.Client.GetInteractivity();

                var pages = interactivity.GeneratePagesInEmbed(allTags, SplitType.Line, builder);
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
            }
            
        }
        
    }
    
    [RequireGuild]
    [Category(Categories.Server)]
    public class TagsCommand : BaseCommandModule
    {
        private readonly TagService _tagService;
        public TagsCommand(TagService tagService)
        {
            _tagService = tagService;
        }
        
        [Command]
        public async Task Tags(CommandContext ctx, DiscordMember user)
        {
            IEnumerable<Tag>? tags = await _tagService.GetUserTagsAsync(user.Id, ctx.Guild.Id);
            if (tags is null)
            {
                await ctx.RespondAsync("User has no tags! :c");
                return;
            }
            
            string allTags = string.Join('\n', tags
                .Select(t =>
                {
                    var s = $"`{t.Name}`";
                    if (t.OriginalTagId is not null)
                    {
                        s += $" → `{t.OriginalTag!.Name}`";
                    }
                    return s;
                }));
            var builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Blurple)
                .WithTitle($"Tags for {user.Username}:")
                .WithFooter($"Silk! | Requested by {ctx.User.Id}");
            
            if (tags.Count() < 10)
            {
                builder.WithDescription(allTags);
                await ctx.RespondAsync(builder);
            }
            else
            {
                var interactivity = ctx.Client.GetInteractivity();

                var pages = interactivity.GeneratePagesInEmbed(allTags, SplitType.Line, builder);
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
            }
        }
        [Command]
        public async Task Tags(CommandContext ctx) => await Tags(ctx, ctx.Member);
    }
}
