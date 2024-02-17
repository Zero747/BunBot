// MuteSystem.cs
// A portion of the mute system containing everything needed for processing mutes from MuteCommands.cs
//  

//TODO general format cleanup

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Data.Sqlite;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using BigSister.ChatObjects;
using BigSister.Database;
using System.Runtime.InteropServices.ComTypes;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Net.Models;

namespace BigSister.Mutes
{
    public static partial class MuteSystem
    {
        /// <summary>Query to add a mute to the database.</summary>
        const string QQ_AddMute = @"INSERT INTO `Mutes` (`Id`, `UserId`, `Message`, `TriggerTime`, `Guild`) 
                                         VALUES ($id, $userid, $message, $time, $guild);";
        /// <summary>Query to remove a mute from the database.</summary>
        const string QQ_RemoveMute = @"DELETE FROM `Mutes` WHERE `Id`=$id;";
        /// <summary>Query to check if a mute exists.</summary>
        const string QQ_MuteExists = @"SELECT EXISTS(SELECT 1 FROM `Mutes` WHERE `Id`=$id);";
        /// <summary>Query to read the entire mute table.</summary>
        const string QQ_ReadTable = @"SELECT `Id`, `UserId`, `Message`, `TriggerTime`, `Guild` FROM `Mutes`;";
        /// <summary>Query to return all mutes that need to be triggered.</summary>
        const string QQ_CheckMutesElapsed = @"SELECT `Id`, `UserId`, `Message`, `TriggerTime`, `Guild` 
                                                  FROM `Mutes` WHERE `TriggerTime` <= $timenow;";
        /// <summary>Query to delete all mute that need to be triggered.</summary>
        const string QQ_DeleteMutesElapsed = @"DELETE FROM `Mutes` WHERE `TriggerTime` <= $timenow;";
        /// <summary>Query to get a single mute from a list.</summary>
        const string QQ_GetMuteFromId = @"SELECT `Id`, `UserId`, `Message`, `TriggerTime`, `Guild` FROM `Mutes` WHERE `Id`=$id;";
        /// <summary>Query to check if any mutes exist for a given user.</summary>
        const string QQ_UserMuteExists = @"SELECT EXISTS(SELECT 1 FROM `Mutes` WHERE `UserId`=$userid and `Guild` =$guild);";
        /// <summary>Query to remove mutes for a given user</summary>
        const string QQ_UserRemoveMute = @"DELETE FROM `Mutes` WHERE `UserId`=$userid and `Guild` =$guild;";
        /// <summary>Query to check mute duration for a user. There should only be one mute
        const string QQ_GetUserMute = @"SELECT `Id`, `UserId`, `Message`, `TriggerTime`, `Guild` FROM `Mutes` WHERE `UserId`=$userid and `Guild` =$guild;";

        #region MuteCommands.cs

