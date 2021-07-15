// Reminder.cs
// Contains a struct that defines a Reminder.
//
// EMIKO

using System;

namespace BigSister.Reminders
{
    /// <summary>
    /// A struct containing everything necessary for a reminder: the User, the notification message (Text), and when to notify (Time).
    /// </summary>
    public struct Reminder
    {
        /// <summary>Invalid reminder.</summary>
        public static Reminder Invalid = 
            new Reminder(
                originalMessageId: default,
                user:          default,
                text:          default,
                time:          default,
                channel:       default,
                usersToNotify: default);

        /// <summary>The original message id.</summary>
        public string OriginalMessageId { get; }
        /// <summary>User who scheduled the reminder.</summary>
        public ulong User { get; }
        /// <summary>Text to be sent when the reminder is called.</summary>
        public string Text { get; }
        /// <summary>Time of the reminder in minutes.</summary>
        public int Time { get; }
        /// <summary>Channel the reminder was originally set in.</summary>
        public ulong Channel { get; }
        /// <summary>A list of users to notify.</summary>
        public string[] UsersToNotify { get; }

        /// <param name="originalMessageId">The original message id used as a unique identifier.</param>
        /// <param name="user">User who scheduled the reminder.</param>
        /// <param name="text">Text to be sent when the reminder is called.</param>
        /// <param name="time">Time of the reminder in minutes.</param>
        /// <param name="channel">Channel the reminder was originally set in.</param>
        /// <param name="usersToNotify">A list of users to notify.</param>
        public Reminder(string originalMessageId,
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
            return obj is Reminder reminder &&
                   OriginalMessageId == reminder.OriginalMessageId;
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