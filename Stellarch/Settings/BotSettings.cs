// BotSettings.cs
// A static class for every bot setting including their default values.
//
// EMIKO.

using System;
using System.Collections.Generic;
using System.Text;

namespace BigSister.Settings
{
    public class BotSettings
    {
        /// <summary>Default bot settings.</summary>
        public static BotSettings Default = new BotSettings();

        /// <summary>Default channel, FDS: #dont-ever-delete.</summary>
        /// I have it sent to a channel in my development server.
        const ulong DEFAULT_CHANNEL = 564290158078197781;

        public BotSettings() { }

        // ----------------
        // Filter

        /// <summary>Channels excluded from filtering.</summary>
        /// Not set to any default channels. Just want to initiate it so it isn't null.
        public List<ulong> ExcludedChannels = new List<ulong>();
        /// <summary>The filter channel.</summary>
        public ulong FilterChannelId = DEFAULT_CHANNEL;

        // ----------------
        // Reminder

        /// <summary>Maximum reminder time allowed in months.</summary>
        public int MaxReminderTimeMonths = 12;

        // ----------------
        // Mention snooper

        /// <summary>ID of #action-logs</summary>
        public ulong ActionChannelId = DEFAULT_CHANNEL;
        /// <summary>Maximum timespan for an action to be considered.</summary>
        public int MaxActionAgeMonths = 6;
        /// <summary>If passive warn snooping is enabled.</summary>
        public bool AutoWarnSnoopEnabled = false;

        // ----------------
        // Rimboard

        /// <summary>If Rimboard is enabled or not.</summary>
        public bool RimboardEnabled = false;
        /// <summary>ID of the Rimboard channel.</summary>
        public ulong RimboardChannelId = DEFAULT_CHANNEL;
        /// <summary>Webhook ID of the Rimboard channel.</summary>
        /// I have it set to a webhook in my development server.
        public ulong RimboardWebhookId = 864991491741843487;
        /// <summary>The Rimboard's pinning emote.</summary>
        public EmojiData RimboardEmoticon = new EmojiData(432227067313127424);
        /// <summary>Number of reactions needed to repost a message to Rimboard.</summary>
        public int RimboardReactionsNeeded = 3;
        /// <summary>Number of reactions needed to pin a message in Rimboard.</summary>
        public int RimboardPinReactionsNeeded = 5;

        // ----------------
        // FunStuff

        /// <summary>If fun is allowed or not.</summary>
        public bool FunAllowed = false;
        /// <summary>The per user cooldown in ms.</summary>
        public ulong PerUserCooldownMilliseconds = 300000;
    }
}
