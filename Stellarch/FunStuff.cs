// FunStuff.cs
// Because I can.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace BigSister
{
    public static class FunStuff
    {
        protected class ResponseInfo
        {
            /// <summary>Regex string to search chat messages for.</summary>
            public readonly string RegexString;
            /// <summary>Response message.</summary>
            public readonly string ResponseString;
            /// <summary>Global cooldown in milliseconds.</summary>
            public readonly ulong CooldownMilliseconds;
            /// <summary>The bot state that this response sets.</summary>
            public readonly string BotState;
            /// <summary>The bot state that this response requires to trigger.</summary>
            public readonly string RequiredBotState;

            /// <summary>If the bot has a state it sets.</summary>
            public readonly bool HasBotState;
            /// <summary>If the bot requires a state to be previously set.</summary>
            public readonly bool RequiresBotState;
            /// <summary>The regex object to search strings with.</summary>
            public readonly Regex Regex;

            /// <summary>The last time this was triggered.</summary>
            public DateTimeOffset LastTrigger;

            public ResponseInfo(ResponseJson jsonData)
            {
                RegexString = jsonData.RegexString;
                ResponseString = jsonData.Response;
                CooldownMilliseconds = jsonData.Cooldown;
                BotState = jsonData.BotState ?? String.Empty;
                RequiredBotState = jsonData.RequiresBotState ?? String.Empty;

                HasBotState = BotState.Length > 0;
                RequiresBotState = RequiredBotState.Length > 0;

                Regex = new Regex(RegexString, RegexOptions.IgnoreCase | RegexOptions.Multiline);

                LastTrigger = DateTimeOffset.MinValue;
            }

            public bool CanTrigger()
            {
                bool @return;

                // Check if it's the default value.
                if (LastTrigger.Equals(DateTimeOffset.MinValue) || CooldownMilliseconds == 0)
                {   // Default value meaning we can trigger the bot.
                    @return = true;
                }
                else
                {   // It's something else, so let's check if we can trigger it.

                    DateTimeOffset dtoNow = DateTimeOffset.Now;
                    DateTimeOffset dtoCooldownTime = LastTrigger.AddMilliseconds(CooldownMilliseconds);

                    // Check if we've gone beyond the cooldown time.
                    if (dtoNow.ToUnixTimeMilliseconds() >= dtoCooldownTime.ToUnixTimeMilliseconds())
                    {
                        @return = true;

                        LastTrigger = DateTimeOffset.MinValue;
                    }
                    else
                    {
                        @return = false;
                    }
                }

                return @return;
            }
        }

        protected class ResponseJson
        {
            public string RegexString;
            public string Response;
            public ulong Cooldown;
            public string BotState;
            public string RequiresBotState;
        }

        protected class FunMessage
        {
            public DiscordEmoji Emoji;
            public string Message;
            public TimeSpan MinTime = TimeSpan.FromMinutes(2);
            public TimeSpan MaxTime = TimeSpan.FromMinutes(4);
        }

        static readonly List<FunMessage> funMessages = new List<FunMessage>
        {

            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"🧹"), Message = "Cleaning up the grounds.", MinTime = TimeSpan.FromMinutes(1), MaxTime = TimeSpan.FromMinutes(3) },
            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"🧹"), Message = "Cleaning up the grounds.", MinTime = TimeSpan.FromMinutes(1), MaxTime = TimeSpan.FromMinutes(3) }, 
            // Weight of 2x. 

            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"🐇"), Message = "Doing bunny things" },
            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"🗡️"), Message = "Taking care of raiders." },
            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"🎯"), Message = "Spending time in the rec room..." },
            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"☁️"), Message = "Cloudwatching..." },
            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"🕒"), Message = "Wandering..." },
            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"💀"), Message = "Disposing of raider bodies."},
            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"🥗"),  Message = "Eating some vegetable medley."},
            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"💓"), Message = "Healing after a raid."},
            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"🩺"),  Message = "Healing a colonist."},
            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"👺"),  Message = "Having a mental break!"},
            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"🔬"),  Message = "....Researching....", MinTime = TimeSpan.FromMinutes(5), MaxTime= TimeSpan.FromMinutes(7)},
            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"🦾"),  Message = "Installing bionics", MinTime = TimeSpan.FromMinutes(1), MaxTime = TimeSpan.FromMinutes(2)},
            new FunMessage { Emoji = DiscordEmoji.FromUnicode(@"😀"),  Message = "Repopulating...", MinTime = TimeSpan.FromMinutes(1), MaxTime = TimeSpan.FromMinutes(2)},
        };

        static readonly Random ran = new Random(Environment.TickCount);
        static DateTimeOffset NextTimeChange = DateTimeOffset.UtcNow;

        static ResponseInfo[] responses;
        static readonly HashSet<string> botStates = new HashSet<string>();

        public static bool LoadFunStuffJson(string file)
        {
            bool @return;
            string fileContents;

            // Check if the file exists.
            if (File.Exists(file))
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    fileContents = sr.ReadToEnd();
                }

                // Check if we got anything.
                if (fileContents.Length > 0)
                {
                    ResponseJson[] json = JsonConvert.DeserializeObject<ResponseJson[]>(fileContents);

                    // Check if json deserialized.
                    responses = new ResponseInfo[json.Length];

                    for (int i = 0; i < responses.Length; i++)
                    {
                        responses[i] = new ResponseInfo(json[i]);
                    }

                    @return = true;
                }
                else
                {
                    @return = false;
                }
            }
            else
            {
                @return = false;
            }
            return @return;
        }
        internal static async Task BotClient_Heartbeated(DiscordClient botClient, HeartbeatEventArgs e)
        {
            if (Program.Settings.FunAllowed)
            {
                if (DateTimeOffset.UtcNow.UtcTicks >= NextTimeChange.UtcTicks)
                {   // If it's change time, let's change.
                    FunMessage newStatus = funMessages[ran.Next(0, funMessages.Count - 1)];

                    try
                    {
                        await botClient.UpdateStatusAsync
                            (
                                new DiscordActivity($"{newStatus.Emoji} {newStatus.Message}", ActivityType.Playing),
                                UserStatus.Online
                            );
                    }
                    catch { }

                    // Update next time.

                    TimeSpan nextUpdate =
                        TimeSpan.FromMilliseconds(newStatus.MinTime.TotalMilliseconds + (ran.NextDouble() * newStatus.MaxTime.TotalMilliseconds));

                    NextTimeChange = NextTimeChange.Add(nextUpdate);
                }
            }
        }

        internal static async Task BotClientMessageCreated(DiscordClient botClient,MessageCreateEventArgs e)
        {
            // Make sure fun is allowed and that this isn't a PM or the bot itself.
            if (Program.Settings.FunAllowed &&
               !e.Channel.IsPrivate &&
               !e.Author.IsBot)
            {
                DiscordMember self = await e.Guild.GetMemberAsync(botClient.CurrentUser.Id);

                // Make sure we actually have the permissions to send messages in this channel before doing anything.
                if (e.Channel.PermissionsFor(self).HasPermission(DSharpPlus.Permissions.SendMessages))
                {
                    bool regexFound = false;

                    ResponseInfo responseInfo = null;

                    // Search through every regex until we find something.
                    for (int i = 0; i < responses.Length && !regexFound; i++)
                    {
                        ResponseInfo a = responses[i];
                        Match m = a.Regex.Match(e.Message.Content);

                        if (m.Success)
                        {
                            // Only continue if...
                            //  1) This response can trigger.
                            //  2) This response has...
                            //      a) No required state.
                            //      b) A required state but it's already been fulfilled.
                            if (a.CanTrigger() && // Case 1
                               (!a.RequiresBotState || (a.RequiresBotState && botStates.Contains(a.RequiredBotState)))) // Case 2
                            {
                                responseInfo = a;
                                regexFound = true;
                            } // end if
                        } // end if
                    } // end for

                    // Now that we've searched through every regex, let's continue only if we've found a regex match.
                    if (regexFound && !(responseInfo is null))
                    {   // We can, but now let's check if the response has a trigger condition.

                        // So at this point, we know the bot has a fulfilled state (or doesn't have any, period), so let's send the message.
                        await e.Channel.SendMessageAsync(content: 
                            String.Format(responseInfo.ResponseString,
                            arg0: e.Author.Mention));

                        var dtoNow = DateTimeOffset.Now;

                        // Set the last triggered time to now.
                        responseInfo.LastTrigger = dtoNow;

                        // If the bot has a bot state let's also set it.
                        if (responseInfo.HasBotState)
                        {
                            botStates.Add(responseInfo.BotState);
                        }

                        // If the bot requires a state, let's unset it if we've made it to this point.
                        if (responseInfo.RequiresBotState)
                        {
                            botStates.Remove(responseInfo.RequiredBotState);
                        } // end if
                    } // end if
                } // end if
            } // end if
        } // end method
    }
}