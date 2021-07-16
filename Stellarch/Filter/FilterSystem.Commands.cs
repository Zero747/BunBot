// FilterSystem.Commands.cs
// A portion of the filter system containing everything needed for processing commands from FilterCommands.cs
// 1) Unwraps commands coming from FilterCommands.cs and responds to those commands:
// 2) Contains methods for querying the database for relevant information.
// 3) Updating the regex array whenever the cache updates.
//


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using BigSister.ChatObjects;
using BigSister.Database;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity.Enums;

namespace BigSister.Filter
{
    public static partial class FilterSystem
    {
        // This region handles everything coming from FilterCommands.cs
        #region FilterCommands.cs

        /// <summary>Mask data type.</summary>
        public const int TYPE_MASK = 1;
        /// <summary>Exclude data type.</summary>
        public const int TYPE_EXCLUDE = 2;


        /// <summary>Query to check if a mask or exclude exists in the database.</summary>
        const string QQ_ItemExistsAny = @"SELECT EXISTS(SELECT 1 FROM `FILTER` WHERE `String`=$string);";
        /// <summary>Query to check if a mask or exclude exists in the database.</summary>
        const string QQ_ItemExistsType = @"SELECT EXISTS(SELECT 1 FROM `FILTER` WHERE `Type`=$type AND `String`=$string);";
        /// <summary>Query to add a mask or exclude into the database.</summary>
        const string QQ_ItemAdd = @"INSERT INTO `Filter` (`Type`, `String`) VALUES ($type, $string);";
        /// <summary>Query to remove a mask or exclude into the database.</summary>
        const string QQ_ItemRemove = @"DELETE FROM `Filter` WHERE `Type`=$type AND `String`=$string;";
        /// <summary>Query to read the table for either a mask or an exclude.</summary>
        const string QQ_ReadTable = @"SELECT `String` FROM `Filter` WHERE `Type`=$type;";

        static readonly RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.Multiline;

        static string[] MaskCache;
        static string[] ExcludeCache;
        static Regex[] FilterRegex;

        public static string GetLabelString(int a)
        {
            return a switch
            {
                TYPE_MASK => "filter",
                TYPE_EXCLUDE => "exclude",
                _ => String.Empty,
            };
        }

        static FilterSystem() { }

