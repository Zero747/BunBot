// Mute.cs
// Contains a struct that defines a Mute.
//


using System;

namespace BigSister.Mutes
{
    /// <summary>
    /// A struct containing everything necessary for a mute: the User, the notification message (Text), and when to notify (Time).
    /// </summary>
    public struct Mute
    {
        /// <summary>Invalid mute.</summary>
        public static Mute Invalid = 
            new Mute(
                originalMessageId: default,
                user:          default,
                text:          default,
                time:          default,
                channel:       default,
                usersToNotify: default);

        /// <summary>The original message id.</summary>
        public string OriginalMessageId { get; }
        /// <summary>User who scheduled the mute.</summary>
        public ulong User { get; }
        /// <summary>Text to be sent when the mute is called.</summary>
        public string Text { get; }
        /// <summary>Time of the mute in minutes.</summary>
        public int Time { get; }
        /// <summary>Channel the mute was originally set in.</summary>
        public ulong Channel { get; }
        /// <summary>A list of users to notify.</summary>
        public string[] UsersToNotify { get; }

        /// <param name="originalMessageId">The original message id used as a unique identifier.</param>
        /// <param name="user">User who scheduled the mute.</param>
        /// <param name="text">Text to be sent when the mute is called.</param>
        /// <param name="time">Time of the mute in minutes.</param>
        /// <param name="channel">Channel the mute was originally set in.</param>
        /// <param name="usersToNotify">A list of users to notify.</param>
        public Mute(string originalMessageId,
                        ulong user,
                        string text,
                        int time,
                        ulong channel,
                        string[] usersToNotify = null)
        {
            OriginalMessageId = originalMessageId;
            User = user;
            Text = text;
            Time = time;
            Channel = channel;

            // Check if it's null.
            if(usersToNotify is null)
            {   // It's null so set it to an empty string[].
                UsersToNotify = new string[0];
            }
            else
            {   // Not null so we can set the value.
                UsersToNotify = usersToNotify;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Mute mute &&
                   OriginalMessageId == mute.OriginalMessageId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(OriginalMessageId);
        }

        #region bunny

        //   /\\=//\-"""-.        
        //  / /6 6\ \     \        
        //   =\_Y_/=  (_  ;{}     
        //     /^//_/-/__/  jgs    
        //     "" ""  """       

        #endregion bunny

    }
}