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
                guild:         default);

        /// <summary>The original message id.</summary>
        public string OriginalMessageId { get; }
        /// <summary>Muted User</summary>
        public ulong User { get; }
        /// <summary>Text to be sent when the mute is called.</summary>
        public string Text { get; }
        /// <summary>Time of the mute in minutes.</summary>
        public int Time { get; }
        /// <summary>User guild (server)</summary>
        public ulong Guild { get; }


        /// <param name="originalMessageId">The original message id used as a unique identifier.</param>
        /// <param name="user">Muted User.</param>
        /// <param name="text">Text to be sent when the mute is called.</param>
        /// <param name="time">Time of the mute in minutes.</param>
        /// <param name="guild">User guild (server)</param>

        public Mute(string originalMessageId,
                        ulong user,
                        string text,
                        int time,
                        ulong guild)
        {
            OriginalMessageId = originalMessageId;
            User = user;
            Text = text;
            Time = time;
            Guild = guild;
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