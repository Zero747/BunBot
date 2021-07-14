// RoleRequestSystem.Commands.cs
// One piece of a partial class for the role request system, specifically the piece handling commands.
//
// EMIKO

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using BigSister.ChatObjects;

namespace BigSister.RoleRequest
{
    static partial class RoleRequestSystem
    {
        // Note: not actually an embed
        internal static async Task RoleEmbedCreate(CommandContext ctx, 
                                                   DiscordChannel channel, 
                                                   string title,
                                                   DiscordEmoji emoji,
                                                   DiscordRole role)
        {
            if (title.Length > 0)
            {

                DiscordMessage roleMessage = await channel.SendMessageAsync(
                    content: String.Format("{0}\n{1} {2}",
                                arg0: title,
                                arg1: EmojiConverter.GetEmojiString(emoji),
                                arg2: role.Mention));

                var a = roleMessage.CreateReactionAsync(emoji);
                var b = AddMessageToDatabase(roleMessage.Id, role, emoji);

                await Task.WhenAll(a, b);

                await ctx.RespondAsync(
                    embed: Generics.GenericEmbedTemplate(
                        color: Generics.NeutralColor,
                        description: $"Tasks:\nCreate Reaction Success: {a.IsCompletedSuccessfully}\n" +
                                     $"Database Success: {a.IsCompletedSuccessfully}",
                        title: @"Create new embed"));
            }
            else
            {
                await GenericResponses.HandleInvalidArguments(ctx);
            }
        }

        internal static async Task RoleEmbedAppendRole(CommandContext ctx, 
                                                       DiscordChannel channel,
                                                       ulong messageId, 
                                                       DiscordEmoji emoji, 
                                                       DiscordRole role)
        {
            var a = CheckEmoteMessageExists(messageId, emoji);
            var b = channel.GetMessageAsync(messageId);

            await Task.WhenAll(a, b);

            bool emoteExistsAlready = a.Result;
            DiscordMessage message = b.Result;
            
            // Check if that role is already on this message.
            if(!emoteExistsAlready)
            {   // It does not, so let's continue.

                var c = message.ModifyAsync(String.Format("{0}\n{1} {2}",
                    arg0: message.Content,
                    arg1: EmojiConverter.GetEmojiString(emoji),
                    arg2: role.Mention));
                var d = message.CreateReactionAsync(emoji);
                var e = AddMessageToDatabase(messageId, role, emoji);

                await Task.WhenAll(c, d, e);

                await ctx.RespondAsync(
                    embed: Generics.GenericEmbedTemplate(
                        color: Generics.NeutralColor,
                        description: $"Tasks:\nEdit Success: {c.IsCompletedSuccessfully}\n" +
                                     $"Create Reaction Success: {d.IsCompletedSuccessfully}\n" +
                                     $"Database Success: {e.IsCompletedSuccessfully}",
                        title: @"Add new roles onto embed"));
            }
            else
            {
                await ctx.RespondAsync(
                    embed: Generics.GenericEmbedTemplate(
                        color: Generics.NegativeColor,
                        description: Generics.NegativeDirectResponseTemplate(
                            mention: ctx.Member.Mention,
                            body: $"that message already has emote {EmojiConverter.GetEmojiString(emoji)} on it...")));
            }
        }

        internal static async Task RoleEmbedRemoveRole(CommandContext ctx, 
                                                 DiscordChannel channel, 
                                                 ulong messageId, 
                                                 DiscordEmoji emoji)
        {
            var a = channel.GetMessageAsync(messageId);
            var b = CheckEmoteMessageExists(messageId, emoji);

            await Task.WhenAll(a, b);

            DiscordMessage message = a.Result;
            bool messageHasEmote = b.Result;

            // Check if the message actually has that emote.
            if (messageHasEmote)
            {   // It does, so let's query the database for everything that should be in the message.

                EmojiData emojiRemove = new EmojiData(emoji);
                RoleInfo[] roleInfos = await GetMessageEmotes(messageId);

                string botComments = String.Empty;

                var c = RemoveRow(messageId, emojiRemove.Value);
                await Task.WhenAll(c);

                // Let's check if this is the message's only react.
                if (roleInfos.Length == 1)
                {   // It is, so let's delete the message as well.

                    await message.DeleteAsync();
                    botComments = @"Additionally, the message was deleted because you deleted its only react.";
                }
                else
                {   // It's not the only react, so let's rebuild the message string.

                    // Get the first line of the content.
                    var stringBuilder = new StringBuilder(
                        new string(message.Content.TakeWhile(a => a != '\n').ToArray()));

                    stringBuilder.Append('\n');

                    foreach (var roleInfo in roleInfos)
                    {
                        // Check if this is the emoji we want to remove.
                        if (!roleInfo.EmojiData.Equals(emojiRemove))
                        {   // It's not the emoji we want to remove, so let's add it to the stringbuilder.
                            DiscordRole role = ctx.Guild.GetRole(roleInfo.RoleId);

                            string emojiString = EmojiConverter.GetEmojiString(
                                emoji: EmojiConverter.GetEmoji(
                                    cl: ctx.Client,
                                    data: roleInfo.EmojiData));

                            stringBuilder.AppendLine($"{role.Mention} {emojiString}");
                        } // end if 
                    }  // end foreach

                    var d = message.ModifyAsync(stringBuilder.ToString());
                    var e = message.DeleteReactionsEmojiAsync(emoji);

                    await Task.WhenAll(d, e);

                    await ctx.RespondAsync(
                        embed: Generics.GenericEmbedTemplate(
                            color: Generics.NeutralColor,
                            description: $"Tasks:\nMessage Edit Success: {d.IsCompletedSuccessfully || e.IsCompletedSuccessfully}\n" +
                                         $"Database Check Success: {b.IsCompletedSuccessfully}\n" +
                                         $"Database Delete Success: {c.IsCompletedSuccessfully}" +
                                         (botComments.Length > 0 ? $"\n{botComments}" : String.Empty),
                            title: @"Add new roles onto embed"));
                } // end else
            } // end if
            else
            {   // It doesn't have the emote we want to remove, so let's notify the user.
                await ctx.RespondAsync(
                    embed: Generics.GenericEmbedTemplate(
                        color: Generics.NegativeColor,
                        description: Generics.NegativeDirectResponseTemplate(
                            mention: ctx.Member.Mention,
                            body: $"that message doesn't have emote {EmojiConverter.GetEmojiString(emoji)} on it...")));
            }
        }

        internal static async Task RoleMessageNew(DiscordChannel channel, 
                                                  string content)
        {
            await channel.SendMessageAsync(content);
        }

        internal static async Task RoleMessageEdit(DiscordChannel channel, 
                                                   ulong messageId, 
                                                   string content)
        {
            DiscordMessage message = await channel.GetMessageAsync(messageId);

            // Only continue if the message was found.
            if( !(message is null) && (message.Id == messageId) )
            {
                await message.ModifyAsync(content);
            }
        }
    }
}
