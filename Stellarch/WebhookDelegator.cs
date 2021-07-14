// WebhookDelegator.cs
// Returns a WebhookInfo for a specified webhook Id.

using System.Collections.Generic;
using System.Linq;
using BigSister.ChatObjects;

namespace BigSister
{
    public static class WebhookDelegator
    {
        /// <summary>List of all webhooks loaded from the identity file.</summary>
        static WebhookInfo[] webhooks;

        /// <summary>Initializer method.</summary>
        public static void Initialize(Dictionary<ulong, string> initDict)
        {
            webhooks = new WebhookInfo[initDict.Count];

            int i = 0;
            foreach(ulong id in initDict.Keys)
            {
                string token = initDict[id];

                webhooks[i++] = new WebhookInfo(id, token);
            }
        }

        /// <summary>Gets a WebhookInfo based on a provided id.</summary>
        /// <returns>Returns WebhookInfo.Invalid if provided id is invalid.</returns>
        /// <see cref="WebhookInfo.Invalid"/>
        public static WebhookInfo GetWebhook(ulong webhookId)
        {
            WebhookInfo webhook_returnVal;

            // Only continue if the array is not null AND has the value we're looking for.
            if (!(webhooks is null) && webhooks.Any(a => a.Id == webhookId))
            {
                webhook_returnVal = webhooks.First(a => a.Id == webhookId);
            }
            else
            {   // It's either null or doesn't have the value, so let's return the invalid value.
                webhook_returnVal = WebhookInfo.Invalid;
            }

            return webhook_returnVal;
        }
    }
}
