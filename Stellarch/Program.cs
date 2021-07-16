// Program.cs
// The main entry into the program, obviously. Here we have:
//  - Commands that relate to the program itself such as saving/loading, SQL handling, auditing, logging, and CLI input processing.
//  - A space to load the settings before initiating the bot. 
//  - Initiating the auditing system so we can immediately start auditing when the bot starts up.
//


#define DEBUG

using System;
using System.IO;
using Newtonsoft.Json;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using BigSister.Settings;
using BigSister.Database;
using System.Text;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;

namespace BigSister
{
    public static class Program
    {
        public static class Files
        {
            public static string ExecutableDirectory
            {
                get => AppDomain.CurrentDomain.BaseDirectory;
            }

            public const string SAVE_DIRECTORY = @"sav";
            public static string SaveDirectory
            {
                get => Path.Combine(ExecutableDirectory, SAVE_DIRECTORY);
            }

            const string IDENTITY_FILE = @"identity0.json";
            public static string IdentityFile
            {
                get => Path.Combine(ExecutableDirectory, SAVE_DIRECTORY, IDENTITY_FILE);
            }

            const string SETTINGS_FILE = @"settings.json";
            public static string SettingsFile
            {
                get => Path.Combine(ExecutableDirectory, SAVE_DIRECTORY, SETTINGS_FILE);
            }

            const string FUN_FILE = @"botReactions.json";
            public static string FunFile
            {
                get => Path.Combine(ExecutableDirectory, SAVE_DIRECTORY, FUN_FILE);
            }

            const string LOG_FILE = @"log.txt";
            public static string LogFile
            {
                get => Path.Combine(ExecutableDirectory, SAVE_DIRECTORY, LOG_FILE);
            }

            const string DATABASE_FILE = @"database.db";
            public static string DatabaseFile
            {
                get => Path.Combine(ExecutableDirectory, SAVE_DIRECTORY, DATABASE_FILE);
            }

            public const string RIMBOARD_DIR = @"temp";
            public static string RimboardTempFileDirectory
            {
                get => Path.Combine(ExecutableDirectory, RIMBOARD_DIR);
            }
        }

        public static BotSettings Settings;
        public static DiscordClient BotClient;

        public const string Prefix = @"~";

        static SaveFile BotSettingsFile;
        static Identity Identity;

        static void Main(string[] args)
        {
            if(args.Length > 1)
            {
                if(args[0].Equals("-md5"))
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append(args[1..]);

                    string baseFile = stringBuilder.ToString();

#pragma warning disable IDE0063
                    using (StreamReader sr = new StreamReader(baseFile))
                    {
                        var tempSaveFile = new SaveFile(baseFile);
                        tempSaveFile.Save(sr.ReadToEnd());
                    }
#pragma warning restore IDE0063
                }

                Environment.Exit(0);
            }

            bool loadSuccess;

            // ----------------
            // Initiate folders

            // Check if the save directory exists.
            if (!Directory.Exists(Files.SaveDirectory))
            {
                Console.Write("Folder {0} not found, creating directory... ", Files.SAVE_DIRECTORY);
                Directory.CreateDirectory(Files.SaveDirectory);
                Console.WriteLine("Created!");
            }

            // Check if the rimboard temp file directory exists.
            if (!Directory.Exists(Files.RimboardTempFileDirectory))
            {   
                Console.Write("Folder {0} not found, creating directory... ", Files.RIMBOARD_DIR);
                Directory.CreateDirectory(Files.RimboardTempFileDirectory);
                Console.WriteLine("Created!");
            }

            // ----------------
            // Load authkey and webhooks.
            Console.Write("Loading identity... ");
            Identity = LoadIdentity();
            Console.WriteLine("Found authkey and {0} webhook{1}.",
                Identity.Webhooks.Count,
                Identity.Webhooks.Count == 1 ? '\0' : 's');

