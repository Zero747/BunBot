// RimboardSystem.cs
// Contains the backend for the Rimboard system such as queries and event handlers.
//


using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using BigSister.ChatObjects;
using BigSister.Database;
using BigSister.Settings;

namespace BigSister.Rimboard
{
    public static class RimboardSystem
    {
       

        /// <summary>Query to get a message.</summary>
        const string QQ_RimboardSelectMessage = @"SELECT `OriginalMessageId`, `PinnedMessageId`, `OriginalMessageChannelId`, `PinnedMessageChannelId` FROM `Rimboard` WHERE `OriginalMessageId`=$originalMessageId;";
        /// <summary>Query to get a message from rimboard id.</summary>
        const string QQ_RimboardSelectMessageViaId = @"SELECT `OriginalMessageId`, `PinnedMessageId`, `OriginalMessageChannelId`, `PinnedMessageChannelId` FROM `Rimboard` WHERE `PinnedMessageId`=$pinnedMessageId;";
        /// <summary>Query to add an entry.</summary>
        const string QQ_AddEntry = @"INSERT INTO `Rimboard` (`OriginalMessageId`, `PinnedMessageId`, `OriginalMessageChannelId`, `PinnedMessageChannelId`) VALUES ($originalMessageId, $pinnedMessageId, $originalChannelId, $originalPinChannelId);";
        /// <summary>Query to remove an entry.</summary>
        const string QQ_RemoveEntry = @"DELETE FROM `Rimboard` WHERE `PinnedMessageId`=$pinnedMessageId;";

        static readonly Func<SqliteDataReader, object> ParsePinInfo = delegate (SqliteDataReader reader)
        {
            Tuple<ulong, ulong, ulong, ulong> info;

            if (reader.Read())
            {
                info = new Tuple<ulong, ulong, ulong, ulong>(
                    item1: ulong.Parse(reader.GetString(0)),
                    item2: ulong.Parse(reader.GetString(1)),
                    item3: ulong.Parse(reader.GetString(2)),
                    item4: ulong.Parse(reader.GetString(3)));
            }
            else
            {
                info = new Tuple<ulong, ulong, ulong, ulong>(0, 0, 0, 0);
            }

            return info;
        };

        static async Task<PinInfo> QueryDatabaseForOriginalMessage(ulong originalId, int reactCount)
        {
            PinInfo pinInfo_returnVal;

            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_RimboardSelectMessage
            };

            SqliteParameter a = new SqliteParameter("$originalMessageId", originalId.ToString())
            {
                DbType = DbType.String
            };

            command.Parameters.Add(a);

            Tuple<ulong, ulong, ulong, ulong> values =
                (Tuple<ulong, ulong, ulong, ulong>)await BotDatabase.Instance.ExecuteReaderAsync(command, processAction: ParsePinInfo);


            if (values.Item1 != 0 && values.Item2 != 0)
            {   // We have stuff in the dictionary.

                pinInfo_returnVal = new PinInfo(
                    originalMessageId: values.Item1,
                    pinnedMessageId: values.Item2,
                    originalChannelId: values.Item3,
                    pinnedChannelId: values.Item4,
                    originalReactCount: reactCount);

            }
            else
            {
                pinInfo_returnVal = PinInfo.Invalid;
            }

            return pinInfo_returnVal;
        }

        internal static async Task BotClientMessageReactionsCleared(DiscordClient botClient, MessageReactionsClearEventArgs e)
        {
            // Before anything, let's make sure that...
            //  1) This is not the rimboard channel
            //  2) Rimboard is enabled.
            //  3) The Rimboard webhook is not default.
            if (e.Channel.Id != Program.Settings.RimboardChannelId &&
                Program.Settings.RimboardEnabled &&
                Program.Settings.RimboardWebhookId != BotSettings.Default.RimboardWebhookId)
            {
                // Let's now try to get the Rimboard message.
                PinInfo pinInfo = await QueryDatabaseForOriginalMessage(e.Message.Id, 0);
                bool validPinInfo = !pinInfo.Equals(PinInfo.Invalid);

                if (validPinInfo)
                {
                    var pinnedMessage = await e.Guild.GetChannel(pinInfo.PinnedChannelId)
                                                .GetMessageAsync(pinInfo.PinnedMessageId);

                    var a = RemovePinFromDatabase(pinInfo);
                    var b = pinnedMessage.DeleteAsync();

                    await Task.WhenAll(a, b);
                }
            }
        }

