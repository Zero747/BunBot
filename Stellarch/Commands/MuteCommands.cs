// MuteCommands.cs
// Contains methods for adding, removing, or listing reminders:
//  !reminder new/add <timeFrame> <message> <mentions> 
//  !reminder remove/delete <reminderId>
//  !reminder list
//


using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using BigSister.ChatObjects;
using BigSister.Mutes;
using System.Text.RegularExpressions;

namespace BigSister.Commands
{
    class MuteCommands : BaseCommandModule
    {
        static readonly Regex UserIdLookupRegex =
            new Regex(@"(\d{17,})", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        [Command("mute"), MinimumRole(Role.CS)]
        public async Task MuteUser(CommandContext ctx, string arg = "")
        {
            await GenericResponses.SendGenericCommandError(
                            ctx.Channel,
                            ctx.Member.Mention,
                            @"Unable to add mute",
                            @"I was unable able to add the mute you gave me. The syntax is mute <mention> <time> <reason>.");
        }


        //Mute mentioned user
        [Command("mute"), MinimumRole(Role.CS)]
        public async Task MuteUser(CommandContext ctx,
                                            string user, 
                                            [RemainingText]
                                            string args = @"")
        {
            bool success = false;
            ulong userId = 0;
            Match m = UserIdLookupRegex.Match(user);

            if (m.Success && ulong.TryParse(m.Value,
                                out
                                ulong a)) // This section is shamelessly stolen from ModerationCommands.cs (including the lookup regex), no I will not say sorry.
            {
                userId = a;
                success = true;
            }
            // Check if they have the permissions to call this command.
            if (await Permissions.HandlePermissionsCheck(ctx) && success)
            {
                 await MuteSystem.AddMute(ctx, userId, args);
            }
            else
            {
                await GenericResponses.SendGenericCommandError(
                            ctx.Channel,
                            ctx.Member.Mention,
                            @"Unable to add mute",
                            @"I was unable able to add the mute you gave me. The syntax is mute <mention> <time> <reason>.");
            }
        }

        //lists mutes
        [Command("mute-list"), MinimumRole(Role.CS)]
        public async Task MuteList(CommandContext ctx)
        {
            // Check if they have the permissions to call this command.
            if (await Permissions.HandlePermissionsCheck(ctx))
            {
                await MuteSystem.ListMutes(ctx);
            }
        }

        //removes mute via DB ID
        [Command("mute-remove"), MinimumRole(Role.CS)]
        public async Task MuteRemove(CommandContext ctx, [RemainingText] string args = @"")
        {
            // Check if they have the permissions to call this command.
            if (await Permissions.HandlePermissionsCheck(ctx))
            {
                Mute possibleMute = await MuteSystem.GetMuteFromDatabase(args);

                if (!possibleMute.Equals(Mute.Invalid))
                {   // It's a reminder.
                    await MuteSystem.RemoveMute(ctx, possibleMute);
                }
                else
                {   // It's not a reminder.
                    await GenericResponses.SendGenericCommandError(
                            ctx.Channel,
                            ctx.Member.Mention,
                            "Unable to remove mute",
                            $"The mute id `{args}` does not exist...");
                }
            }
        }

        //removes mute via mention
        [Command("unmute"), MinimumRole(Role.CS)]
        public async Task MuteRemove(CommandContext ctx, DiscordMember user)
        {
            // Check if they have the permissions to call this command.
            if (await Permissions.HandlePermissionsCheck(ctx))
            {

                await MuteSystem.RemoveUserMute(ctx, user);

            }
        }


    }
}