        /// <summary>The date recognition Regex.</summary>
        // It groups every number/time unit pairing "(number)+(time unit)" into a group. 
        static readonly Regex DateRegex
            = new Regex(@"(\d+)\s?(months?|days?|d|weeks?|wks?|w|hours?|hrs?|h|minutes?|mins?|m)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
        public static async Task AddMute(CommandContext ctx, DiscordMember targetUser, string args)
        {   // Firstly get all the matches.
            MatchCollection regexMatches = DateRegex.Matches(args);
            BitArray regexCoverage = new BitArray(args.Length);
            var dto = ctx.Message.CreationTimestamp;

            // String processing - find the message and get the mute end date. 
            // To find what's not a date, we simply look for the first character that isn't in the boundaries of a Regex match. 
            //
            // Structure of a possible string:
            //
            //             Date String | Message String
            //  DATE, DATE, DATE, DATE | message
            //
            // Beyond the Date String we want to stop processing time information as people may reference time in the message string, so we don't
            // erronously want that data added to the date string.


            // Populate the regexCoverage...
            foreach (Match match in regexMatches)
            {
                for (int i = match.Index; i < match.Index + match.Length; i++)
                {
                    regexCoverage[i] = true;
                }
            }

            // So I want to explain what I'm about to do here. Every value in regexCoverage[] indicates if that's part of the initial time
            // string. We want to use it to determine if something is a message or a time value, so if at any point, we run into something that
            // isn't a time string, we want to set every instance thereafter as false so we know it's part of a message.
            if (regexMatches.Count > 0)
            {
                bool value = regexCoverage[0];
                for (int k = 1; k < regexCoverage.Count; k++)
                {
                    if (!IsWhitespace(args[k]))
                    {
                        if (!regexCoverage[k] && value)
                        {
                            value = false;
                        }

                        if (!value)
                        {
                            regexCoverage[k] = value;
                        }
                    }
                }
            }
            // We need to figure out where the date string ends.
            string messageString = String.Empty;

            int dateEndIndex = 0;
            bool messageFound = false;
            while (dateEndIndex < regexCoverage.Length && !messageFound)
            {
                char stringChar = args[dateEndIndex];
                bool inRegexBoundaries = regexCoverage[dateEndIndex];

                // This checks to see if the character is non-white-space and outside of any RegEx boundaries.
                messageFound = !IsWhitespace(stringChar) && !inRegexBoundaries;

                // If not found, continue; otherwise, keep incrementing.
                if (!messageFound)
                {
                    dateEndIndex++;
                }
            }

            // If we aren't going out of bounds, let's set the string to this.
            if (dateEndIndex < regexCoverage.Length)
            {
                messageString = args.Substring(dateEndIndex);
            }

            // Get date information
            foreach (Match match in regexMatches)
            {
                // Only try to exclude Message String date information if a message string was found.
                if (!messageFound || (regexCoverage[match.Index] && regexCoverage[match.Index + match.Length - 1]))
                {
                    InterpretTime(match.Groups[1].Value, match.Groups[2].Value, ref dto);
                }
            }

            // Get mention
            //mention = targetUser.Id;
            

            // At this point, now we have the DateTimeOffset describing when this mute needs to be set off, and we have a message string if
            // any. So now we just need to make sure it's within reasonable boundaries, set the mute, and notify the user.

            DateTimeOffset maxtime = new DateTimeOffset(ctx.Message.CreationTimestamp.UtcDateTime).AddDays(Program.Settings.MaxMuteTimeDays);
            DiscordEmbedBuilder embed;

            bool sendErrorEmbed = false;

            if (dto.UtcTicks == ctx.Message.CreationTimestamp.UtcTicks)
            {   // No time was added.

                embed = Generics.GenericEmbedTemplate(
                        color: Generics.NegativeColor,
                        description: Generics.NegativeDirectResponseTemplate(
                            mention: ctx.Member.Mention,
                            body: @"I was unable able to add the mute you gave me. You didn't supply me a valid time. The syntax is mute <mention> <time> <reason>."),
                        title: @"Unable to add mute",
                        thumbnail: Generics.URL_MUTE_GENERIC
                    );

                sendErrorEmbed = true;
            }
            else if (dto.UtcTicks > maxtime.UtcTicks)
            {   // More than our allowed time away.

                int maxDays = Program.Settings.MaxMuteTimeDays;

                embed = Generics.GenericEmbedTemplate(
                        color: Generics.NegativeColor,
                        description: Generics.NegativeDirectResponseTemplate(
                            mention: ctx.Member.Mention,
                            body: $"I was unable able to add the mute you gave me. That's more than {maxDays} day{(maxDays > 0 ? @"s" : String.Empty)} away..."),
                        title: @"Unable to add mute",
                        thumbnail: Generics.URL_MUTE_GENERIC
                    );

                sendErrorEmbed = true;
            }
            else
            {   // Everything is good in the world... except that the world is burning, but that's not something we're worried about here, for
                // now...

                //some flags to manage mute duration
                bool noTrack = false; //for when an existing mute is longer

                //this is really just here so the error embed bit doesn't yell at me
                embed = Generics.GenericEmbedTemplate(
                        color: Generics.PositiveColor,
                        description: Generics.PositiveDirectResponseTemplate(
                            mention: ctx.Member.Mention,
                            body: @"I added the mute you gave me! You shouldn't see this!"),
                        title: @"User muted",
                        thumbnail: Generics.URL_MUTE_GENERIC
                    );

                Mute mute = new Mute(
                    originalMessageId: ctx.Message.Id.ToString(),
                    text: messageString.Length.Equals(0) ? @"n/a" : messageString.ToString(),
                    time: (int)(dto.ToUnixTimeSeconds() / 60),
                    user: targetUser.Id,
                    guild: ctx.Guild.Id
                    );

                // add muted role to listed user, won't break anything if it's already there
                DiscordMember member = await ctx.Guild.GetMemberAsync(mute.User);
                DiscordRole role = ctx.Guild.GetRole(Program.Settings.MuteRoleID[mute.Guild]);
                await member.GrantRoleAsync(role);
                await member.ModifyAsync(delegate (MemberEditModel user)
                {
                    user.VoiceChannel = null;
                });

                //Lets check if there's a longer mute or not first
                Mute old_mute;
                string note = "";

                // Let's build the command.
                using var check_command = new SqliteCommand(BotDatabase.Instance.DataSource)
                {
                    CommandText = QQ_GetUserMute
                };

                SqliteParameter aa = new SqliteParameter("$userid", targetUser.Id)
                {
                    DbType = DbType.String
                };
                SqliteParameter bb = new SqliteParameter("$guild", ctx.Guild.Id)
                {
                    DbType = DbType.String
                };

                check_command.Parameters.AddRange(new SqliteParameter[] { aa, bb });

                // Get a single item from the list.
                // We're using a delegate that supposedly returns a list of mutes, but in this case it should only return one.
                old_mute = ((Mute[])await BotDatabase.Instance.ExecuteReaderAsync(check_command,
                        processAction: readMutes)).SingleOrDefault();

                // Check if it's default aka nothing found (for some reason)
                if (!old_mute.Equals(default(Mute)))
                {
                    // If we're here, there's an old mute
                    // We're overriding it, aka deleting any pre-existing ones
                    // Let's build the command.
                    using var remove_command = new SqliteCommand(BotDatabase.Instance.DataSource)
                    {
                        CommandText = QQ_UserRemoveMute
                    };

                    SqliteParameter aaa = new SqliteParameter("$userid", targetUser.Id)
                    {
                        DbType = DbType.String
                    };
                    SqliteParameter bbb = new SqliteParameter("$guild", ctx.Guild.Id)
                    {
                        DbType = DbType.String
                    };

                    remove_command.Parameters.AddRange(new SqliteParameter[] { aaa, bbb });

                    // and run it
                    await BotDatabase.Instance.ExecuteNonQuery(remove_command);
                    note = "\nNote: Overriding previous mute";

                }




                if (!noTrack) { 

                    // Let's build the command.
                    using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
                    {
                        CommandText = QQ_AddMute
                    };

                    SqliteParameter a = new SqliteParameter("$id", mute.OriginalMessageId.ToString())
                    {
                        DbType = DbType.String
                    };

                    SqliteParameter b = new SqliteParameter("$userid", mute.User)
                    {
                        DbType = DbType.String
                    };

                    SqliteParameter c = new SqliteParameter("$message", mute.Text)
                    {
                        DbType = DbType.String
                    };

                    SqliteParameter d = new SqliteParameter("$time", mute.Time)
                    {
                        DbType = DbType.Int32
                    };
                    SqliteParameter e = new SqliteParameter("guild", mute.Guild)
                    {
                        DbType = DbType.String
                    };


                    command.Parameters.AddRange(new SqliteParameter[] { a, b, c, d, e });

                    await BotDatabase.Instance.ExecuteNonQuery(command);
                }
                // Send the response.

                //redirect to action channel
                DiscordChannel sendChannel = await Program.BotClient.GetChannelAsync(Program.Settings.ActionChannelId);
                //make a message to report the action

                await sendChannel.SendMessageAsync(content: $"**Muted User**: {Generics.GetMention(mute.User)}\nStaff: {Generics.GetMention(ctx.User.Id)}\nRemaining time: {Generics.GetRemainingTime(dto)}\nReason: {mute.Text}"+note);
                

                //await sendChannel.SendMessageAsync(embed: embed); //formerly sending as an embed
            }

            if (sendErrorEmbed)
            {
                //send the error if one occured
                await ctx.Channel.SendMessageAsync(embed: embed);
                //var b = GenericResponses.HandleInvalidArguments(ctx);

                //await Task.WhenAll(a, b);
            }
        }

        /// <summary>Remove a mute if it exists.</summary>
        public static async Task RemoveMute(CommandContext ctx, Mute mute)
        {
            // It's a mute, so let's remove it.

            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_RemoveMute
            };

            SqliteParameter a = new SqliteParameter("$id", mute.OriginalMessageId.ToString())
            {
                DbType = DbType.String
            };

            command.Parameters.Add(a);

            // Now that we have the old mute, let's remove the old one from the database.
            await BotDatabase.Instance.ExecuteNonQuery(command);

            // Now let's respond.

            var discordEmbedBuilder = new DiscordEmbedBuilder(Generics.GenericEmbedTemplate(
                color: Generics.PositiveColor,
                description: Generics.PositiveDirectResponseTemplate(
                    mention: ctx.Member.Mention,
                    @"I was able to remove the mute you gave me!"),
                thumbnail: Generics.URL_MUTE_DELETED,
                title: @"Removed mute"));

            DateTimeOffset dto = DateTimeOffset.FromUnixTimeSeconds(mute.Time * 60); // The mute's DTO.
            TimeSpan remainingTime = dto.Subtract(DateTimeOffset.UtcNow); // The remaining time left for the mute.
            string originalAuthorMention = Generics.GetMention(mute.User);

            discordEmbedBuilder.AddField(@"User", originalAuthorMention, true);
            discordEmbedBuilder.AddField(@"Time (UTC)", dto.ToString(Generics.DateFormat), true);
            discordEmbedBuilder.AddField(@"Mute Identifier", mute.OriginalMessageId.ToString(), false);
            discordEmbedBuilder.AddField(@"Remaining time", Generics.GetRemainingTime(dto), false);
            discordEmbedBuilder.AddField(@"Message", mute.Text, false);

            // Send the response.
            await ctx.Channel.SendMessageAsync(embed: discordEmbedBuilder);
        }

