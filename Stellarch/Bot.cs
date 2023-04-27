// Bot.cs
// Initiates commands and events related to the bot and handles event handlers from custom classes. It also runs the bot and maintains a connection.
//


using System;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using BigSister.Mutes;
using BigSister.Reminders;
using BigSister.Commands;
using DSharpPlus.Entities;
using System.Text;
using BigSister.ChatObjects;
using BigSister.Welcome;

namespace BigSister
{
    public static class Bot
    {
        static Timer reminderTimer;
        static Timer muteTimer;

        public static async Task RunAsync(DiscordClient botClient)
        {
            // Configure timer. Set it up BEFORE registering events.
            reminderTimer = new Timer
            {
                Interval = 60000, // 1 minute
                AutoReset = true
            };

            muteTimer = new Timer
            {
                Interval = 60000, // 1 minute
                AutoReset = true
            };

            RegisterCommands(botClient);
            RegisterEvents(botClient);

            reminderTimer.Start();
            muteTimer.Start();

            await botClient.ConnectAsync();
            await Task.Delay(-1);
        }

        static void RegisterCommands(DiscordClient botClient)
        {
            var commands = botClient.GetCommandsNext();

            commands.RegisterCommands<FilterCommands>();
            commands.RegisterCommands<MuteCommands>();
            commands.RegisterCommands<ReminderCommands>();
            commands.RegisterCommands<RoleRequestCommands>();
            commands.RegisterCommands<ModerationCommands>();
            commands.RegisterCommands<SettingsCommands>();
        }
        static void RegisterEvents(DiscordClient botClient)
        {
            // ----------------
            // Filter
            botClient.MessageCreated += Filter.FilterSystem.BotClient_MessageCreated;
            botClient.MessageUpdated += Filter.FilterSystem.BotClient_MessageUpdated;
            
            // Filter triggered.
            Filter.FilterSystem.FilterTriggered += FilterSystem_FilterTriggered;

            // ----------------
            // Snooper
            botClient.MessageCreated += MentionSnooper.MentionSnooper.BotClientMessageCreated;
            botClient.MessageUpdated += MentionSnooper.MentionSnooper.BotClientMessageUpdated;

            // ----------------
            // Mute 
            botClient.GuildMemberAdded += MuteSystem.CheckMuteEvade;
            muteTimer.Elapsed += MuteSystem.MuteTimer_Elapsed;

            // ----------------
            // Welcome
            botClient.GuildMemberAdded += WelcomeSystem.DoWelcomeMessage;
            botClient.GuildMemberRemoved += WelcomeSystem.DoLeaveMessage;

            // ----------------
            // Admit
            botClient.MessageCreated += Admission.Admission.BotClient_MessageCreated;


            // ----------------
            // Reminder timer
            reminderTimer.Elapsed += ReminderSystem.ReminderTimer_Elapsed;

            // ----------------
            // Rimboard
            botClient.MessageReactionAdded += Rimboard.RimboardSystem.BotClientMessageReactionAdded;
            botClient.MessageReactionsCleared += Rimboard.RimboardSystem.BotClientMessageReactionsCleared;
            botClient.MessageDeleted += Rimboard.RimboardSystem.BotClientMessageDeleted;

            // ----------------
            // RoleRequest

            botClient.MessageReactionAdded += RoleRequest.RoleRequestSystem.MessageReactionAdded;
            botClient.MessageReactionRemoved += RoleRequest.RoleRequestSystem.MessageReactionRemoved;

            // ----------------
            // Auditing

            var commandsNext = botClient.GetCommandsNext();

            commandsNext.CommandExecuted += AuditSystem.Bot_CommandExecuted;

            // ----------------
            // Fun stuff

            botClient.MessageCreated += FunStuff.BotClientMessageCreated;
            botClient.Heartbeated += FunStuff.BotClient_Heartbeated;
        }

        /// <summary>Send a message to the filter channel.</summary>
        /// <param name="embed">The embed to send</param>
        /// <param name="text">The text to send</param>
        private static async Task NotifyFilterChannel(DiscordEmbed embed)
        {
            var auditChannel = await Program.BotClient.GetChannelAsync(Program.Settings.FilterChannelId);

            try
            {
                await auditChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
            catch
            {
                throw new ArgumentException("Audit channel not found.");
            }
        }

        private static async Task FilterSystem_FilterTriggered(Filter.FilterEventArgs e)
        {
            var stringBuilder = new StringBuilder();

            // Append all the found bad words to the string builder.
            foreach (string str in e.BadWords)
            {
                stringBuilder.Append(str);
                stringBuilder.Append(' ');
            }

            // Create the Discord Embed
            var deb = new DiscordEmbedBuilder()
            {
                Title = "Filter: Word Detected",
                Color = DiscordColor.Red
            };

            deb.WithDescription(String.Format("Filter Trigger(s):```{0}```Excerpt:```{1}```",
                stringBuilder.ToString(), e.NotatedMessage));

            deb.AddField(@"Author ID", e.User.Id.ToString(), inline: true);
            deb.AddField(@"Author Username", $"{e.User.Username}#{e.User.Discriminator}", inline: true);
            deb.AddField(@"Author Mention", e.User.Mention, inline: true);
            deb.AddField(@"Channel", e.Channel.Mention, inline: true);
            deb.AddField(@"Timestamp (UTC)", e.Message.CreationTimestamp.UtcDateTime.ToString(Generics.DateFormat), inline: true);
            deb.AddField(@"Link", Generics.GetMessageUrl(e.Message));

            deb.WithThumbnail(Generics.URL_FILTER_BUBBLE);

            // Notify the filter channel.
            await NotifyFilterChannel(deb.Build());
        }
    }
}
