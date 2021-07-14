// BotDatabase.cs
// Contains methods for accessing the SQLite database. So far there are four tables:
//  1) Rimboard =================================================================================================================================================
//  |   OriginalMessageId               |   PinnedMessageId                     |   OriginalMessageChannelId            |   PinnedMessageChannelId              |  
//  |-----------------------------------+---------------------------------------|---------------------------------------|---------------------------------------|  
//  |   String representing UInt64      |   String representing UInt64          |   String representing UInt64          |   String representing UInt64          |  
//  |   Snowflake of original message   |   Snowflake of pinned aka reposted    |   Snowflake of pinned aka reposted    |   Snowflake of pinned aka reposted    |  
//  |                                   |       message.                        |       message.                        |       message.                        |
//  2) Reminders    =============================================================================================================================================================================================================================
//  |   Id                              |   UserId                              |   ChannelId                           |   Message                             |   TriggerTime                         |   Mentions                            |
//  |-----------------------------------+---------------------------------------+---------------------------------------+---------------------------------------+---------------------------------------+---------------------------------------|
//  |   String not Null                 |   String representing UInt64 not Null |   String representing UInt64 not Null |   String                              |   Integer not Null                    |   String                              |
//  |   Snowflake of the message that   |   The user who created the reminder.  |   Snowflake of the channel the        |   Reminder message                    |   Reminder trigger timestamp in Unix  |   Whitespace separated mentions       |
//  |   created the reminder.           |                                       |   reminder was sent in                |                                       |   epoch UTC in seconds                |                                       |
//  3) Filter   =================================================================================================================================================================================================================================
//  |   Type                            |   String                              |                                           
//  |-----------------------------------+---------------------------------------|(\(\                                       
//  |   Integer not Null Default '1'    |   String not Null                     |(-,-)      <-- bunnies ftw                                                        _______________________________________________
//  |   1 = Regex Mask and 2 = Exclude  |   Mask of Regex or Exclude            |o_(")(")                                                                          | __|  \/  |_ _| |/ / _ \    THE
//  4) Roles    =================================================================================================================================================  | _|| |\/| || || ' < (_) |       BEST
//  |   MessageId                       |   RoleId                              |   IsUnicode                           |   EmoteData                           |  |___|_|  |_|___|_|\_\___/            VAMPBUN
//  |-----------------------------------+---------------------------------------+---------------------------------------|---------------------------------------|                                           AROUND
//  |String representing UInt64 not Null|   String representing UInt64 not Null |   Integer representing boolean        |   String representing either a unicode|
//  |   Snowflake of message            |   Snowflake of role                   |   If the EmoteData is a unicode value |   character or a ulong id.            |
//  =============================================================================================================================================================
//
// EMIKO                          

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace BigSister.Database
{
    public sealed class BotDatabase
    {
        private static readonly BotDatabase instance =
            new BotDatabase(Program.Files.DatabaseFile);

        readonly SemaphoreSlim semaphoreSlim;

        /// <summary>Database connection datasource.</summary>
        public string DataSource;

        /// <summary>Single database instance.</summary>
        public static BotDatabase Instance
        {
            get => instance;
        }

        static BotDatabase() { }

        BotDatabase(string uri)
        {
            DataSource = $"Data Source={uri}";
            semaphoreSlim = new SemaphoreSlim(1, 1);
        }

        ~BotDatabase()
        {
            semaphoreSlim.Dispose();
        }

#pragma warning disable IDE0063
        /// <summary>Executes a command and invokes an action on the results.</summary>
        /// <param name="processAction">Action to invoke on the results.</param>
        public async Task<object> ExecuteReaderAsync(SqliteCommand cmd, Func<SqliteDataReader, object> processAction)
        {
            object returnVal;

            semaphoreSlim.Wait();

            try
            {
                using(var connection = new SqliteConnection(DataSource))
                {
                    DataSet ds = new DataSet();

                    cmd.Connection = connection;

                    connection.Open();

                    using (SqliteDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        returnVal = processAction.Invoke(reader);
                    }                    

                    connection.Close();
                    connection.Dispose();
                }
            } 
            finally
            {
                semaphoreSlim.Release();
            }

            return returnVal;
        }
#pragma warning restore IDE0063

#pragma warning disable IDE0063

        /// <summary>Execute a command to the database.</summary>
        public async Task ExecuteNonQuery(SqliteCommand cmd)
        {
            semaphoreSlim.Wait();

            try
            {
                using (var connection = new SqliteConnection(DataSource))
                {
                    cmd.Connection = connection;

                    connection.Open();

                    await cmd.ExecuteNonQueryAsync();

                    connection.Close();
                    connection.Dispose();
                }
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
#pragma warning restore IDE0063

#pragma warning disable IDE0063
        /// <summary>Generate a default database file.</summary>
        public static void GenerateDefaultFile(string outputFile)
        {   //Data Source=c:\mydb.db;Version=3;
            string dataSource = $"Data Source={outputFile};";

            using (var connection = new SqliteConnection(dataSource))
            {
                connection.Open();

                using var command = connection.CreateCommand();

                // --------------------------------
                // Rimboard table.

                command.CommandText =
                    @"
                        CREATE TABLE `Rimboard` (
                            `OriginalMessageId`        TEXT NOT NULL, -- Snowflake of original message.
                            `PinnedMessageId`          TEXT NOT NULL, -- Snowflake of pinned aka reposted message.
                            `OriginalMessageChannelId` TEXT NOT NULL, -- Snowflake of channel of original message.
                            `PinnedMessageChannelId`   TEXT NOT NULL  -- Snowflake of channel of pinned aka reposted message.
                        );
                    ";

                command.ExecuteNonQuery();

                // --------------------------------
                // Reminder table.

                command.CommandText =
                    @"
                        CREATE TABLE `Reminders` (
	                        `Id`            TEXT    NOT NULL, -- Snowflake of the original message that created the reminder.
	                        `UserId`        TEXT    NOT NULL, -- Snowflake of user who created the reminder
	                        `ChannelId`     TEXT    NOT NULL, -- Snowflake of channel the reminder was created in
	                        `Message`       TEXT            , -- Reminder message
	                        `TriggerTime`   INTEGER NOT NULL, -- Reminder trigger timestamp in minutes
	                        `Mentions`      TEXT              -- Whitespace separated mention strings
                        );
                    ";

                command.ExecuteNonQuery();

                // --------------------------------
                // Exclude system.

                command.CommandText =
                    @"
                        CREATE TABLE `Filter` (
                            `Type`      INTEGER unsigned NOT NULL DEFAULT '1', -- 1 REGEX 2 EXCLUDE
                            `String`    TEXT             NOT NULL              -- Mask of regex or exclude
                        );
                    ";

                command.ExecuteNonQuery();

                // --------------------------------
                // Role request.

                command.CommandText =
                    @"
                        CREATE TABLE `Roles` (
	                        `MessageId` TEXT    NOT NULL, -- Snowflake of message
	                        `RoleId`    TEXT    NOT NULL, -- Snowflake of role
	                        `IsUnicode` Integer NOT NULL, -- (Boolean) If emote data is unicode character.
	                        `EmoteData` TEXT    NOT NULL  -- Emote data (either unicode character or a ulong id)
                        );
                    ";

                command.ExecuteNonQuery();
            }
        }
#pragma warning restore IDE0063 
    }
}
