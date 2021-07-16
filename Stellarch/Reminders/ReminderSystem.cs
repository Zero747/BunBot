// ReminderSystem.cs
// A portion of the reminder system containing everything needed for processing reminders from ReminderCommands.cs
//  


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

namespace BigSister.Reminders
{
    public static partial class ReminderSystem
    {
        /// <summary>Query to add a reminder to the database.</summary>
        const string QQ_AddReminder = @"INSERT INTO `Reminders` (`Id`, `UserId`, `ChannelId`, `Message`, `TriggerTime`, `Mentions`) 
                                         VALUES ($id, $userid, $channelid, $message, $time, $mention);";
        /// <summary>Query to remove a reminder from the database.</summary>
        const string QQ_RemoveReminder = @"DELETE FROM `Reminders` WHERE `Id`=$id;";
        /// <summary>Query to check if a reminder exists.</summary>
        const string QQ_ReminderExists = @"SELECT EXISTS(SELECT 1 FROM `Reminders` WHERE `Id`=$id);";
        /// <summary>Query to read the entire reminder table.</summary>
        const string QQ_ReadTable = @"SELECT `Id`, `UserId`, `ChannelId`, `Message`, `TriggerTime`, `Mentions` FROM `Reminders`;";
        /// <summary>Query to return all reminders that need to be triggered.</summary>
        const string QQ_CheckRemindersElapsed = @"SELECT `Id`, `UserId`, `ChannelId`, `Message`, `TriggerTime`, `Mentions` 
                                                  FROM `Reminders` WHERE `TriggerTime` <= $timenow;";
        /// <summary>Query to delete all reminders that need to be triggered.</summary>
        const string QQ_DeleteRemindersElapsed = @"DELETE FROM `Reminders` WHERE `TriggerTime` <= $timenow;";
        /// <summary>Query to get a single reminder from a list.</summary>
        const string QQ_GetReminderFromId = @"SELECT `Id`, `UserId`, `ChannelId`, `Message`, `TriggerTime`, `Mentions` FROM `Reminders` WHERE `Id`=$id;";

        #region ReminderCommands.cs

