using DSharpPlus.EventArgs;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BigSister.Filter.FilterSystem;
using BigSister.Mutes;
using DSharpPlus.Entities;
using System.Diagnostics.Metrics;
using DSharpPlus.CommandsNext.Converters;

namespace BigSister.Admission
{
    public static class Admission
    {
        

        private static async Task AdmitUser(MessageCreateEventArgs e)
        {
            //check channel
            if(!(e.Channel.Id == 410118895526084609)) { // hardcoded barracks ID cause I'm lazy
                return;
            }

            bool usernameBad = false;
            bool messageBad = false;

            //filter message

            var badWords = GetBadWords(e.Author.Username, out string notatedMessage); // The detected bad words.
            usernameBad = badWords.Count > 0;
            badWords = GetBadWords(e.Message.Content, out notatedMessage);
            messageBad = badWords.Count > 0;

            //generate message

            if(usernameBad || messageBad)
            {
                string message = "{0} your username or message is in violation of the rules, please message a member of staff for assistance";
                await e.Channel.SendMessageAsync(content: String.Format(message, arg0: e.Author.Mention));
            }
            else //assign role
            {
                var callingMember = await e.Guild.GetMemberAsync(e.Author.Id);
                DiscordRole colonistRole = e.Guild.GetRole(934494665539993611);
                //only do if not colonist
                if (!callingMember.Roles.Contains(colonistRole))
                {
                    await callingMember.GrantRoleAsync(colonistRole);
                    await e.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
                }
                
            }

        }



        internal static async Task BotClient_MessageCreated(DiscordClient botClient, MessageCreateEventArgs e)
        {
            if (!e.Channel.IsPrivate && !e.Author.IsCurrent &&
                (e.Message.MessageType == MessageType.Default || e.Message.MessageType == MessageType.Reply))
            {
                await AdmitUser(e);
            }
        }

    }
}
