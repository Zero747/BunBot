// PinInfo.cs
// Contains information about a pinned message.

using System;
using System.Collections.Generic;
using System.Text;

namespace BigSister.Rimboard
{
    public struct PinInfo
    {
        public static PinInfo Invalid = new PinInfo(default, default, default, default, default);

        /// <summary>The pinned message (in the rimboard channel).</summary>
        public ulong PinnedMessageId { get; }
        /// <summary>The message's channel ID.</summary>
        public ulong OriginalChannelId { get; }
        /// <summary>The channel the message was pinned in.</summary>
        public ulong PinnedChannelId { get; }
        /// <summary>The original message.</summary>
        public ulong OriginalMessageId { get; }
        /// <summary>The number of reacts the original message has.</summary>
        public int OriginalReactCount { get; }

        public PinInfo(ulong pinnedMessageId, ulong originalMessageId, ulong pinnedChannelId, ulong originalChannelId, int originalReactCount)
        {
            PinnedMessageId    = pinnedMessageId;
            PinnedChannelId    = pinnedChannelId;
            OriginalMessageId  = originalMessageId;
            OriginalChannelId  = originalChannelId;
            OriginalReactCount = originalReactCount;
        }

        public override bool Equals(object obj)
        {
            return obj is PinInfo info &&
                   OriginalMessageId == info.OriginalMessageId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(OriginalMessageId);
        }
    }
}