        /// <summary>
        /// please call me when program starts
        /// </summary>
        public static void Initialize()
        {
            // bun makes cache

            UpdateCache()
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public static async Task<bool> HasMask(string item)
            => await HasItem(item, TYPE_MASK);
        public static async Task<bool> HasExclude(string item)
            => await HasItem(item, TYPE_EXCLUDE);

        /// <summary>Check if an item (possibly a mask or an exclude) exists in the database.</summary>
        public static async Task<bool> HasItem(string item, int type = -1)
        {
            bool hasItem_returnVal;
            bool anyType = type == -1;

            // Check the cache with three possible cases:
            //  CASE A: We're looking for a mask so we search for a mask.
            //  CASE B: We're looking for an exclude so we search for an exclude.
            //  CASE C: We're not looking for anything in particular so we search indiscriminately.
            if ( (type == TYPE_MASK && ExistsInCache(TYPE_MASK, item)) || // CASE A
                 (type == TYPE_EXCLUDE && ExistsInCache(TYPE_EXCLUDE, item) ) || // CASE B
                 (type == -1 && (ExistsInCache(TYPE_MASK, item) || ExistsInCache(TYPE_EXCLUDE, item))) ) // CASE C
            {   // In cache.
                hasItem_returnVal = true;
            }
            else
            {   // Not in cache so we have to look for it

                // Let's build the command.
                using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
                {
                    CommandText = anyType ? 
                                    QQ_ItemExistsAny :  // We're looking for any kind of type.
                                    QQ_ItemExistsType   // We're looking for a specific item type.
                };

                SqliteParameter a = new SqliteParameter("$string", item)
                {
                    DbType = DbType.String
                };

                command.Parameters.Add(a);

                // We're looking for a specific type.
                if(!anyType)
                {
                    SqliteParameter b = new SqliteParameter("$type", type)
                    {
                        DbType = DbType.String
                    };

                    command.Parameters.Add(b);
                }

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
            }

            return hasItem_returnVal;
        }

        /// <summary>Add a mask to the database.</summary>
        public static async Task AddMask(CommandContext ctx, string mask)
            => await AddItem(ctx, TYPE_MASK, mask);

        /// <summary>Adds an exclude to the database.</summary>
        public static async Task AddExclude(CommandContext ctx, string exclude)
            => await AddItem(ctx, TYPE_EXCLUDE, exclude);

        /// <summary>Try to add an item to the database.</summary>
        static async Task AddItem(CommandContext ctx, int type, string item)
        {
            string label = GetLabelString(type);

            // Check the cache.
            if (ExistsInCache(type, item))
            {   // It's in the cache, so we need to let the user know that this word already exists.

                await GenericResponses.SendGenericCommandError(
                        ctx.Channel,
                        ctx.Member.Mention,
                        $"Unable to add {label}",
                        $"the provided {label} exists already...");
            }
            else
            {   // It's not in the cache so we can add it to the thing
                // Let's build the command.
                using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
                {
                    CommandText = QQ_ItemAdd
                };

                var a = new SqliteParameter("$type", type)
                {
                    DbType = DbType.Byte
                };

                var b = new SqliteParameter("$string", item)
                {
                    DbType = DbType.String
                };

                command.Parameters.Add(a);
                command.Parameters.Add(b);

                // Add it to the database.
                await BotDatabase.Instance.ExecuteNonQuery(command);

                // Respond
                var deb = Generics.GenericEmbedTemplate(
                    color: Generics.PositiveColor,
                    description: Generics.PositiveDirectResponseTemplate(
                        mention: ctx.Member.Mention,
                        body: $"I added a new {label} to the filter: `{item}`"),
                        thumbnail: Generics.URL_FILTER_ADD,
                        title: $"Added new {label}");
                await ctx.Channel.SendMessageAsync(embed: deb);

                await UpdateCache();
            }
        }

        /// <summary>Remove a mask from the database.</summary>
        public static async Task RemoveMask(CommandContext ctx, string mask)
            => await RemoveItem(ctx, TYPE_MASK, mask);
        /// <summary>Remove an exclude from the database.</summary>
        public static async Task RemoveExclude(CommandContext ctx, string exclude)
            => await RemoveItem(ctx, TYPE_EXCLUDE, exclude);
        /// <summary>Remove an item from the database.</summary>
        static async Task RemoveItem(CommandContext ctx, int type, string item)
        {
            string label = GetLabelString(type);

            // Check if it's in cache.
            if (!ExistsInCache(type, item))
            {   // It's in the cache, so we need to let the user know that it wasn't added
                await GenericResponses.SendGenericCommandError(
                        ctx.Channel,
                        ctx.Member.Mention,
                        $"Unable to remove {label}",
                        $"the provided {label} does not exist already...");
            }
            else
            {
                // Build commdand

                using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
                {
                    CommandText = QQ_ItemRemove
                };

                var a = new SqliteParameter("$type", type)
                {
                    DbType = DbType.Byte
                };

                var b = new SqliteParameter("$string", item)
                {
                    DbType = DbType.String
                };

                command.Parameters.Add(a);
                command.Parameters.Add(b);

                // please remove it from the database thank you
                await BotDatabase.Instance.ExecuteNonQuery(command);

                // Respond
                var deb = Generics.GenericEmbedTemplate(
                    color: Generics.PositiveColor,
                    description: Generics.PositiveDirectResponseTemplate(
                        mention: ctx.Member.Mention,
                        body: $"I removed the {label} from the filter: `{item}`"),
                        thumbnail: Generics.URL_FILTER_SUB,
                        title: $"Removed {label}");
                await ctx.Channel.SendMessageAsync(embed: deb);

                await UpdateCache();
            }
        }

        /// <summary>List all the masks in the database.</summary>  
        public static async Task ListMasks(CommandContext ctx)
            => await ListItems(ctx, TYPE_MASK);
        /// <summary>List all the excludes in the database.</summary>
        public static async Task ListExcludes(CommandContext ctx)
            => await ListItems(ctx, TYPE_EXCLUDE);
        /// <summary>List all the items in the database in a paginated string.</summary>
        public static async Task ListItems(CommandContext ctx, int type)
        {
            // Making this a constant in case DSharpPlus changes how many lines until a pagination is split (it is currently 15 as of 2020-Sep-16).
            const int SPLIT_LINES = 15;

            string[] items = await ReadTable(type);
            string label = GetLabelString(type);

            var stringBuilder = new StringBuilder();
            foreach (string item in items) // Make a list of all the items.
            {
                stringBuilder.AppendLine(Formatter.InlineCode(item));
            }

            var interactivity = Program.BotClient.GetInteractivity();

            // Now we want to define the pages.
            Page[] pages;
            
            // If we actually have items, we can set up the variable.
            if (items.Length > 0)
            {   // Items there, make page.
                pages = interactivity.GeneratePagesInEmbed(
                    input: stringBuilder.ToString(),
                    splittype: SplitType.Line,
                    embedbase:
                        Generics.GenericEmbedTemplate(
                            color: Generics.NeutralColor,
                            thumbnail: Generics.URL_FILTER_GENERIC)).ToArray();
            }
            else
            {   // No items, make blank variable.
                pages = new Page[0];
            }

            // Go through each embed and give them a header because for some reason I can't provide my own description in the base embed. Thanks
            // for that.
            for (int i = 0; i < pages.Length; i++)
            {
                DiscordEmbedBuilder deb = new DiscordEmbedBuilder(pages[i].Embed);

                deb.Description =
                    Generics.NeutralDirectResponseTemplate(
                        mention: ctx.Member.Mention,
                        body: $"please note you are the only one who can react to this. Here is the {label} list:\n\n{deb.Description}");

                pages[i].Embed = deb.Build();
            }

            // Check if there are even any pages. If it's equal to SPLIT_LINES or SPLIT_LINES + 1 it means the pagination embed builder is going to
            // try and fit it all into one page, so we also want to account for those too.
            if (pages.Length == 1 || items.Length == SPLIT_LINES || items.Length == SPLIT_LINES + 1)
            {   // Exactly one page we need to send.

                // Delete the page footer.
                DiscordEmbedBuilder deb = new DiscordEmbedBuilder(pages[0].Embed);
                deb.WithFooter(String.Empty);

                await ctx.RespondAsync(embed: deb);
            }
            else if (pages.Length > 1)
            {   // There are pages we need to send.

                // Send the paginated message.
                await interactivity
                    .SendPaginatedMessageAsync(
                        c: ctx.Channel,
                        u: ctx.User,
                        pages: pages,
                        emojis: Generics.DefaultPaginationEmojis);
            }
            else if(pages.Length == 0)
            {   // There are no pages we need to send.
                await ctx.RespondAsync(
                    embed: Generics.GenericEmbedTemplate(
                        color: Generics.NeutralColor,
                        description: Generics.NeutralDirectResponseTemplate(
                            mention: ctx.Member.Mention,
                            body: $"there are no {label}s currently"),
                        thumbnail: Generics.URL_FILTER_GENERIC,
                        title: $"List of {label}s"));
            }
        }

        /// <summary>Read a column from the filter table and return it as an array.</summary>
        public static async Task<string[]> ReadTable(int type)
        {
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_ReadTable
            };

            var a = new SqliteParameter("$type", type)
            {
                DbType = DbType.Byte
            };

            command.Parameters.Add(a);

            string[] returnVal = (string[])await BotDatabase.Instance.ExecuteReaderAsync(command,
                processAction: delegate (SqliteDataReader reader)
                {
                    var rows = new List<string>();

                    while (reader.Read())
                    {   // From each row in column String add the result to the list.
                        rows.Add(reader.GetString(0));
                    }

                    return rows.ToArray();
                });

            return returnVal;
        }

        /// <summary>Checks if an item is in cache.</summary>
        static bool ExistsInCache(int type, string searchItem)
        {
            return type switch
            {
                TYPE_MASK => MaskCache.Contains(searchItem),
                TYPE_EXCLUDE => ExcludeCache.Contains(searchItem),
                // why are you here?
                _ => false,
            };
        }

        /// <summary>Update the cache.</summary>
        static async Task UpdateCache()
        {
            MaskCache = await ReadTable(TYPE_MASK);
            ExcludeCache = await ReadTable(TYPE_EXCLUDE);

            // Update the regex array.
            List<string> maskListDesc = MaskCache.ToList();

            maskListDesc.Sort();
            maskListDesc.Reverse();

            // Initiate a regex value for every mask.
            FilterRegex = new Regex[maskListDesc.Count];
            for (int i = 0; i < maskListDesc.Count; i++)
            {
                FilterRegex[i] = new Regex(maskListDesc[i], regexOptions);
            }
        }


        #endregion FilterCommands.cs
    }
}
