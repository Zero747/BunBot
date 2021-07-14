// MentionCommands.cs
// Contains commands for looking up mentions:
//  !mentions <users>
//  !mentions <userIds>
//
//   (\_/)
//   (>.<)
//   (")_(")
//
// EMIKO

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BigSister.ChatObjects;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace BigSister.Commands
{
    class ModerationCommands : BaseCommandModule
    {
        static readonly Regex UserIdLookupRegex = 
            new Regex(@"(\d{17,})", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        [Command("mentions"), MinimumRole(Role.CS)]
        public async Task MentionsLookup(CommandContext ctx, 
                                            params string[] users)
        {
            if (await Permissions.HandlePermissionsCheck(ctx))
            {
                // Convert the string[] into a ulong[]

                ulong[] userIds = new ulong[users.Length];
                bool validArguments = true;

                for (int i = 0; i < users.Length && validArguments; i++)
                {
                    Match m = UserIdLookupRegex.Match(users[i]);

                    if (m.Success && ulong.TryParse(m.Value,
                                        out
                                        ulong a))
                    {
                        userIds[i] = a;
                    }
                    else
                    {
                        validArguments = false;
                    }
                }

                // Check if all arguments are valid. If any are not, we tell the user they fucked it up.
                if (validArguments)
                {
                    await MentionsLookupUnwrapper(ctx, userIds);
                } 
                else
                {
                    await GenericResponses.HandleInvalidArguments(ctx);
                }
            }
        }

        /// <summary>Gets information about a user's account creation date and server join date.</summary>
        [Command("userinfo"),
         MinimumRole(Role.CS)]
        public async Task GetUserInfo(CommandContext ctx,
                                        DiscordMember member)
        {
            const string RECENT_MSG = @"In the last minute...";
            // Check if the user can use commands.
            if (await Permissions.HandlePermissionsCheck(ctx))
            {

                await ctx.Channel.TriggerTypingAsync();

                if (ctx.Guild.Members.ContainsKey(member.Id))
                {   // This member exists in the guild.
                    // DEB!
                    var deb = Generics.GenericEmbedTemplate(
                        color: Generics.NeutralColor,
                        description: Generics.NeutralDirectResponseTemplate(
                            mention: ctx.Member.Mention,
                            body: $"here's some info about that user!"),
                        title: @"User Info",
                        thumbnail: member.AvatarUrl);

                    deb.AddField(@"Mention", member.Mention, true);
                    deb.AddField(@"Username", $"{member.Username}#{member.Discriminator}", true);
                    deb.AddField(@"Joined Discord", Generics.GetRemainingTime(GetJoinedDiscordTime(member.Id), false, RECENT_MSG, @"ago"));
                    deb.AddField($"Joined {ctx.Guild.Name}", Generics.GetRemainingTime(member.JoinedAt, false, RECENT_MSG, @"ago"));

                    await ctx.Channel.SendMessageAsync(embed: deb);
                }
                else
                {   // This member does not exist in the guild.
                    await ctx.Channel.SendMessageAsync(
                        embed: Generics.GenericEmbedTemplate(
                            color: Generics.NegativeColor,
                            description: @"Unable to find that user...",
                            thumbnail: member.AvatarUrl,
                            title: @"Cannot get user info"));
                } // end else
            } // end if
        } // end method

        /// <summary>Retrieves the user's Discord join date by their snowflake.</summary>
        /// <param name="id">Snowflake</param>
        private static DateTimeOffset GetJoinedDiscordTime(ulong id)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds((long)(id >> 22) + 1420070400000);
        }

        public async Task MentionsLookupUnwrapper(CommandContext ctx, ulong[] users)
            => await MentionSnooper.MentionSnooper.SeekWarns(ctx, users);
    }
}