        /// <summary>The date recognition Regex.</summary>
        // It groups every number/time unit pairing "(number)+(time unit)" into a group. 
        static readonly Regex DateRegex
            = new Regex(@"(\d+)\s?(months?|days?|d|weeks?|wks?|w|hours?|hrs?|h|minutes?|mins?|m)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
        public static async Task AddReminder(CommandContext ctx, string args)
        {   // Firstly get all the matches.
            MatchCollection regexMatches = DateRegex.Matches(args);
            BitArray regexCoverage = new BitArray(args.Length);
            var dto = ctx.Message.CreationTimestamp;
            List<ulong> mentions = new List<ulong>();

            // String processing - find the message and get the reminder end date. 
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

            // Get mentions
            foreach (DiscordUser user in ctx.Message.MentionedUsers)
            {
                ulong id = user.Id;

                if (!user.IsBot && !mentions.Contains(id))
                {
                    mentions.Add(id);
                }
            }

            // At this point, now we have the DateTimeOffset describing when this reminder needs to be set off, and we have a message string if
            // any. So now we just need to make sure it's within reasonable boundaries, set the reminder, and notify the user.

            DateTimeOffset maxtime = new DateTimeOffset(ctx.Message.CreationTimestamp.UtcDateTime).AddMonths(Program.Settings.MaxReminderTimeMonths);
            DiscordEmbedBuilder embed;

            bool sendErrorEmbed = false;

            if (dto.UtcTicks == ctx.Message.CreationTimestamp.UtcTicks)
            {   // No time was added.

                embed = Generics.GenericEmbedTemplate(
                        color: Generics.NegativeColor,
                        description: Generics.NegativeDirectResponseTemplate(
                            mention: ctx.Member.Mention,
                            body: @"I was unable able to add the reminder you gave me. You didn't supply me a valid time..."),
                        title: @"Unable to add reminder",
                        thumbnail: Generics.URL_REMINDER_GENERIC
                    );

                sendErrorEmbed = true;
            }
            else if (dto.UtcTicks > maxtime.UtcTicks)
            {   // More than our allowed time away.

                int maxMonths = Program.Settings.MaxReminderTimeMonths;

                embed = Generics.GenericEmbedTemplate(
                        color: Generics.NegativeColor,
                        description: Generics.NegativeDirectResponseTemplate(
                            mention: ctx.Member.Mention,
                            body: $"I was unable able to add the reminder you gave me. That's more than {maxMonths} month{(maxMonths > 0 ? @"s" : String.Empty)} away..."),
                        title: @"Unable to add reminder",
                        thumbnail: Generics.URL_REMINDER_GENERIC
                    );

                sendErrorEmbed = true;
            }
            else
            {   // Everything is good in the world... except that the world is burning, but that's not something we're worried about here, for
                // now...

                embed = Generics.GenericEmbedTemplate(
                        color: Generics.PositiveColor,
                        description: Generics.PositiveDirectResponseTemplate(
                            mention: ctx.Member.Mention,
                            body: @"I added the reminder you gave me!"),
                        title: @"Add reminder",
                        thumbnail: Generics.URL_REMINDER_GENERIC
                    );

                Reminder reminder = new Reminder(
                    originalMessageId: ctx.Message.Id.ToString(),
                    text: messageString.Length.Equals(0) ? @"n/a" : messageString.ToString(),
                    time: (int)(dto.ToUnixTimeSeconds() / 60),
                    user: ctx.Member.Id,
                    channel: ctx.Channel.Id,
                    usersToNotify: mentions.Select(a => Generics.GetMention(a)).ToArray());

                embed.AddField(@"User", ctx.Member.Mention, true);
                embed.AddField(@"Time (UTC)", dto.ToString(Generics.DateFormat), true);
                embed.AddField(@"Remaining time", Generics.GetRemainingTime(dto), true);
                embed.AddField(@"Notification Identifier", reminder.OriginalMessageId.ToString(), false);

                if (GetUsersToNotify(reminder.UsersToNotify, out string mentionsString))
                {
                    embed.AddField(@"Users to mention", mentionsString, false);
                }

                // Let's build the command.
                using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
                {
                    CommandText = QQ_AddReminder
                };

                SqliteParameter a = new SqliteParameter("$id", reminder.OriginalMessageId.ToString())
                {
                    DbType = DbType.String
                };

                SqliteParameter b = new SqliteParameter("$userid", reminder.User)
                {
                    DbType = DbType.String
                };

                SqliteParameter c = new SqliteParameter("$channelid", reminder.Channel)
                {
                    DbType = DbType.String
                };

                SqliteParameter d = new SqliteParameter("$message", reminder.Text)
                {
                    DbType = DbType.String
                };

                SqliteParameter e = new SqliteParameter("$time", reminder.Time)
                {
                    DbType = DbType.Int32
                };

                var stringBuilder = new StringBuilder();
                stringBuilder.AppendJoin(' ', reminder.UsersToNotify);

                SqliteParameter f = new SqliteParameter("$mention", stringBuilder.ToString())
                {
                    DbType = DbType.String
                };

                command.Parameters.AddRange(new SqliteParameter[] { a, b, c, d, e, f });

                await BotDatabase.Instance.ExecuteNonQuery(command);
                // Send the response.
                await ctx.Channel.SendMessageAsync(embed: embed);
            }

            if (sendErrorEmbed)
            {
                var a = ctx.Channel.SendMessageAsync(embed: embed);
                var b = GenericResponses.HandleInvalidArguments(ctx);

                await Task.WhenAll(a, b);
            }
        }

        /// <summary>Remove a reminder if it exists.</summary>
        public static async Task RemoveReminder(CommandContext ctx, Reminder reminder)
        {
            // It's a reminder, so let's remove it.

            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_RemoveReminder
            };

            SqliteParameter a = new SqliteParameter("$id", reminder.OriginalMessageId.ToString())
            {
                DbType = DbType.String
            };

            command.Parameters.Add(a);

            // Now that we have the old reminder, let's remove the old one from the database.
            await BotDatabase.Instance.ExecuteNonQuery(command);

            // Now let's respond.

            var discordEmbedBuilder = new DiscordEmbedBuilder(Generics.GenericEmbedTemplate(
                color: Generics.PositiveColor,
                description: Generics.PositiveDirectResponseTemplate(
                    mention: ctx.Member.Mention,
                    @"I able to remove the reminder you gave me!"),
                thumbnail: Generics.URL_REMINDER_DELETED,
                title: @"Removed reminder"));

            DateTimeOffset dto = DateTimeOffset.FromUnixTimeSeconds(reminder.Time * 60); // The reminder's DTO.
            TimeSpan remainingTime = dto.Subtract(DateTimeOffset.UtcNow); // The remaining time left for the reminder.
            string originalAuthorMention = Generics.GetMention(reminder.User);

            discordEmbedBuilder.AddField(@"User", originalAuthorMention, true);
            discordEmbedBuilder.AddField(@"Time (UTC)", dto.ToString(Generics.DateFormat), true);
            discordEmbedBuilder.AddField(@"Notification Identifier", reminder.OriginalMessageId.ToString(), false);
            if (GetUsersToNotify(reminder.UsersToNotify, out string mentionsString))
            {
                discordEmbedBuilder.AddField(@"Users to mention", mentionsString, false);
            }
            discordEmbedBuilder.AddField(@"Remaining time", Generics.GetRemainingTime(dto), false);
            discordEmbedBuilder.AddField(@"Message", reminder.Text, false);

            // Send the response.
            await ctx.Channel.SendMessageAsync(embed: discordEmbedBuilder);
        }

