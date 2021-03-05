using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using ProBot.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProBot.Commands
{
    public class UsefulCommands : BaseCommandModule
    {
        [Command("ping")]
        [Description("Ping Pong")]
        public async Task Ping(CommandContext ctx)
        {
            // Simple Ping Pong message
            await ctx.Channel.SendMessageAsync("Pong").ConfigureAwait(false);
        }

        [Command("poll")]
        [Description("Creates a poll based on Title, Duration and Reactions")]
        [RequireCategoriesAtrribute(ChannelCheckMode.Any,"CHAT")]
        public async Task Poll(CommandContext ctx,
            [Description("Poll's duration i.e 10s 5m 2h...")] TimeSpan duration,
            [Description("Title of the poll")] string pollTitle,
            [Description("Poll's reactions to be used as result")] params DiscordEmoji[] emojiOptions)
        {
            // Create interactivity extension
            var interactivity = ctx.Client.GetInteractivity();
            // Get the Emojis as strings
            var options = emojiOptions.Select(x => x.ToString());
            // Get user avatar 
            var avatar = ctx.User.GetAvatarUrl(DSharpPlus.ImageFormat.Png);

            // Create the Embed
            var pollEmbed = new DiscordEmbedBuilder();
            pollEmbed.WithTitle($"Poll for : {pollTitle}"); //Title
            pollEmbed.WithDescription(string.Join("   ", options)); //Description
            pollEmbed.WithColor(DiscordColor.Azure); //Form color
            pollEmbed.WithFooter($"{ctx.User.Username}", avatar ?? ctx.User.DefaultAvatarUrl); // Form footer
            pollEmbed.WithThumbnail(avatar, 10, 10); // Thumbnail
            var pollMessage = await ctx.Channel.SendMessageAsync(embed: pollEmbed).ConfigureAwait(false); // Send the embed to the channel

            // Loop through different emojis that were passed in
            foreach (var option in emojiOptions)
            {
                await pollMessage.CreateReactionAsync(option).ConfigureAwait(false);
            }

            // Get reactions for the specified time duration
            var pollResult = await interactivity.CollectReactionsAsync(pollMessage, duration).ConfigureAwait(false);
            // Make a distinct list by excluding bot pre reaction
            var distinctResult = pollResult.Distinct();
            // Gather total for each pressed emojis
            var finalResult = distinctResult.Select(x => $"{x.Emoji}: {x.Total} \n");
            // Format the message output
            await ctx.Channel.SendMessageAsync("Final Result: \n ------------ \n");
            // Send back the result to the channel
            await ctx.Channel.SendMessageAsync(string.Join("\n", finalResult)).ConfigureAwait(false);
        }
    }
}
