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


            //filter message
            var user = await e.Guild.GetMemberAsync(e.Author.Id, true);
            bool contentBad = GetBadWords(user.DisplayName + "\n" + e.Message.Content, out string notatedMessage).Count > 0; // The detected bad words.

            //generate message

            if(contentBad)
            {
                string message = "{0} your display name or message is in violation of the rules, please message a member of staff for assistance";
                await e.Channel.SendMessageAsync(content: String.Format(message, arg0: e.Author.Mention));
            }
            else //assign role
            {
                DiscordRole colonistRole = e.Guild.GetRole(934494665539993611);
                //only do if not colonist
                if (!user.Roles.Contains(colonistRole))
                {
                    await user.GrantRoleAsync(colonistRole);
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
