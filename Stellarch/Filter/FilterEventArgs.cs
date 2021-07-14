// FilterEventArgs.cs
// Contains arguments detailing the bad words found and other context in a filter event.
//
// EMIKO

using DSharpPlus.Entities;
using System;

namespace BigSister.Filter
{
    public class FilterEventArgs : EventArgs
    {
        /// <summary>The message that triggered the event.</summary>
        public DiscordMessage Message;
        /// <summary>The user that sent the message.</summary>
        public DiscordUser User;
        /// <summary>The channel the message was sent in.</summary>
        public DiscordChannel Channel;
        /// <summary>The bad words found by the filter.</summary>
        public string[] BadWords;
        /// <summary>A string that emphasizes the found bad words.</summary>
        public string NotatedMessage;
    }
}