// RoleRequestSystem.cs
// One piece of a partial class, handling mostly the backend such as queries and event handlers.
//
// Emiko

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using BigSister.Commands;
using BigSister.Database;
using DSharpPlus;

namespace BigSister.RoleRequest
{
    static partial class RoleRequestSystem
    {
        protected struct RoleInfo
        {
            public readonly EmojiData EmojiData;
            public readonly ulong RoleId;

            public RoleInfo(EmojiData data, ulong roleid)
            {
                EmojiData = data;
                RoleId = roleid;
            }
        }

        //MessageIdRoleIdIsUnicodeEmoteData
        const string QQ_AddRow = @"INSERT INTO `Roles` (`MessageId`, `RoleId`, `IsUnicode`, `EmoteData`) VALUES ($messageId, $roleId, $isUnicode, $emoteData);";
        const string QQ_RemoveRow = @"DELETE FROM `Roles` WHERE `MessageId`=$messageId AND `EmoteData`=$emoteData;";
        const string QQ_CheckMessageEmoteExists = @"SELECT 1 FROM `Roles` WHERE `MessageId`=$messageId AND `EmoteData`=$emoteData;";
        const string QQ_QueryRowsMessageId = @"SELECT `RoleId`, `IsUnicode`, `EmoteData` FROM `Roles` WHERE `MessageId`=$messageId;";
        const string QQ_QueryRoleMessageIdEmote = @"SELECT `RoleId` FROM `ROLES` WHERE `MessageId`=$messageId AND `EmoteData`=$emoteData;";

        private static async Task AddMessageToDatabase(ulong id, DiscordRole role, DiscordEmoji emoji)
        {
            var emojiData = new EmojiData(emoji);

            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_AddRow
            };

            SqliteParameter a = new SqliteParameter("$messageId", id.ToString())
            {
                DbType = DbType.String
            };

            SqliteParameter b = new SqliteParameter("$roleId", role.Id.ToString())
            {
                DbType = DbType.String
            };

            SqliteParameter c = new SqliteParameter("$isUnicode", emojiData.IsUnicodeCharacter)
            {
                DbType = DbType.Boolean
            };

            SqliteParameter d = new SqliteParameter("$emoteData", emojiData.Value)
            {
                DbType = DbType.String
            };

            command.Parameters.AddRange(new SqliteParameter[] { a, b, c, d });

            await BotDatabase.Instance.ExecuteNonQuery(command);
        }
        private static async Task<bool> CheckEmoteMessageExists(ulong messageId, DiscordEmoji emoji)
        {
            var emojiData = new EmojiData(emoji);

            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_CheckMessageEmoteExists
            };

            SqliteParameter a = new SqliteParameter("$messageId", messageId.ToString())
            {
                DbType = DbType.String
            };

            SqliteParameter b = new SqliteParameter("$emoteData", emojiData.Value)
            {
                DbType = DbType.String
            };

            command.Parameters.AddRange(new SqliteParameter[] { a, b });

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

            return returnValC == 1;
        }

        private static async Task<RoleInfo[]> GetMessageEmotes(ulong messageId)
        {
            RoleInfo[] @return;

            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_QueryRowsMessageId
            };

            SqliteParameter a = new SqliteParameter("$messageId", messageId.ToString())
            {
                DbType = DbType.String
            };

            command.Parameters.Add(a);

            @return = (RoleInfo[])await BotDatabase.Instance.ExecuteReaderAsync(command,
                processAction: delegate (SqliteDataReader reader)
                {
                    List<RoleInfo> returnVal = new List<RoleInfo>();

                    while (reader.Read())
                    {   // Let's read the database.
                        returnVal.Add(new RoleInfo(
                                    roleid: ulong.Parse(reader.GetString(0)),
                                    data: new EmojiData(
                                        isUnicodeCharacter: reader.GetBoolean(1),
                                        value: reader.GetString(2))));
                    }

                    return returnVal.ToArray();
                });

            return @return;
        }

        private static async Task RemoveRow(ulong messageId, string emoteData)
        {
            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_RemoveRow
            };

            SqliteParameter a = new SqliteParameter("$messageId", messageId.ToString())
            {
                DbType = DbType.String
            };

            SqliteParameter b = new SqliteParameter("$emoteData", emoteData)
            {
                DbType = DbType.String
            };

            command.Parameters.Add(a);
            command.Parameters.Add(b);

            await BotDatabase.Instance.ExecuteNonQuery(command);
        }

        private static async Task<ulong> QueryRoleInfo(ulong messageId, string emoteData)
        {
            ulong @return;

            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_QueryRoleMessageIdEmote
            };

            SqliteParameter a = new SqliteParameter("$messageId", messageId.ToString())
            {
                DbType = DbType.String
            };

            SqliteParameter b = new SqliteParameter("$emoteData", emoteData)
            {
                DbType = DbType.String
            };

            command.Parameters.Add(a);
            command.Parameters.Add(b);


            @return = (ulong)await BotDatabase.Instance.ExecuteReaderAsync(command,
                processAction: delegate (SqliteDataReader reader)
                {
                    ulong returnVal;

                    if(reader.Read())
                    {   // Let's read the database.
                        string val = reader.GetString(0);

                        if(ulong.TryParse(val, out ulong @ulong))
                        {
                            returnVal = @ulong;
                        }
                        else
                        {
                            returnVal = 0;
                        }
                    }
                    else
                    {
                        returnVal = 0;
                    }

                    return returnVal;
                });

            return @return;
        }

        static readonly MinimumRole MinRoleColonist = new MinimumRole(Role.Colonist);
        static async Task HandleTakeGiveRole(bool giveRole, 
                                             DiscordGuild guild,
                                             DiscordUser user, 
                                             DiscordChannel channel, 
                                             DiscordMessage message,
                                             DiscordEmoji emoji)
        {
            var callingMember = await guild.GetMemberAsync(user.Id);

            // Make sure the person is a colonist and not muted
            if (await Permissions.HandlePermissionsCheck(
                member: callingMember,
                chan: channel,
                minRole: MinRoleColonist,
                shouldRespondToRejection: false) &&
                (!user.IsBot) )
            {
                // Check the database for exactly what role we're giving or removing from them.

                EmojiData emojiData = new EmojiData(emoji);

                ulong discordRoleId = await QueryRoleInfo(message.Id, emojiData.Value);

                // Let's check if it's a valid role
                if(discordRoleId != 0)
                {   // It's a valid role, so let's give or remove it to the person.

                    DiscordMember member = await guild.GetMemberAsync(user.Id);
                    DiscordRole role = guild.GetRole(discordRoleId);

                    // Let's give them the role or remove it from them now.
                    if(giveRole)
                        await member.GrantRoleAsync(role);
                    else
                        await member.RevokeRoleAsync(role);
                } // end if
            } // end if
        } // end method

        internal static async Task MessageReactionAdded(DiscordClient botClient, MessageReactionAddEventArgs e)
            => await HandleTakeGiveRole(true, e.Guild, e.User, e.Channel, e.Message, e.Emoji);

        internal static async Task MessageReactionRemoved(DiscordClient botClient, MessageReactionRemoveEventArgs e)
            => await HandleTakeGiveRole(false, e.Guild, e.User, e.Channel, e.Message, e.Emoji);
    }
}