        internal static async Task BotClientMessageReactionAdded(DiscordClient botClient, MessageReactionAddEventArgs e)
        {

            //short circuit, don't do anything if it's not the rimboard emote
            EmojiData react = new EmojiData(e.Emoji);
            if (react.Value != Program.Settings.RimboardEmoticon.Value)
            {
                return;
            }

            // We don't want the cached version of this message because if it was sent during downtime, the bot won't be able to do
            // anything with it.
            var message_noCache = await e.Channel.GetMessageAsync(e.Message.Id);

            // Before anything, let's make sure that...
            //  1) This is not the rimboard channel
            //  2) This isn't an excluded channel
            //  3) Rimboard is enabled.
            //  4) The Rimboard webhook is not default.
            //  5) This was not sent by a bot (requires nocache).
            if (e.Channel.Id != Program.Settings.RimboardChannelId &&
                !Program.Settings.RimboardExcludedChannels.Contains((ulong)(e.Channel.IsThread ? e.Channel.ParentId : e.Channel.Id)) &&
                Program.Settings.RimboardEnabled &&
                Program.Settings.RimboardWebhookId != BotSettings.Default.RimboardWebhookId &&
                // De-cache the message so we can get its author.
                !(message_noCache.Author.IsBot))
            {  
                DiscordEmoji emoji = GetReactionEmoji(botClient);


                // This contains a list of the reactions that have rimboardEmoji. It's only ever really going to be be 1 long.
                var pinReactionsList = message_noCache.Reactions.Where(a => a.Emoji.Name == emoji.Name).ToArray();

                if (pinReactionsList.Length == 0)
                    return;         // NON-SESE ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! !

                int reactCount = pinReactionsList.FirstOrDefault().Count;

                //short circuit, don't involve the DB if not enough reacts
                if (reactCount < Program.Settings.RimboardReactionsNeeded)
                {
                    return;
                }

                // Let's now try to get the Rimboard message.

                    PinInfo pinInfo = await QueryDatabaseForOriginalMessage(e.Message.Id, reactCount);
                bool validPinInfo = !pinInfo.Equals(PinInfo.Invalid);

                // Right now we want to check if the message has been posted already and react according to its reaction count.
                if (!validPinInfo && reactCount >= Program.Settings.RimboardReactionsNeeded)
                {   // We don't have stuff in the database, so it probably hasn't been pinned. Let's pin it.
                    bool file = false;

                    // DEB!
                    var deb = new DiscordEmbedBuilder();

                    deb.WithColor(DiscordColor.Gold);

                    UriBuilder avatarUri = new UriBuilder(message_noCache.Author.AvatarUrl)
                    {
                        Query = "?size=64"
                    };

                    deb.WithThumbnail(avatarUri.ToString());
                    deb.WithDescription(message_noCache.Content);
                    deb.AddField(@"Colonist", $"{message_noCache.Author.Mention}", true);
                    deb.AddField(@"Link", Generics.GetMessageUrl(message_noCache), true);

                    if (message_noCache.Attachments.Count > 0)
                    {
                        file = true;
                    }

                    // Let's send this shit already.

                    List<DiscordEmbed> embeds = new List<DiscordEmbed>
                    {
                        deb.Build()
                    };

                    if (message_noCache.Embeds.Count > 0)
                    {   // We only want to have up to 10 embeds. Keep in mind we alread have an embed, so we can only take up to 9.
                        embeds.AddRange(message_noCache.Embeds
                            .Take(message_noCache.Embeds.Count >= 9 ? 9 : message_noCache.Embeds.Count));
                    }

                    DiscordMessage rimboardMessage;

#pragma warning disable IDE0063
                    if (file)
                    {   // Send a message with a file.
                        using (WebClient webclient = new WebClient())
                        {
                            string fileName = Path.Combine(
                                path1: Program.Files.RimboardTempFileDirectory,
                                path2: Path.ChangeExtension(
                                    path: Guid.NewGuid().ToString(),
                                    extension: Path.GetExtension(message_noCache.Attachments[0].FileName)));


                            await webclient.DownloadFileTaskAsync(new Uri(message_noCache.Attachments[0].Url), fileName);

                            using (FileStream fs = new FileStream(fileName, FileMode.Open))
                            {
                                // Send the file paired with the embed!

                                rimboardMessage = await WebhookDelegator.GetWebhook(webhookId: Program.Settings.RimboardWebhookId)
                                        .SendWebhookMessage(
                                            embeds: embeds.ToArray(),
                                            fileStream: fs,
                                            fileName: fileName);
                            }

                            if (File.Exists(fileName))
                            {   // Delete it now that we're done with it.
                                File.Delete(fileName);
                            } // end if
                        } // end using
                    }
#pragma warning restore IDE0063
                    else
                    {   // Send a message with no file.

                        rimboardMessage = await WebhookDelegator.GetWebhook(webhookId: Program.Settings.RimboardWebhookId)
                                .SendWebhookMessage(embeds: embeds.ToArray());
                    }

                    pinInfo = new PinInfo(
                        pinnedMessageId: rimboardMessage.Id,
                        pinnedChannelId: rimboardMessage.Channel.Id,
                        originalMessageId: message_noCache.Id,
                        originalChannelId: message_noCache.Channel.Id,
                        originalReactCount: reactCount);

                    validPinInfo = true; // The pin info has been validated.

                    await AddPinToDatabase(pinInfo);
                }
                else if (validPinInfo && reactCount >= Program.Settings.RimboardPinReactionsNeeded)
                {   // The pin is valid and is equal to or over the threshold to be actually pinned in the Rimboard channel.
                    var rimboardChannel = e.Guild.GetChannel(pinInfo.PinnedChannelId);

                    var b = rimboardChannel.GetPinnedMessagesAsync();
                    var c = rimboardChannel.GetMessageAsync(pinInfo.PinnedMessageId);

                    await Task.WhenAll(b, c);
                    var pinnedMessages = b.Result;
                    var messageToPin = c.Result;

                    // Check if we need to get rid of the last pin.
                    if (pinnedMessages.Count() == 50)
                    {
                        await pinnedMessages.Last().UnpinAsync();
                    }

                    // Pin the message and react to it.
                    var f = messageToPin.PinAsync();
                    var g = messageToPin.CreateReactionAsync(GetPinEmoji(botClient));

                    await Task.WhenAll(f, g);
                } // end else if
            } // end if
        } // end method

