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
                DateTimeOffset cutoffDate = new DateTimeOffset(e.Message.CreationTimestamp.UtcDateTime).AddDays(-14);
                DiscordRole colonistRole = e.Guild.GetRole(934494665539993611);
                if (GetJoinedDiscordTime(e.Author.Id) >= cutoffDate)
                {
                    colonistRole = e.Guild.GetRole(1291125653999058955); // If the account is less than 2 weeks old from creation of the message, we are giving them a separate role to prevent giving new alt accounts certain role perms.
                }
                //only do if not colonist
                if (!user.Roles.Contains(colonistRole))
                {
                    await user.GrantRoleAsync(colonistRole);
                    await e.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
                }
                
            }

        }

        private static async Task AdmitActiveUser(GuildMemberUpdateEventArgs e)
        {
            var roleList = e.RolesAfter.Except(e.RolesBefore).ToList();
            var user = e.Member;

            var newColonistRole = e.Guild.GetRole(934494665539993611);
            var oldColonistRole = e.Guild.GetRole(1291125653999058955);

            if (roleList.Contains(e.Guild.GetRole(793924952465735700)) && !user.Roles.Contains(newColonistRole)) // If user has been given the Level 10 role and doesn't already have colonist, run the following
            {
                await user.GrantRoleAsync(newColonistRole);
                await user.RevokeRoleAsync(oldColonistRole);
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

        internal static async Task BotClient_GuildMemberUpdated(DiscordClient botClient, GuildMemberUpdateEventArgs e)
        {
            if(e.RolesAfter.Count - e.RolesBefore.Count > 0)
            {
                await AdmitActiveUser(e);
            }
        }

        private static DateTimeOffset GetJoinedDiscordTime(ulong id)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds((long)(id >> 22) + 1420070400000);
        }
    }
}