        public static async Task RemoveUserMute(CommandContext ctx, DiscordMember user)
        {
           

            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_UserRemoveMute
            };

            SqliteParameter a = new SqliteParameter("$userid", user.Id)
            {
                DbType = DbType.String
            };
            SqliteParameter b = new SqliteParameter("$guild", ctx.Guild.Id)
            {
                DbType = DbType.String
            };

            command.Parameters.AddRange(new SqliteParameter[] { a, b });



            // and run it
            await BotDatabase.Instance.ExecuteNonQuery(command);

            if (ctx.Guild.Members.ContainsKey(user.Id))
            {
                DiscordRole role = ctx.Guild.GetRole(Program.Settings.MuteRoleID[ctx.Guild.Id]);
                await user.RevokeRoleAsync(role);
            }


            // Now let's respond.

            var discordEmbedBuilder = new DiscordEmbedBuilder(Generics.GenericEmbedTemplate(
                color: Generics.PositiveColor,
                description: Generics.PositiveDirectResponseTemplate(
                    mention: ctx.Member.Mention,
                    @"I removed any mutes on this user!"),
                thumbnail: Generics.URL_MUTE_DELETED,
                title: @"Removed mute"));

            discordEmbedBuilder.AddField(@"User", user.Mention, true);

            // Send the response.
            await ctx.Channel.SendMessageAsync(embed: discordEmbedBuilder);
        }

