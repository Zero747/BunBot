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

        private static List<ulong> newDiscordUsers = new List<ulong>();
        private static async Task AdmitUser(MessageCreateEventArgs e)
        {
            //check channel
            if(!Program.Settings.AdmissionEnabled || e.Channel.Id != 410118895526084609 || e.Author.IsBot) { // hardcoded barracks ID cause I'm lazy
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
                DateTimeOffset cutoffDate = user.JoinedAt.AddDays(-Program.Settings.DaysSinceCreation);
                bool userAlreadyEnteredBefore = newDiscordUsers.Contains(user.Id); // If the user already entered once within a month of account creation date

                DiscordRole colonistRole;
                if(user.CreationTimestamp >= cutoffDate || userAlreadyEnteredBefore)
                {
                    if(!userAlreadyEnteredBefore)
                    {
                        newDiscordUsers.Add(user.Id);
                    }
                    colonistRole = e.Guild.GetRole(1291125653999058955); // If the account is less than a month old after joining the discord, we are giving them a separate role to prevent giving new alt accounts certain role perms.
                }
                else
                {
                    colonistRole = e.Guild.GetRole(934494665539993611);
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
            var user = e.Member;
            var newColonistRole = e.Guild.GetRole(934494665539993611);
            var oldColonistRole = e.Guild.GetRole(1291125653999058955);

            if(user.Roles.Contains(oldColonistRole))
            {
                await user.RevokeRoleAsync(oldColonistRole);
            }
            if (!user.Roles.Contains(newColonistRole))
            {
                await user.GrantRoleAsync(newColonistRole);
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
            if (e.RolesAfter.Count - e.RolesBefore.Count != 0)
            {
                List<ulong> roleList = e.RolesAfter.Select(x => x.Id).ToList();
                ulong oldColonistRoleID = 1291125653999058955;
                ulong levelRoleID = 793924952465735700;
                if (roleList.Contains(levelRoleID) && roleList.Contains(oldColonistRoleID)) // If the user already given the normal colonist role, don't check again.
                {
                    await AdmitActiveUser(e);
                }
            }
        }
    }
}
