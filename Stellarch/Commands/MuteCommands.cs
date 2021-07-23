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

namespace BigSister.Commands
{
    class MuteCommands : BaseCommandModule
    {
        [Command("mute"), MinimumRole(Role.CS)]
        public async Task MuteUser(CommandContext ctx)
        {
            await GenericResponses.SendGenericCommandError(
                            ctx.Channel,
                            ctx.Member.Mention,
                            "Syntax error",
                            "The syntax is: mute <mention> <time> <reason>.");
        }


        //Mute mentioned user
        [Command("mute"), MinimumRole(Role.CS)]
        public async Task MuteUser(CommandContext ctx, 
                                            DiscordMember user, 
                                            [RemainingText]
                                            string args = @"")
        {
            // Check if they have the permissions to call this command.
            if (await Permissions.HandlePermissionsCheck(ctx))
            {
                 await MuteSystem.AddMute(ctx, user, args);
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
                            "Unable to remove reminder",
                            $"The reminder id `{args}` does not exist...");
                }
            }
        }

    }
}
