// MentionSnooper.cs
// Receives message creation and update events and looks for mentions in them, assuming they're sent in the action logs channel.
//


using BigSister.ChatObjects;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BigSister.MentionSnooper
{
    public static partial class MentionSnooper
    {
        internal static async Task BotClientMessageCreated(DiscordClient botClient, MessageCreateEventArgs e)
        {
            if (Program.Settings.AutoWarnSnoopEnabled &&
                //!e.Author.IsCurrent &&
                e.Channel.Id == Program.Settings.ActionChannelId)
            {   // Only continue if this is the action channel and wasn't sent by the bot.

                // ----
                // Get the DiscordMember of each user.

                var mentionedColonistIds = new List<ulong>();

                foreach (var user in GetMessageMentions(e.Message.Content))
                {
                    try
                    {
                        var member = await e.Guild.GetMemberAsync(user);

                        var userPerms = new UserPermissions(member.Roles);

                        if (!userPerms.IsRoleOrHigher(Role.CS) && !member.IsBot)
                        {   // Only add this person if they're not CS or higher AND not a bot.

                            mentionedColonistIds.Add(member.Id);
                        } // end if
                    }
                    catch
                    {   // Most likely they're not in the guild if an exception is thrown, so let's just add their id to the list anyway.
                        mentionedColonistIds.Add(user);
                    }
                } // end foreach

                // ----
                // Great, now we have all our mentioned colonists. Let's just make sure there's no duplicates.
                mentionedColonistIds = mentionedColonistIds.Distinct().ToList();

                if (mentionedColonistIds.Count > 0)
                {   // Only continue if there's actually mentioned colonists.

                    var actionChannel = e.Channel;

                    // Create a dictionary based on a DiscordMember and all of the messages mentioning him or her.
                    Dictionary<ulong, List<DiscordMessage>> warnDict =
                        await QueryMemberMentions(mentionedColonistIds, actionChannel, Program.Settings.MaxActionAgeMonths, e.Message);

                    // ----
                    // So at this point, we know there's at least one person who has been warned (0,inf) times.
                    // Let's notify the action channel.

                    await NotifyActionLogsMentionsFound(warnDict, actionChannel);
                } // end if
            } // end if
        } // end method

        internal static async Task BotClientMessageUpdated(DiscordClient botClient, MessageUpdateEventArgs e)
        {
            if (Program.Settings.AutoWarnSnoopEnabled &&
                !e.Author.IsCurrent &&
                e.Channel.Id == Program.Settings.ActionChannelId)
            {   // We only want to continue through here if warn snooping is enabled, the bot isn't self-triggering, and we're in #action-logs.

                // Do not used cache messages in case something changed. Let's get messages directly from the server.
                var oldMessage = e.MessageBefore;
                var newMessage = e.Message;

                // If either of the messages aren't cached, we should just stop right here. We can't tell if any new mentions were added, so we'll
                // end up causing more spam therefore more harm. It's unfortunate because we should always try to find some way to interact with
                // some kind of data, but this is a hard limitation I've ran into.

                if (!(oldMessage is null) && !(newMessage is null))
                {   // Only continue if neither message is null.
                    var mentionUsers_old = GetMessageMentions(oldMessage.Content);
                    var mentionUsers_new = GetMessageMentions(newMessage.Content);

                    // Users that we've found by comparing the old and new lists.
                    var newlyFoundUserIds = new List<ulong>();

                    for (int i = 0; i < mentionUsers_new.Count; i++)
                    {
                        var user = mentionUsers_new[i];

                        if (!mentionUsers_old.Contains(user))
                        {   // If the old list doesn't contain it, this means it's a new mention.

                            newlyFoundUserIds.Add(user);
                        }
                    }

                    if (newlyFoundUserIds.Count > 0)
                    {   // We only want to start formatting a message and sending it if there's actually users to look at.

                        var actionChannel = e.Channel;

                        // Create a dictionary based on a DiscordMember and all of the messages mentioning him or her.
                        Dictionary<ulong, List<DiscordMessage>> warnDict =
                            await QueryMemberMentions(newlyFoundUserIds, actionChannel, Program.Settings.MaxActionAgeMonths, e.Message);

                        await NotifyActionLogsMentionsFound(warnDict, actionChannel);
                    } // end if
                } // end if
            } // end if
        }

        static async Task NotifyActionLogsMentionsFound(Dictionary<ulong, List<DiscordMessage>> warnDict, DiscordChannel channel)
        {
            foreach (ulong snowflake in warnDict.Keys)
            {   // Iterate through each user snowflake logged in the dictionary.

                // Only make an embed if there are actually mentions.
                if (warnDict[snowflake].Count > 0)
                {
                    var mentionList = warnDict[snowflake];

                    var embed = new DiscordEmbedBuilder();
                    var stringBuilder = new StringBuilder();

                    // Build header.
                    stringBuilder.Append($"__**{Generics.GetMention(snowflake)} has had {mentionList.Count + 1} total mention{(mentionList.Count == 1 ? String.Empty : @"s")} (including the most recent one)" +
                        $"in {channel.Mention} in the last {Program.Settings.MaxActionAgeMonths} months:**__\n");

                    // Build link list.
                    stringBuilder.AppendJoin(' ',
                        Generics.BuildLimitedLinkList(
                            links: mentionList
                                .Select(a => Generics.GetMessageUrl(a))
                                .ToArray(),
                            endMessage: @"... Too many to display...",
                            maxLength: 2000 - stringBuilder.Length));

                    embed.WithDescription(stringBuilder.ToString());
                    embed.WithTitle($"Previous mention{(mentionList.Count == 1 ? String.Empty : @"s")} found");
                    embed.WithColor(DiscordColor.Red);

                    await channel.SendMessageAsync(embed: embed);
                } // end if
            } // end foreach
        }

        /// <summary>For searching mentions.</summary>
        static readonly Regex MentionRegex = new Regex(@"<@!?(\d{17,})>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>Use Regex to get mentions in a message.</summary>
        /// <returns>A list of found mentions.</returns>
        private static List<ulong> GetMessageMentions(string message)
        {
            var matches = MentionRegex.Matches(message);
            List<ulong> mentions_returnVal = new List<ulong>();

            foreach (Match match in matches)
            {
                // ulong id - The possible snowflake ID that was found.
                if (ulong.TryParse(match.Groups[1].Value, out ulong id) &&
                    !mentions_returnVal.Contains(id))
                {   // Only continue if this is absolutely a ulong id, and if it's not a duplicate.

                    mentions_returnVal.Add(id);

                }
            }

            return mentions_returnVal;
        }

        /// <summary>Query for all mentions of specific members.</summary>
        /// <param name="members">The members to query for.</param>
        /// <param name="actionChannel">#action-logs</param>
        /// <param name="warnThreshold">How long ago to query in months.</param>
        /// <param name="originalMessage">The original message.</param>
        /// <returns></returns>
        private static async Task<Dictionary<ulong, List<DiscordMessage>>>
            QueryMemberMentions(List<ulong> memberIds,
                                DiscordChannel actionChannel,
                                int warnThreshold,
                                DiscordMessage originalMessage)
        {
            const int MESSAGE_COUNT = 2000;
            // Remember to use DTO in the current timezone and not in UTC! API is running on OUR time.
            DateTimeOffset startTime = DateTimeOffset.Now.AddMonths(warnThreshold * -1);

            // We want to set the initial messages.

            IReadOnlyList<DiscordMessage> messages;

            if (originalMessage.ChannelId != Program.Settings.ActionChannelId)
            {   // If the original message is anywhere besides the action channel, we want to start at the most recent message.
                messages = await actionChannel.GetMessagesAsync(MESSAGE_COUNT);
            }
            else
            {   // If the original message is in the action channel, we want to exclude the message that triggered this response, as to not count it.
                messages = await actionChannel.GetMessagesBeforeAsync(originalMessage.Id, MESSAGE_COUNT);
            }

            // We want a "stop" value, so to speak. If this is true, it means we've gone before startTime.
            bool exceededStartTime = false;

            // Every instance where this user has been mentioned.
            var warnInstances = new Dictionary<ulong, List<DiscordMessage>>();

            // Populate the dictionary.
            memberIds.ForEach(a => warnInstances.Add(a, new List<DiscordMessage>()));

            do
            {
                if (messages.Count > 0)
                {
                    foreach (var message in messages)
                    {   // For each message, we want to check its mentioned users.

                        if (startTime.ToUnixTimeMilliseconds() <= message.CreationTimestamp.ToUnixTimeMilliseconds())
                        {   // We only want to continue if this is after our startValue.

                            foreach (ulong memberId in memberIds)
                            {
                                if (MentionedUsersContains(message, memberId))
                                {   // Only continue if there are actually mentioned users, and if the mentioned users has the member we want.

                                    warnInstances[memberId].Add(message);

                                } // end if
                            } // end foreach
                        } // end if
                        else
                        {   // We've gone way too far back, so we need to stop this.
                            exceededStartTime = true;
                            break;  // Break out of the foreach. NON-SESE ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! !
                        } // end else
                    } // end foreach

                    if (!exceededStartTime && messages.Count == MESSAGE_COUNT)
                    {   // Only repopulate if we're still within the time range AND if there are still messages to grab. My logic behind checking the
                        // message count is that there should be 2,000 or more messages if there's still messages to be found, unless the message
                        // count is exactly a multiple of 2,000.
                        messages = await actionChannel.GetMessagesBeforeAsync(messages.Last().Id, MESSAGE_COUNT);

                        // Give the bot some time to process.
                        await Task.Delay(500);
                    }
                    else
                    {   // Stop the loop.
                        exceededStartTime = false;
                        messages = default;
                    }// end else
                } // end if
            } while (!exceededStartTime && !(messages is null) && messages.Count > 0);
            // ^ Only continue if we haven't gone back far enough in history AND if we still have messages.

            return warnInstances;
        }

        /// <summary>Checks if a message contains the specified user.</summary>
        private static bool MentionedUsersContains(DiscordMessage message, ulong memberId)
        {
            bool returnVal = false;

            if (message.MentionedUsers.Count() > 0)
            {
                foreach (var user in message.MentionedUsers)
                {
                    if (returnVal)
                    {
                        break;// NON-SESE ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! 
                    }

                    if (user is null)
                    {   // Most likely they've left, so let's try to search the string with a regex.

                        Regex searchRegex = new Regex($"<@!{memberId}>", RegexOptions.IgnoreCase);

                        returnVal = searchRegex.IsMatch(message.Content);
                    }
                    else
                    {   // They haven't left, we can access their ID.
                        returnVal = user.Id == memberId;
                    } // end else
                } // end foreach
            } // end if

            return returnVal;
        } // end method
    }
}
