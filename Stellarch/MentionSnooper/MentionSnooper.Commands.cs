// MentionSnooper.Commands.cs
// One piece of a partial class for snooping warns, specifically the commands-handling side of it.
//



using BigSister.ChatObjects;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigSister.MentionSnooper
{
    public static partial class MentionSnooper
    {
        public static async Task SeekWarns(CommandContext ctx, ulong[] memberIds)
        {   const int MAX_FIELDS = 10;            

            DiscordChannel actionLogChannel = await Program.BotClient.GetChannelAsync(Program.Settings.ActionChannelId);

            Dictionary<ulong, List<DiscordMessage>> warnDict =
                await QueryMemberMentions(memberIds.Distinct().ToList(), actionLogChannel, Program.Settings.MaxActionAgeMonths, ctx.Message);

            // Let's start paginating.
            var pages = new Page[warnDict.Keys.Count];
            int page = 0;

            if (warnDict.Keys.Count > 0)
            {
                // Want to generate a page for each member.
                foreach (var member in warnDict.Keys)
                {   

                    // We want a boolean to check first because if there's no key, we'll get an exception trying to get the count.
                    bool warnsFound = warnDict.ContainsKey(member) && warnDict[member].Count > 0;

                    var deb = new DiscordEmbedBuilder
                    {
                        Title = $"Mentions found in action logs",
                        Description = Generics.NeutralDirectResponseTemplate(mention: ctx.Member.Mention, 
                                        body: warnsFound ? // Warning, really fucking long string ahead:
                                        $"I found {warnDict[member].Count} mention{(warnDict[member].Count == 1 ? String.Empty : @"s")} for " +
                                        $"{Generics.GetMention(member)} in {actionLogChannel.Mention} in the last {Program.Settings.MaxActionAgeMonths} months. " +
                                        $"{(warnDict[member].Count > MAX_FIELDS ? $"There are over {MAX_FIELDS}. I will only show the most recent." : String.Empty)}" :
                                        $"{ctx.Member.Mention}, I did not find any mentions for {Generics.GetMention(member)}. Good for them..."),
                        Color = warnsFound ? Generics.NegativeColor : Generics.NeutralColor
                    };

                    if (warnsFound)
                    {   // Only continue here if there are actually warns, otherwise just slap a footer on.
                        foreach (var message in warnDict[member])
                        {   // Generate a field for each detected message.

                            if (deb.Fields.Count < MAX_FIELDS)
                            {   // Only continue if we have less than MAX_FIELDS fields.

                                // This SB is for all the content.
                                var stringBuilder = new StringBuilder();
                                // This SB is for all the misc information.
                                var stringBuilderFooter = new StringBuilder();

                                stringBuilder.Append($"{ChatObjects.Generics.GetMention(message.Author.Id)}: ");

                                stringBuilder.Append(message.Content);

                                if (message.Attachments.Count > 0)
                                {
                                    stringBuilderFooter.Append($"\n\n{Formatter.Bold(@"There is an image attached:")} ");

                                    stringBuilderFooter.Append(Formatter.MaskedUrl(@"Image", new Uri(message.Attachments[0].Url)));
                                } // end if

                                stringBuilderFooter.Append("\n\n");
                                stringBuilderFooter.Append(Formatter.MaskedUrl(@"Link", new Uri(Generics.GetMessageUrl(message))));

                                // We want to prefer the footer's information over the content. So let's figure out how much of the content we
                                // need to trim out.

                                var finalStringBuilder = new StringBuilder();

                                if (stringBuilder.Length + stringBuilderFooter.Length > 1000)
                                {   // We need to do some trimming.

                                    if (stringBuilder.Length > 0)
                                    {   // Let's get the content in there.
                                        finalStringBuilder.Append(Generics.BuildLimitedString(
                                            originalString: stringBuilder.ToString(), 
                                            endMessage: @". . . Unable to preview long message...",
                                            maxLength: 1000 - stringBuilderFooter.Length));
                                    }
                                    if (stringBuilderFooter.Length > 0)
                                    {   // Let's get the footer in there.
                                        finalStringBuilder.Append(stringBuilderFooter);
                                    }
                                }
                                else
                                {   // We don't need to do any trimming.
                                    if (stringBuilder.Length > 0)
                                    {   // Let's get the content in there.
                                        finalStringBuilder.Append(stringBuilder);
                                    }
                                    if (stringBuilderFooter.Length > 0)
                                    {   // Let's get the footer in there.
                                        finalStringBuilder.Append(stringBuilderFooter);
                                    }
                                }

                                deb.AddField($"Action on {message.Timestamp.ToString(Generics.DateFormat)}", finalStringBuilder.ToString());
                            }
                            else
                            {   // Stop the loop if we have MAX_FIELDS fields.
                                break; // NON-SESE ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! !
                            } // end else
                        } // end foreach
                    } // end if

                    deb.WithFooter($"Page {page + 1}/{warnDict.Keys.Count}");

                    pages[page++] = new Page(embed: deb);
                } // end foreach
            } // end if

            // Delete the message if it's the action channel so it's kind of out of the way and doesn't get logged again in the future.
            if (ctx.Message.ChannelId == Program.Settings.ActionChannelId)
            {
                await ctx.Message.DeleteAsync();
            }

            if (pages.Length > 1)
            {   // More than 1 page.
                var interactivity = Program.BotClient.GetInteractivity();

                await interactivity.SendPaginatedMessageAsync
                    (
                        channel: ctx.Channel,
                        user: ctx.User,
                        pages: pages,
                        emojis: Generics.DefaultPaginationEmojis
                    );
            }
            else
            {   // Only one page, we want to send it as a regular embed instead.
                var anotherDeb = new DiscordEmbedBuilder(pages[0].Embed);

                // Clear the footer. We don't want the page count.
                anotherDeb.WithFooter(null, null);

                await ctx.Channel.SendMessageAsync(embed: anotherDeb);
            }
        }
    }
}