        /// <summary>Check if a provided ID is a reminder.</summary>
        public static async Task<bool> IsReminder(string id)
        {
            bool hasItem_returnVal;

            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_ReminderExists
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

        public static async Task<Reminder> GetReminderFromDatabase(string id)
        {
            Reminder item_returnVal;

            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_GetReminderFromId
            };

            SqliteParameter a = new SqliteParameter("$id", id)
            {
                DbType = DbType.String
            };

            command.Parameters.Add(a);

            // Get a single item from the list.
            // We're using a delegate that supposedly returns a list of reminders, but in this case it should only return one.
            item_returnVal = ((Reminder[])await BotDatabase.Instance.ExecuteReaderAsync(command,
                    processAction: readReminders)).SingleOrDefault();

            // Check if it's default aka nothing found (for some reason). We should've already checked that this item exists previously, but I still
            // want to be super careful
            if (item_returnVal.Equals(default(Reminder)))
            {   // Equals default.
                item_returnVal = Reminder.Invalid;
            }

            return item_returnVal;
        }

        /// <summary>List all the reminders</summary>
        public static async Task ListReminders(CommandContext ctx)
        {
            Reminder[] reminders = await ReadTable();

            // Check if there are any notifications. If there are none, let the user know.
            if (reminders.Length > 0)
            {   // There are reminders.
                var interactivity = Program.BotClient.GetInteractivity();
                List<Page> pages = new List<Page>();

                var deb = new DiscordEmbedBuilder();

                int count = 0;
                int curPage = 1;

                // Paginate all the results.
                const int REMINDERS_PER_PAGE = 5;
                for (int i = 0; i < reminders.Length; i++)
                {
                    Reminder reminder = reminders[i];



                    var dto = DateTimeOffset.FromUnixTimeSeconds(reminder.Time * 60);

                    var valueStringBuilder = new StringBuilder();

                    valueStringBuilder.Append($"{Generics.GetMention(reminder.User)}: {reminder.Text}\n");
                    if (GetUsersToNotify(reminder.UsersToNotify, out string mentionsString))
                    {
                        valueStringBuilder.Append($"**Users to mention:** {mentionsString}\n");
                    }
                    valueStringBuilder.Append($"**Id:** {reminder.OriginalMessageId}\n");
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
                    //  o < bunny poopy l0l
                    // ------------------------------------------------
                    // This ASCII pic can be found at
                    // https://asciiart.website/index.php?art=animals/rabbits

                    #endregion a bunny

                    string name = dto.ToString(Generics.DateFormat);

                    deb.AddField(name, valueStringBuilder.ToString());
                    count++;

                    if (count == REMINDERS_PER_PAGE || i == reminders.Length - 1)
                    {   // Create a new page.
                        deb.WithDescription(Generics.NeutralDirectResponseTemplate(
                            mention: ctx.User.Mention,
                            body: $"Hello {ctx.Member.Mention}, please note you are the only one who can react to this message.\n\n" +
                            $"**Showing {count} reminders out of a total of {reminders.Length}.**"));
                        deb.WithTitle($"Reminders Page {curPage}/{Math.Ceiling((float)reminders.Length / (float)REMINDERS_PER_PAGE)}");
                        deb.WithColor(Generics.NeutralColor);
                        deb.WithThumbnail(Generics.URL_REMINDER_GENERIC);

                        pages.Add(new Page(embed: deb));
                        count = 0;
                        curPage++;

                        deb = new DiscordEmbedBuilder();
                    } // end if
                } // end for

                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, emojis: Generics.DefaultPaginationEmojis);
            }
            else
            {   // There are no reminders.
                await ctx.Channel.SendMessageAsync(
                        embed: Generics.GenericEmbedTemplate(
                            color: Generics.NeutralColor,
                            description: Generics.NeutralDirectResponseTemplate(
                                mention: ctx.Member.Mention,
                                body: "there are no reminders."),
                            thumbnail: Generics.URL_SPEECH_BUBBLE,
                            title: "Reminders"));

            }
        }

