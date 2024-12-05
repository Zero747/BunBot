// FilterSystem.Filtering.cs
// A portion of the filter system containing everything needed for processing commands from FilterCommands.cs
// 1) Receives all message creation/edit events and filters them.
// 2) Raises events whenever the filter is triggered.
//


using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace BigSister.Filter
{
    public static partial class FilterSystem
    {
        /// <summary>Invoked when the filter is triggered.</summary>
        public static event FilterTriggeredEventHandler FilterTriggered;
        public delegate Task FilterTriggeredEventHandler(FilterEventArgs e);

        // Disable compiler warning for async code not having await. DSharpPlus demands that these two methods be an awaitable type
        // but throwing them into the ether with a task.run should prevent the handler from timing out
#pragma warning disable CS1998

        internal static async Task BotClient_MessageCreated(DiscordClient botClient, MessageCreateEventArgs e)
        {
            // Only continue if the channel isn't excluded, if the sender isn't the bot, and if this isn't sent in PMs.
            if(!Program.Settings.ExcludedChannels.Contains(e.Channel.Id) && 
               !e.Channel.IsPrivate &&
               !e.Author.IsCurrent &&
                (e.Message.MessageType == MessageType.Default || e.Message.MessageType == MessageType.Reply))
            {
                //CheckMessage(e.Message);
                _ = Task.Run(() => CheckMessage(e.Message)).ConfigureAwait(false);
            }
        }
        internal static async Task BotClient_MessageUpdated(DiscordClient botClient, MessageUpdateEventArgs e)
        {
            // Only continue if the channel isn't excluded, if the sender isn't the bot, if this isn't sent in PMs, if this wasn't due to an embed, and if this isn't due to a
            // system messages e.g. message pinned, member joins, 
            if (!Program.Settings.ExcludedChannels.Contains((ulong)(e.Channel.IsThread ? e.Channel.ParentId : e.Channel.Id)) &&
               !e.Channel.IsPrivate &&
               !e.Author.IsCurrent &&
                (e.Message.MessageType == MessageType.Default || e.Message.MessageType == MessageType.Reply) && (e.MessageBefore.Content != e.Message.Content))
            {
                _ = Task.Run(() => CheckMessage(e.Message)).ConfigureAwait(false);
            }
        }

#pragma warning restore CS1998


        /// <summary>Check the message against the filter.</summary>
        private static void CheckMessage(DiscordMessage message)
        {
            // Let's check if the audit channel is set.
            if (Program.Settings.FilterChannelId != 0)
            {

                var badWords = GetBadWords(message.Content, 
                    out 
                    string notatedMessage); // The detected bad words.

                if (badWords.Count > 0)
                {
                    OnFilterTriggered(
                        new FilterEventArgs
                        {
                            Message = message,
                            Channel = message.Channel,
                            User = message.Author,
                            BadWords = badWords.ToArray(),
                            NotatedMessage = notatedMessage
                        });
                }
            }
        }

        /// <summary>Get all the bad words in a message.</summary>
        /// <param name="message">The message to search.</param>
        /// <param name="notatedMessage">A string to notate, emphasizing where the bad words are.</param>
        public static List<string> GetBadWords(string message, out string notatedMessage)
        {
            var returnVal = new List<string>(); // Our sentinel value for no bad word is an empty List<string>.

            Regex ansiRegex = new Regex($"\u001b\\[[\\d;]+m", RegexOptions.IgnoreCase);
            message = ansiRegex.Replace(message, ""); // This just strips all ansi color codes from the message before we add our own

            if (MaskCache.Length > 0)
            {
                foreach (Regex regexPattern in FilterRegex)
                {
                    MatchCollection mc = regexPattern.Matches(message);

                    if (mc.Count > 0)
                    {
                        int annoteSymbolsOffset = 0; // The amount a position should be shifted per text insertion operation.
                        var matches = new List<Match>(mc.ToList()); // This is so we get a match list sorted based on the position of the matching text, this prevents annotation issues such as "ma%tched text  %"
                        // Let's check every bad word
                        for (int i = 0; i < matches.Count; i++)
                        {
                            Match match = matches[i];
                            string badWord = match.Value;
                            int badWordIndex = match.Index;

                            if (!IsExcluded(message, badWord, badWordIndex))
                            {
                                returnVal.Add(badWord);
                                message = message.Insert(badWordIndex + annoteSymbolsOffset, "\u001b[4;35m");
                                annoteSymbolsOffset += 7;
                                message = message.Insert(badWordIndex + badWord.Length + annoteSymbolsOffset, "\u001b[0m");
                                annoteSymbolsOffset += 4; // This all ensures that the ansi color codes are properly added to the correct position in text
                            } // end if
                        } // end for
                    } // end if
                } // end foreach
            } // end if

            notatedMessage = message;
            notatedMessage = Regex.Replace(notatedMessage, "\\p{Cf}", ""); // Some people deliberately toss in control characters which can massively increase character count without taking up any space, this should hopefully remove a lot of the control characters being abused
            // If the notated message is over 950 characters, let's cut it down a little bit. ANSI color coding does not work above 1000 characters, and the following additional text is 48 characters
            if (notatedMessage.Length > 950)
            {
                notatedMessage = $"{notatedMessage.Substring(0, 950)}\u001b[2;31m...\nMessage is too long to preview.\u001b[0m";
            }

            return returnVal;
        }

        // When the filter is triggered.
        static void OnFilterTriggered(FilterEventArgs e)
        {
            FilterTriggeredEventHandler handler = FilterTriggered;
            handler?.Invoke(e);
        }
    }
}