            // ----------------
            // Load bot settings.
            Console.Write("Loading settings... ");
            loadSuccess = LoadSettings(out Settings);
            Console.WriteLine(loadSuccess ? @"Successfully loaded" : @"No file - Used default values.");

            // ----------------
            // TODO: Initiate auditing.

            // ----------------
            // Initiate database.
            Console.Write("Looking for database file... ");
            string localDbPath = Path.GetRelativePath(Files.ExecutableDirectory, Files.DatabaseFile);
            if (File.Exists(Files.DatabaseFile)) // DB found
                Console.WriteLine("Found {0}!", localDbPath);
            else
            { // DB not found
                Console.WriteLine("No database - Instantiating default {0}.", localDbPath);
                BotDatabase.GenerateDefaultFile(Files.DatabaseFile);
            }

            // ----------------
            // Load fun stuff settings.
            Console.Write("Loading fun stuff... ");
            loadSuccess = FunStuff.LoadFunStuffJson(Files.FunFile);
            Console.WriteLine(loadSuccess ? @"Successfully loaded" : @"No file - No fun allowed.");

            if(Settings.FunAllowed && !loadSuccess)
            {
                UpdateSettings(ref Settings.FunAllowed, false);
            }

            // ----------------
            // Run the bot.

            var botConfig = new DiscordConfiguration()
            {
                Token = Identity.Authkey,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            BotClient = new DiscordClient(botConfig);

            BotClient.UseCommandsNext(new CommandsNextConfiguration()
            {
                CaseSensitive = false,
                EnableDefaultHelp = false,
                EnableDms = false,
                StringPrefixes = new string[] { Prefix }
            });

            BotClient.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = DSharpPlus.Interactivity.Enums.PaginationBehaviour.Ignore
            });

            // ----------------
            // Initialize static classes.

            Filter.FilterSystem.Initialize();
            WebhookDelegator.Initialize(Identity.Webhooks);


            Bot.RunAsync(BotClient).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>Load authkey and webhooks.</summary>
        static Identity LoadIdentity()
        {
            Identity identity_returnVal;

            if (File.Exists(Files.IdentityFile))
            {
                string identityFileContents;

                // Read the identity file's contents
                using (StreamReader sr = new StreamReader(Files.IdentityFile))
                {
                    identityFileContents = sr.ReadToEnd();
                }

                // Deserialize the object.
                identity_returnVal = JsonConvert.DeserializeObject<Identity>(identityFileContents);
            } 
            else
            {
                throw new FileNotFoundException("Unable to find identity.json.");
            }

            return identity_returnVal;
        }

        /// <summary>Update a setting and save if desired.</summary>
        /// <param name="setting">Reference of the setting to update.</param>
        /// <param name="newVal">New value of the setting.</param>
        /// <param name="save">If should be immediately save to disk.</param>
        public static void UpdateSettings<T>(ref T setting, T newVal, bool save = true)
        {
            setting = newVal;

            if(save)
            {
                SaveSettings();
            }
        }

        /// <summary>Save settings as is.</summary>
        public static void SaveSettings()
        {
            BotSettingsFile.Save<BotSettings>(Settings);
        }
        
        /// <summary>Load the bot's settings.</summary>
        /// Doesn't have to be asynchronous because I don't care what performance the bot has upon start.
        static bool LoadSettings(out BotSettings botSettings)
        {
            bool loadedValues_returnVal;

            BotSettingsFile = new SaveFile(Files.SettingsFile);

            // Check if it's an existing save file.
            if (BotSettingsFile.IsExistingSaveFile())
            {   // It's an existing file, so let's get the values.
                loadedValues_returnVal = true;

                botSettings = BotSettingsFile.Load<BotSettings>();
            }
            else
            {   // It's not an existing file, so let's use default values and then save them.
                botSettings = new BotSettings();
                loadedValues_returnVal = false;

                BotSettingsFile.Save<BotSettings>(botSettings);
            }

            return loadedValues_returnVal;
        }
    }
}