        static async Task AddPinToDatabase(PinInfo pin)
        {

            //($originalmessageId, $pinnedMessageId, $originalChannelId, $originalPinChannelId)
            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_AddEntry
            };

            SqliteParameter a = new SqliteParameter("$originalMessageId", pin.OriginalMessageId.ToString())
            {
                DbType = DbType.String
            };

            SqliteParameter b = new SqliteParameter("$pinnedMessageId", pin.PinnedMessageId.ToString())
            {
                DbType = DbType.String
            };

            SqliteParameter c = new SqliteParameter("$originalChannelId", pin.OriginalChannelId.ToString())
            {
                DbType = DbType.String
            };

            SqliteParameter d = new SqliteParameter("$originalPinChannelId", pin.PinnedChannelId.ToString())
            {
                DbType = DbType.String
            };

            command.Parameters.AddRange(new SqliteParameter[] { a, b, c, d });

            await BotDatabase.Instance.ExecuteNonQuery(command);
        }

        static async Task RemovePinFromDatabase(PinInfo pinInfo)
        {
            // Let's build the command.
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_RemoveEntry
            };

            SqliteParameter a = new SqliteParameter(@"$pinnedMessageId", pinInfo.PinnedMessageId.ToString())
            {
                DbType = DbType.String
            };

            command.Parameters.Add(a);

            await BotDatabase.Instance.ExecuteNonQuery(command);
        }

        static async Task<PinInfo> QueryPinInfoFromRimboardId(ulong rimboardMessageId)
        {
            using var command = new SqliteCommand(BotDatabase.Instance.DataSource)
            {
                CommandText = QQ_RimboardSelectMessageViaId
            };

            SqliteParameter a = new SqliteParameter("$pinnedMessageId", rimboardMessageId.ToString())
            {
                DbType = DbType.String
            };

            command.Parameters.Add(a);
            
            Tuple<ulong, ulong, ulong, ulong> values =
                (Tuple<ulong, ulong, ulong, ulong>)await BotDatabase.Instance.ExecuteReaderAsync(command, processAction: ParsePinInfo);

            return new PinInfo(
                originalMessageId: values.Item1,
                pinnedMessageId:   values.Item2,
                originalChannelId: values.Item3,
                pinnedChannelId:   values.Item4,
                originalReactCount: -1);
        }

        internal static async Task BotClientMessageDeleted(DiscordClient botClient, MessageDeleteEventArgs e)
        {
            if (e.Channel.Id == Program.Settings.RimboardChannelId)
            {
                // Be warned: OriginalReactCount is always equal to -1.
                PinInfo pinInfo = await QueryPinInfoFromRimboardId(e.Message.Id);

                if (!pinInfo.Equals(PinInfo.Invalid))
                {
                    // Get the channels.
                    DiscordChannel originalChannel = e.Guild.GetChannel(pinInfo.OriginalChannelId);

                    DiscordMessage originalMessage = await originalChannel.GetMessageAsync(pinInfo.OriginalMessageId);

                    var a = originalMessage.DeleteAllReactionsAsync();
                    var b = RemovePinFromDatabase(pinInfo);

                    await Task.WhenAll(a, b);
                }
            }
        }

        static DiscordEmoji GetReactionEmoji(BaseDiscordClient cl)
            => EmojiConverter.GetEmoji(cl, Program.Settings.RimboardEmoticon);
        static DiscordEmoji GetPinEmoji(BaseDiscordClient client)
            => DiscordEmoji.FromName(client, @":pushpin:");
    }
}
