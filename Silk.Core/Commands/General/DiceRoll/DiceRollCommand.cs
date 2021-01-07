﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Silk.Core.Utilities;

namespace Silk.Core.Commands.General.DiceRoll
{
    [Category(Categories.General)]
    public class DiceRollCommand : BaseCommandModule
    {
        [Command]
        [Description("Generate a random number in a given range; defaults to 100. (Hard limit of ~2.1 billion)")]
        public async Task Random(CommandContext ctx, int max = 100) =>
            await ctx.RespondAsync(new Random().Next(max).ToString()).ConfigureAwait(false);

        [Command]
        [Description("Roll die like it's DnD! Example: 2d4 + 10 + d7")]
        public async Task Roll(CommandContext ctx, [RemainingText] string roll)
        {
            var parser = new DiceParser(roll);
            IList<Step> steps = parser.Run();

            var embed = InitEmbed(new());
            var ran = new Random((int) ctx.Message.Id);
            var modifiers = new List<int>();
            var rolls = new List<int>();

            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i].Type == StepType.Addition)
                {
                    modifiers.Add(steps[i].TotalNumber);
                }
                else
                {
                    var localRolls = new List<int>();
                    for (int j = 0; j < steps[i].TotalNumber; j++)
                    {
                        int result = ran.Next(1, steps[i].DiceNoSides + 1);
                        localRolls.Add(result);
                    }

                    int sum = localRolls.Sum();
                    rolls.Add(sum);
                    embed.AddField($"🎲{steps[i].TotalNumber}d{steps[i].DiceNoSides}",
                        $"\t{string.Join(", ", localRolls)}   =   {sum}");
                }
            }


            embed.AddField("Modifiers", $"\t{string.Join(", ", modifiers)} | {modifiers.Sum()}");
            embed.AddField("Total", $"{(rolls.Sum() + modifiers.Sum())}");
            await ctx.RespondAsync(embed: embed).ConfigureAwait(false);
        }

        private DiscordEmbedBuilder InitEmbed(DiscordEmbedBuilder embed) =>
            embed.WithColor(DiscordColor.PhthaloGreen)
                .WithTitle("You rolled:")
                .WithFooter("Made by alex#6555 with <3");
    }
}