        static readonly Func<SqliteDataReader, object> readReminders =
            delegate (SqliteDataReader reader)
                {
                    var reminderList = new List<Reminder>();

                    while (reader.Read())
                    {   // Generate a reminder per each row.
                        var r = new Reminder(
                            originalMessageId: reader.GetString(0),
                            user: ulong.Parse(reader.GetString(1)),
                            channel: ulong.Parse(reader.GetString(2)),
                            text: reader.GetString(3),
                            time: reader.GetInt32(4),
                            usersToNotify: reader.GetString(5).Split(' ')
                        );

                        reminderList.Add(r);
                    }

                    return reminderList.ToArray();
                };


        /// <summary>Read the table and return as an array.</summary>
        public static async Task<Reminder[]> ReadTable()
        {
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_ReadTable
            };

            Reminder[] returnVal = (Reminder[])await BotDatabase.Instance.ExecuteReaderAsync(command,
                processAction: readReminders);

            return returnVal;
        }

        /// <summary>Find any reminders that need to be triggered and trigger them.</summary>
        static async Task LookTriggerReminders(int timeNowMinutes)
        {
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_CheckRemindersElapsed
            };

            SqliteParameter a = new SqliteParameter("$timenow", timeNowMinutes)
            {
                DbType = DbType.Int32
            };

            command.Parameters.Add(a);

            Reminder[] pendingReminders = (Reminder[])await BotDatabase.Instance.ExecuteReaderAsync(command,
                processAction: readReminders);

            // Check if there are any reminders
            if (pendingReminders.Length > 0)
            {   // There are reminders.
                using var delCommand = new SqliteCommand(BotDatabase.Instance.DataSource)
                {
                    CommandText = QQ_DeleteRemindersElapsed
                };

                delCommand.Parameters.Add(a);

                Task[] tasks = new Task[pendingReminders.Length + 1];
                tasks[0] = BotDatabase.Instance.ExecuteNonQuery(delCommand);

                for (int i = 0; i < pendingReminders.Length; i++)
                {
                    var reminder = pendingReminders[i];


                    DateTimeOffset reminderTime = DateTimeOffset.FromUnixTimeSeconds(reminder.Time * 60);
                    DateTimeOffset utcNow = DateTimeOffset.UtcNow;

                    var stringBuilder = new StringBuilder();
                    TimeSpan lateBy = utcNow.Subtract(reminderTime);

                    DiscordEmbedBuilder deb = new DiscordEmbedBuilder()
                    {
                        Title = "Notification",
                        Description = reminder.Text
                    };

                    deb.WithThumbnail(Generics.URL_REMINDER_EXCLAIM);
                    deb.AddField(@"Late by",
                        value: String.Format("{0}day {1}hr {2}min {3}sec",
                            /*0*/ lateBy.Days,
                            /*1*/ lateBy.Hours,
                            /*2*/ lateBy.Minutes,
                            /*3*/ lateBy.Seconds));

                    // Get all the people we need to remind.
                    stringBuilder.Append(Generics.GetMention(reminder.User));

                    Array.ForEach(reminder.UsersToNotify,            // For every user (a), append them to sb in mention format <@id>.
                        a => stringBuilder.Append($"{a} "));



                    tasks[i + 1] = (await Program.BotClient.GetChannelAsync(reminder.Channel))
                                    .SendMessageAsync(
                                        content: stringBuilder.ToString(),
                                        embed: deb.Build());
                }

                await Task.WhenAll(tasks);
            }
        }

        #endregion ReminderCommands.cs

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

        internal static async void ReminderTimer_Elapsed(object sender, ElapsedEventArgs e)
            => await LookTriggerReminders(
                 (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60));
    }
}
