// Generics.cs
// Contains generic chat objects and methods for generating templates.
//


using System;
using System.Linq;
using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

namespace BigSister.ChatObjects
{
    public static class Generics
    {
        const string EMPTY_STRING = @"";

        public const string NEGATIVE_PREFIX = @":x: **/(u︵u)\\**";
        public const string POSITIVE_PREFIX = @":white_check_mark: **/(^‿^)\\**";
        public const string NEUTRAL_PREFIX = @":speech_left: **/('▿')\\**";

        public static DiscordColor PositiveColor = DiscordColor.Green;
        public static DiscordColor NegativeColor = DiscordColor.Red;
        public static DiscordColor NeutralColor = DiscordColor.MidnightBlue;

        public const string URL_SPEECH_BUBBLE = @"https://i.imgur.com/o304dw9.png";
        public const string URL_FILTER_ADD = @"https://i.imgur.com/yrOMSae.png";
        public const string URL_FILTER_SUB = @"https://i.imgur.com/jMcIUkj.png";
        public const string URL_FILTER_BUBBLE = @"https://i.imgur.com/RxsCYFc.png";
        public const string URL_FILTER_GENERIC = @"https://i.imgur.com/dePUaat.png";
        public const string URL_REMINDER_GENERIC = @"https://i.imgur.com/FcpwjEm.png";
        public const string URL_REMINDER_EXCLAIM = @"https://i.imgur.com/4BmoFjQ.png";
        public const string URL_REMINDER_DELETED = @"https://i.imgur.com/IqG56C4.png";

        public const string DateFormat = @"yyyy-MMM-dd HH:mm";

        public static PaginationEmojis DefaultPaginationEmojis = new PaginationEmojis
        {
            Left = DiscordEmoji.FromName(Program.BotClient, @":arrow_backward:"),
            Right = DiscordEmoji.FromName(Program.BotClient, @":arrow_forward:"),
            Stop = DiscordEmoji.FromName(Program.BotClient, @":stop_button:"),
            SkipLeft = null,
            SkipRight = null
        };


        public static string GetMessageUrl(DiscordMessage message)
            => GetMessageUrl((ulong) message.Channel.GuildId, message.ChannelId, message.Id);
        public static string GetMessageUrl(ulong guildId, ulong channelId, ulong messageId)
            => $"https://discord.com/channels/{guildId}/{channelId}/{messageId}";


        /// <summary>Gets a mention string from a snowflake.</summary>
        public static string GetMention(ulong id) => $"<@!{id}>";
        /// <summary>Gets a mention string from a snowflake.</summary>
        public static string GetMention(string id) => $"<@!{id}>";

        /// <summary>Gets a mention string for a channel.</summary>
        public static string GetChannelMention(ulong id) => $"<#{id}>";

        /// <summary>A generic embed template.</summary>
        public static DiscordEmbedBuilder GenericEmbedTemplate(DiscordColor? color,
                                                               string description = EMPTY_STRING,
                                                               string thumbnail = EMPTY_STRING,
                                                               string title = EMPTY_STRING)
        {
            var embedBuilder = new DiscordEmbedBuilder();

            embedBuilder.WithColor(color ?? NeutralColor);

            if (description.Length > 0)
                embedBuilder.WithDescription(description);
            if (title.Length > 0)
                embedBuilder.WithTitle(title);

            embedBuilder.WithThumbnail(thumbnail.Length > 0 ? thumbnail : URL_SPEECH_BUBBLE);

            return embedBuilder;
        }

        /// <summary>A template for Floppy directly responding to a user.</summary>
        public static string PositiveDirectResponseTemplate(string mention, string body)
            => $"{POSITIVE_PREFIX} {mention}, {body}";

        /// <summary>A template for Floppy directly responding to a user.</summary>
        public static string NegativeDirectResponseTemplate(string mention, string body)
            => $"{NEGATIVE_PREFIX} {mention}, {body}";

