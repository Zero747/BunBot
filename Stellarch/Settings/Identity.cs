// Identity.cs
// Contains information about the bot's identity, notably its authkey and webhooks.
//
// EMIKO.

using System;
using System.Collections.Generic;
using System.Text;

namespace BigSister.Settings
{
    public class Identity
    {
        /// <summary>Authkey.</summary>
        public string Authkey = String.Empty;
        /// <summary>A list of webhooks.</summary>
        /// <remarks>ulong is channel ID and string is webhook ID.</remarks>
        public Dictionary<ulong, string> Webhooks = new Dictionary<ulong, string>();
    }
}
