// EmojiData.cs
// Casts a unicode string or a discord emote id to an DiscordEmoji type.
//
// EMIKO

using DSharpPlus;
using DSharpPlus.Entities;
using System;

namespace BigSister
{
    public struct EmojiData
    {
        /// <summary>The main data of the emoji. Can either be a string or a ulong emote id.</summary>
        public string Value;
        /// <summary>If the emoji is a unicode character.</summary>
        public bool IsUnicodeCharacter;

        /// <summary>Get a new instance of this struct with a unicode character.</summary>
        public EmojiData(string unicodeCharacter)
        {
            Value = unicodeCharacter;
            IsUnicodeCharacter = true;
        }

        /// <summary>Get a new instance of this struct with an emote id.</summary>
        public EmojiData(ulong emoteId)
        {
            Value = emoteId.ToString();
            IsUnicodeCharacter = false;
        }

        public EmojiData(string value, bool isUnicodeCharacter)
        {
            Value = value;
            IsUnicodeCharacter = isUnicodeCharacter;
        }

        public EmojiData(DiscordEmoji discordEmoji)
        {
            // Check if it's a unicode character or an emote id.
            IsUnicodeCharacter = discordEmoji.Id == 0;

            switch(IsUnicodeCharacter)
            {
                case true: Value = discordEmoji.Name;
                    break;
                case false: Value = discordEmoji.Id.ToString();
                    break;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is EmojiData data &&
                   Value == data.Value &&
                   IsUnicodeCharacter == data.IsUnicodeCharacter;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, IsUnicodeCharacter);
        }
    }

    public static class EmojiConverter
    {
        /// <summary>Gets a DiscordEmoji from a unicode value/id and a boolean indicating if it's a unicode character.</summary>
        public static DiscordEmoji GetEmoji(BaseDiscordClient cl, string value, bool isUnicodeChar)
        {
            DiscordEmoji discordEmoji_returnVal;

            // Check if it's a unicode character or an emote id.
            if (isUnicodeChar)
            {   // Unicode character
                discordEmoji_returnVal = DiscordEmoji.FromUnicode(value);
            }
            else
            {   // Emote id
                try
                {
                    discordEmoji_returnVal = DiscordEmoji.FromGuildEmote(cl, ulong.Parse(value));
                }
                catch (FormatException e)
                {
                    throw new FormatException(@"Tried to convert a non-ulong type to a Discord emoji id.", e);
                }
            }

            return discordEmoji_returnVal;
        }

        public static DiscordEmoji GetEmoji(BaseDiscordClient cl, EmojiData data)
            => GetEmoji(cl, data.Value, data.IsUnicodeCharacter);

        // Thank you DSharpPlus for malforming emote strings that have unicode characters. I really appreciate it. It makes my life so easy because
        // I have to write this fucking method for you.
        public static string GetEmojiString(DiscordEmoji emoji)
        {
            string returnVal;

            if(emoji.Id == 0)
            {
                returnVal = emoji.Name;
            }
            else
            {
                returnVal = Formatter.Emoji(emoji);
            }

            return returnVal;
        }
    }
}