        #region bunny
        //                     ,\
        //             \\\,_
        //              \` ,\
        //         __,.-" =__)
        //       ."        )
        //    ,_/   ,    \/\_
        //    \_|    )_-\ \_-`
        //jgs    `-----` `--`
        #endregion bunny

        /// <summary>A template for Floppy directly responding to a user.</summary>
        public static string NeutralDirectResponseTemplate(string mention, string body)
            => $"{NEUTRAL_PREFIX} {mention}, {body}";

        public static string ExceptionDirectResponseTemplate()
            => throw new NotImplementedException();

        /// <summary>Build a string that's limited to a certain length that caps with a specified message when it overflows.</summary>
        public static string BuildLimitedString(string originalString, string endMessage = @". . .", int maxLength = 2000)
        {
            string returnVal;

            // Check if we're over the maximum length.
            if (originalString.Length > maxLength)
            {   // We are over the max length so let's trim the string and cap it with the end message.
                returnVal = String.Concat(
                    originalString.Substring(0, length: maxLength - endMessage.Length),
                    endMessage);
            }
            else
            {   // We are not over the max length so let's just return the original string.
                returnVal = originalString;
            }

            return returnVal;
        }

        /// <summary>Build a series of links that terminates gracefully after a certain length.</summary>
        /// <param name="links">List of links</param>
        /// <param name="endMessage">Message to display if the link list cannot be completed.</param>
        /// <param name="maxLength">Maximum length of list, including error message.</param>
        /// <returns></returns>
        public static string BuildLimitedLinkList(string[] links, string endMessage, int maxLength = 2000)
        {
            string returnVal;

            const string baseUriString = @"#{0}";

            // Check if we've even been given anything AND if we can even default to an error-only message.
            if (endMessage.Length <= maxLength)
            {   // Yeah, we have something to work with.
                // A list where each item progressively gets larger. Item 0 will be the size of one link. Item 1 will be the size of one space,
                // Item 0, and another link. So forth.
                int[] totalLinkListSize = new int[links.Length];
                string[] maskedLinks = new string[links.Length];

                // The index found where a link would cause the string to be larger than the maxLength limit.
                int linkTooBigIndex = -1;
                bool fullListNotPossible = false;

                // The number of links to take and put into the link list string. Default to 0 so we can return a blank string if any edge-case 
                // errors occur.
                int linksToTake = 0;

                // Turn every provided link into a masked link. Stop if we find a link that will make the list too large.
                for (int i = 0; i < links.Length && linkTooBigIndex == -1; i++)
                {
                    // Get a masked link.
                    string maskedLink = Formatter.MaskedUrl(
                                       text: String.Format(baseUriString, i + 1),
                                       url: new Uri(links[i]));

                    maskedLinks[i] = maskedLink;

                    // Firstly, make the size of this field the length of the masked link.
                    int curSize = maskedLink.Length;

                    // Secondly, let's add the length of a prepended if this is not the first entry. Because we're also tracking the link list 
                    // additively, let's also add the previous size (if there's even anything before this link).
                    curSize += (i > 0 ? 1 + totalLinkListSize[i - 1] : 0);

                    // Firstly, let's make the size of this field the size of the masked link.
                    totalLinkListSize[i] = curSize;

                    // Check if this link will cause the link list string to be too big.
                    if (curSize > maxLength)
                    {   // It's going to cause the link list string to be far too big.
                        linkTooBigIndex = i;
                        fullListNotPossible = true;
                    }
                }

                // Check if we ever found a link that will make the link list string too large.
                if (fullListNotPossible)
                {   // We did, and now we need to find out how many links we can take.

                    // Variable to stop the for loop.
                    bool continueLoop = true;

                    // Start looking for the last link we can take starting from the end moving towards the start of the array.
                    for (int i = linkTooBigIndex; i >= 0 && continueLoop; i--)
                    {
                        // Let's see if we can take the current link and pair it with the endMessage with a space between. If we can do this, we can
                        // terminate the string gracefully with as many links as we can with the string capped with the endMessage.
                        if (totalLinkListSize[i] + 1 + endMessage.Length <= maxLength)
                        {   // Yes, the current list size plus an end message is under our max length.
                            linksToTake = i + 1;  // Let's set the number of links we're going to take.
                            continueLoop = false; // Let's also let the loop know we want to stop now.
                        } // end if
                    } // end for
                }  // end if
                else
                {   // We didn't and we can take all of the links.
                    linksToTake = links.Length;
                }

                // Great. We've found exactly how many links we can take and determined if we need to cap the string with an end message. Now let's
                // actually build the string.

                var stringBuilder = new StringBuilder();

                stringBuilder.AppendJoin(' ', maskedLinks.Take(linksToTake));

                if (fullListNotPossible)
                {   // If a full listing was not possible, let's cap the message with the end message.
                    stringBuilder.Append(' ');
                    stringBuilder.Append(endMessage);
                }

                returnVal = stringBuilder.ToString();
            }
            else
            {   // We do not have anything to work with, so let's set the string to empty.
                returnVal = String.Empty;
            }

            return returnVal;
        } // end method


