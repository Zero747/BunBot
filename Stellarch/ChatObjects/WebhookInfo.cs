// WebhookInfo.cs
// Contains information about a webhook including its id and token.
//
// EMIKO

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace BigSister.ChatObjects
{
    public class WebhookInfo
    {
        public static WebhookInfo Invalid = new WebhookInfo();

        public ulong Id;

        readonly bool validWebhook;
        readonly DiscordWebhook webhook;
        
        public WebhookInfo() { validWebhook = false; }

        /// <summary>Generate a new webhook instance with the provided settings.</summary>
        public WebhookInfo(ulong id, string token)
        {
            webhook = Program.BotClient.GetWebhookWithTokenAsync(id, token)
                .ConfigureAwait(false).GetAwaiter().GetResult();

            Id = id;

            validWebhook = true;
        }

        #region bunny
        //        ,
        //        /|      __
        //       / |   ,-~ /
        //      Y :|  //  /
        //      | jj /( .^
        //      >-"~"-v"
        //     /       Y
        //    jo  o    |
        //   ( ~T~j
        //    >._-' _./
        //   /   "~"  |
        //  Y _,  |
        // /| ;-"~ _  l
        /// l/ ,-"~    \
        //\//\/      .- \
        // Y        /    Y    -Row
        // l       I     !
        // ]\      _\    /"\
        //(" ~----( ~   Y.  )
        #endregion bunny


        public async Task<DiscordMessage> SendWebhookMessage(string content = null, 
                                             DiscordEmbed[] embeds = null, 
                                             FileStream fileStream = null, 
                                             string fileName = null)

        {
            var dwb = new DiscordWebhookBuilder();

            if (!(embeds is null))
            {
                if (embeds.Length > 10)
                {
                    throw new ArgumentException("More than 10 embeds provided.");
                }

                dwb.AddEmbeds(embeds);
            }

            if (!(content is null))
            {
                dwb.WithContent(content);
            }

            if (!(fileStream is null) && !(fileName is null))
            {
                dwb.AddFile(Path.GetFileName(fileName), fileStream);
            }

            if (embeds is null && content is null && fileStream is null)
            {
                throw new ArgumentException("Cannot send an empty message.");
            }

            return await webhook.ExecuteAsync(dwb);
        }

        public bool Valid() => !Equals(Invalid);

        public override bool Equals(object obj)
        {
            return obj is WebhookInfo info &&
                   validWebhook == info.validWebhook &&
                   EqualityComparer<DiscordWebhook>.Default.Equals(webhook, info.webhook);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(validWebhook, webhook);
        }
    }
}