        /// <summary>Check if a provided ID is a mute.</summary>
        public static async Task<bool> IsMute(string id)
        {
            bool hasItem_returnVal;

            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_MuteExists
            };

            SqliteParameter a = new SqliteParameter("$id", id)
            {
                DbType = DbType.String
            };

            command.Parameters.Add(a);

            object returnVal = await BotDatabase.Instance.ExecuteReaderAsync(command,
                    processAction: delegate (SqliteDataReader reader)
                    {
                        object a;

                        if (reader.Read())
                        {   // Let's read the database.
                            a = reader.GetValue(0);
                        }
                        else
                        {
                            a = null;
                        }

                        return a;
                    });

            int returnValC;

            // Try to convert it to an int. If it throws an exception for some reason, chances are it's not what we're looking for.
            try
            {
                returnValC = Convert.ToInt32(returnVal);
            }
            catch
            {   // Probably not an int, so let's set the value to something we absolutely know will return as false.
                returnValC = -1;
            }

            // Let's get the return value by checking if the returnval == 1
            hasItem_returnVal = returnValC == 1;

            return hasItem_returnVal;
        }

        /// <summary>Check if a provided ID is a mute.</summary>
        public static async Task<bool> IsMutedUser(ulong id, ulong gid)
        {
            bool hasItem_returnVal;

            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_UserMuteExists
            };

            SqliteParameter a = new SqliteParameter("$userid", id)
            {
                DbType = DbType.String
            };
            SqliteParameter b = new SqliteParameter("$guild", gid)
            {
                DbType = DbType.String
            };

            command.Parameters.AddRange(new SqliteParameter[] { a, b});

            object returnVal = await BotDatabase.Instance.ExecuteReaderAsync(command,
                    processAction: delegate (SqliteDataReader reader)
                    {
                        object a;

                        if (reader.Read())
                        {   // Let's read the database.
                            a = reader.GetValue(0);
                        }
                        else
                        {
                            a = null;
                        }

                        return a;
                    });

            int returnValC;

            // Try to convert it to an int. If it throws an exception for some reason, chances are it's not what we're looking for.
            try
            {
                returnValC = Convert.ToInt32(returnVal);
            }
            catch
            {   // Probably not an int, so let's set the value to something we absolutely know will return as false.
                returnValC = -1;
            }

            // Let's get the return value by checking if the returnval == 1
            hasItem_returnVal = returnValC == 1;

            return hasItem_returnVal;
        }

        public static async Task<Mute> GetMuteFromDatabase(string id)
        {
            Mute item_returnVal;

            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_GetMuteFromId
            };

            SqliteParameter a = new SqliteParameter("$id", id)
            {
                DbType = DbType.String
            };

            command.Parameters.Add(a);

            // Get a single item from the list.
            // We're using a delegate that supposedly returns a list of mutes, but in this case it should only return one.
            item_returnVal = ((Mute[])await BotDatabase.Instance.ExecuteReaderAsync(command,
                    processAction: readMutes)).SingleOrDefault();

            // Check if it's default aka nothing found (for some reason). We should've already checked that this item exists previously, but I still
            // want to be super careful
            if (item_returnVal.Equals(default(Mute)))
            {   // Equals default.
                item_returnVal = Mute.Invalid;
            }

            return item_returnVal;
        }

        /// <summary>List all the mutes</summary>
        public static async Task ListMutes(CommandContext ctx)
        {
            Mute[] mutes = await ReadTable();

            // Check if there are any notifications. If there are none, let the user know.
            if (mutes.Length > 0)
            {   // There are mutes.
                var interactivity = Program.BotClient.GetInteractivity();
                List<Page> pages = new List<Page>();

                var deb = new DiscordEmbedBuilder();

                int count = 0;
                int curPage = 1;

                // Paginate all the results.
                const int MUTES_PER_PAGE = 5;
                for (int i = 0; i < mutes.Length; i++)
                {
                    Mute mute = mutes[i];



                    var dto = DateTimeOffset.FromUnixTimeSeconds(mute.Time * 60);

                    var valueStringBuilder = new StringBuilder();

                    valueStringBuilder.Append($"{Generics.GetMention(mute.User)}: {mute.Text}\n");
                    valueStringBuilder.Append($"**Id:** {mute.OriginalMessageId}\n");
                    valueStringBuilder.Append($"**Remaining time:** {Generics.GetRemainingTime(dto)}");

                    #region a bunny

                    //                      .".
                    //                     /  |
                    //                    /  /
                    //                   / ,"
                    //       .-------.--- /
                    //      "._ __.-/ o. o\
                    //         "   (    Y  )
                    //              )     /
                    //             /     (
                    //            /       Y
                    //        .-"         |
                    //       /  _     \    \
                    //      /    `. ". ) /' )
                    //     Y       )( / /(,/
                    //    ,|      /     )
                    //   ( |     /     /
                    //    " \_  (__   (__        [nabis]
                    //        "-._,)--._,)
                    // 
                    // ------------------------------------------------
                    // This ASCII pic can be found at
                    // https://asciiart.website/index.php?art=animals/rabbits

                    #endregion a bunny

                    string name = dto.ToString(Generics.DateFormat);

                    deb.AddField(name, valueStringBuilder.ToString());
                    count++;

                    if (count == MUTES_PER_PAGE || i == mutes.Length - 1)
                    {   // Create a new page.
                        deb.WithDescription(Generics.NeutralDirectResponseTemplate(
                            mention: ctx.User.Mention,
                            body: $"Hello {ctx.Member.Mention}, please note you are the only one who can react to this message.\n\n" +
                            $"**Showing {count} mutes out of a total of {mutes.Length}.**"));
                        deb.WithTitle($"Mutes Page {curPage}/{Math.Ceiling((float)mutes.Length / (float)MUTES_PER_PAGE)}");
                        deb.WithColor(Generics.NeutralColor);
                        deb.WithThumbnail(Generics.URL_MUTE_GENERIC);

                        pages.Add(new Page(embed: deb));
                        count = 0;
                        curPage++;

                        deb = new DiscordEmbedBuilder();
                    } // end if
                } // end for

                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, emojis: Generics.DefaultPaginationEmojis);
            }
            else
            {   // There are no mutes.
                await ctx.Channel.SendMessageAsync(
                        embed: Generics.GenericEmbedTemplate(
                            color: Generics.NeutralColor,
                            description: Generics.NeutralDirectResponseTemplate(
                                mention: ctx.Member.Mention,
                                body: "there are no mutes."),
                            thumbnail: Generics.URL_SPEECH_BUBBLE,
                            title: "Mutes"));

            }
        }

        static readonly Func<SqliteDataReader, object> readMutes =
            delegate (SqliteDataReader reader)
                {
                    var muteList = new List<Mute>();

                    while (reader.Read())
                    {   // Generate a mute per each row.
                        var r = new Mute(
                            originalMessageId: reader.GetString(0),
                            user: ulong.Parse(reader.GetString(1)),
                            text: reader.GetString(2),
                            time: reader.GetInt32(3),
                            guild: ulong.Parse(reader.GetString(4))
                        );

                        muteList.Add(r);
                    }

                    return muteList.ToArray();
                };


        /// <summary>Read the table and return as an array.</summary>
        public static async Task<Mute[]> ReadTable()
        {
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_ReadTable
            };

            Mute[] returnVal = (Mute[])await BotDatabase.Instance.ExecuteReaderAsync(command,
                processAction: readMutes);

            return returnVal;
        }

        public static async Task CheckMuteEvade(DSharpPlus.DiscordClient c, DSharpPlus.EventArgs.GuildMemberAddEventArgs ctx)
        {
            if(await IsMutedUser(ctx.Member.Id, ctx.Guild.Id))
            {
                DiscordMember member = await ctx.Guild.GetMemberAsync(ctx.Member.Id);
                DiscordRole role = ctx.Guild.GetRole(Program.Settings.MuteRoleID[ctx.Guild.Id]); 
                await member.GrantRoleAsync(role);
            }
        }

        /// <summary>Find any mutes that need to be triggered and trigger them.</summary>
        static async Task LookTriggerMutes(int timeNowMinutes)
        {
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_CheckMutesElapsed
            };

            SqliteParameter a = new SqliteParameter("$timenow", timeNowMinutes)
            {
                DbType = DbType.Int32
            };

            command.Parameters.Add(a);

            Mute[] pendingMutes = (Mute[])await BotDatabase.Instance.ExecuteReaderAsync(command,
                processAction: readMutes);

            // Check if there are any mutes
            if (pendingMutes.Length > 0)
            {   // There are mutes.
                using var delCommand = new SqliteCommand(BotDatabase.Instance.DataSource)
                {
                    CommandText = QQ_DeleteMutesElapsed
                };

                delCommand.Parameters.Add(a);

                Task[] tasks = new Task[pendingMutes.Length + 1];
                tasks[0] = BotDatabase.Instance.ExecuteNonQuery(delCommand);

                for (int i = 0; i < pendingMutes.Length; i++)
                {
                    var mute = pendingMutes[i];


                    DateTimeOffset muteTime = DateTimeOffset.FromUnixTimeSeconds(mute.Time * 60);
                    DateTimeOffset utcNow = DateTimeOffset.UtcNow;

                    var stringBuilder = new StringBuilder();
                    TimeSpan lateBy = utcNow.Subtract(muteTime);

                    DiscordEmbedBuilder deb = new DiscordEmbedBuilder()
                    {
                        Title = "User Unmuted",
                        Description = Generics.GetMention(mute.User)
                    };

                    deb.WithThumbnail(Generics.URL_MUTE_EXCLAIM);
                    deb.AddField(@"Late by",
                        value: String.Format("{0}day {1}hr {2}min {3}sec",
                            /*0*/ lateBy.Days,
                            /*1*/ lateBy.Hours,
                            /*2*/ lateBy.Minutes,
                            /*3*/ lateBy.Seconds));

                    try
                    {
                        DiscordGuild guild = await Program.BotClient.GetGuildAsync(mute.Guild);
                        DiscordMember member = await guild.GetMemberAsync(mute.User);
                        DiscordRole role = guild.GetRole(Program.Settings.MuteRoleID[mute.Guild]);
                        await member.RevokeRoleAsync(role);
                    }
                    catch(Exception e)
                    {
                        deb.AddField("Note", value: "User has left the server");
                    }


                        

                    tasks[i + 1] = (await Program.BotClient.GetChannelAsync(Program.Settings.ActionChannelId))
                                    .SendMessageAsync(
                                        content: stringBuilder.ToString(),
                                        embed: deb.Build());
                }

                await Task.WhenAll(tasks);
            }
        }

        #endregion MuteCommands.cs

        /// <summary>Interpret time value and increment a DateTimeOffset based on the values.</summary>
        /// <param name="measureString">The measure or numeric value.</param>
        /// <param name="unit">The time unit.</param>
        /// <param name="dto">The DateTimeOffset to increment.</param>
        private static void InterpretTime(string measureString, string unit, ref DateTimeOffset dto)
        {
            // Only continue if these two have a valid value.
            if (int.TryParse(measureString, out int measure) && measure > 0 && unit.Length > 0)
            {
                switch (unit.ToLower())
                {
                    case "month":
                    case "months":
                        dto = dto.AddMonths(measure);
                        break;
                    case "day":
                    case "days":
                    case "d":
                        dto = dto.AddDays(measure);
                        break;
                    case "week":
                    case "weeks":
                    case "wk":
                    case "wks":
                    case "w":
                        dto = dto.AddDays(measure * 7);
                        break;
                    case "hour":
                    case "hours":
                    case "hr":
                    case "hrs":
                    case "h":
                        dto = dto.AddHours(measure);
                        break;
                    case "minute":
                    case "minutes":
                    case "min":
                    case "mins":
                    case "m":
                        dto = dto.AddMinutes(measure);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>Checks if a character is a white space.</summary>
        private static bool IsWhitespace(char c)
        {
            return c.Equals(' ') ||
                   c.Equals('\r') ||
                   c.Equals('\n') ||
                   c.Equals('\t');
        }

        private static bool GetUsersToNotify(string[] users, out string mentions)
        {
            bool usersFound_returnVal = false;

            var stringBuilder = new StringBuilder();

            foreach (string user in users)
            {
                if (user.Length > 0)
                {
                    stringBuilder.Append($"{user} ");

                    if (!usersFound_returnVal)
                    {
                        usersFound_returnVal = true;
                    }
                }
            }

            mentions = stringBuilder.ToString();

            return usersFound_returnVal;
        }

        internal static async void MuteTimer_Elapsed(object sender, ElapsedEventArgs e)
            => await LookTriggerMutes(
                 (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60));
    }
}