        const string HEARTBEAT_TRIGGER = "Should trigger at next heartbeat...";

        /// <summary>Get the remaining time.</summary>
        /// <returns>A string representing how much time is left.</returns>
        public static string GetRemainingTime(DateTimeOffset dto, bool timeInFuture = true, string recentMsg = HEARTBEAT_TRIGGER, string capMsg = @"left")
        {
            var dtoNow = DateTimeOffset.UtcNow;
            string returnVal;

            if ((timeInFuture && dtoNow.ToUnixTimeMilliseconds() >= dto.ToUnixTimeMilliseconds()) ||
                 (!timeInFuture && dtoNow.ToUnixTimeMilliseconds() <= dto.ToUnixTimeMilliseconds()))
            {   // Check if this should be triggering already.
                returnVal = recentMsg;
            }
            else
            {   // No, it's not triggering that soon.
                TimeSpan remainingTime;

                if (timeInFuture)
                {   // This is some time in the future.
                    remainingTime = dto.UtcDateTime - dtoNow;
                }
                else
                {   // This is some time in the past.
                    remainingTime = dtoNow - dto.UtcDateTime;
                }
                var stringBuilder = new StringBuilder();

                // If anything has been added to the time string.
                bool timeAdded = false;

                // Add days
                if (remainingTime.Days > 0)
                {
                    stringBuilder.Append(remainingTime.Days);
                    stringBuilder.Append(" days ");

                    timeAdded = true;
                }

                // Add hours
                if (remainingTime.Hours > 0)
                {
                    stringBuilder.Append(remainingTime.Hours);
                    stringBuilder.Append(" hours ");

                    timeAdded = true;
                }

                // Add minutes
                if (remainingTime.Minutes > 0)
                {
                    stringBuilder.Append(remainingTime.Minutes);
                    stringBuilder.Append(" minutes ");

                    timeAdded = true;

                }

                // Add seconds
                if (remainingTime.Seconds > 0)
                {
                    stringBuilder.Append(remainingTime.Seconds);
                    stringBuilder.Append(" seconds ");

                    timeAdded = true;
                }

                if (timeAdded)
                {   // Time was added. We want to check if time was added on the off-chance this is called when there are milliseconds or ticks left.
                    // We haven't checked for those two units so it could cause a malformed string. Reason I don't want to check for those two is
                    // because it'll make the string longer than it really should be for any regular user.
                    stringBuilder.Append(capMsg);
                    stringBuilder.Append('.');
                }
                else
                {   // No time added. Let's let the user know that, basically, it SHOULD trigger next heartbeat. In the case it doesn't trigger on 
                    // the immediately next heartbeat, then it should trigger the next one. The user won't know this, so being super accurate doesn't
                    // matter here.
                    stringBuilder.Clear();
                    stringBuilder.Append(recentMsg);
                }

                returnVal = stringBuilder.ToString();
            }

            return returnVal;
        }
    }
}
