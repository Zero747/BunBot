using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using BigSister.ChatObjects;
using BigSister.Database;
using System.Runtime.InteropServices.ComTypes;
using DSharpPlus.Interactivity.Extensions;

namespace BigSister.Welcome
{
    public static partial class WelcomeSystem
    {
        public static async Task DoWelcomeMessage(DSharpPlus.DiscordClient c, DSharpPlus.EventArgs.GuildMemberAddEventArgs ctx)
        {
            //TODO make these settings
            DiscordChannel sendChannel = await Program.BotClient.GetChannelAsync(214523504673030144);

            await sendChannel.SendMessageAsync(content: $"{Generics.GetMention(ctx.Member.Id)}, welcome to the RimWorld Server!\n\nI'm here to convince you to join our colony, but first read through the <#265153830528876544> channel to make sure that you understand how to behave here. \n\nOnce you've done that, head to <#410118895526084609> and introduce yourself, our staff will give you the speaking role!\n\nHave fun!");

        }

        public static async Task DoLeaveMessage(DSharpPlus.DiscordClient c, DSharpPlus.EventArgs.GuildMemberRemoveEventArgs ctx)
        {

            DiscordChannel sendChannel = await Program.BotClient.GetChannelAsync(214523504673030144);

            await sendChannel.SendMessageAsync(content: $"{Generics.GetMention(ctx.Member.Id)} just left. Another one bites the dust.");

        }
    }
}
