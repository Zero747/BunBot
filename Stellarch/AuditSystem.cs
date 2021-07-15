// AuditSystem.cs
// Contains methods for logging to a simple plaintext file on the disk.
//
// EMIKO

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using BigSister.ChatObjects;
using DSharpPlus;
using DSharpPlus.EventArgs;

namespace BigSister
{
    static class AuditSystem
    {
        static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

#pragma warning disable IDE0063
        internal static async Task Bot_CommandExecuted(CommandsNextExtension c, CommandExecutionEventArgs e)
        {
            semaphore.Wait();
            try
            {
                using (StreamWriter sw = new StreamWriter(Program.Files.LogFile, true))
                {
                    DateTimeOffset dto = DateTimeOffset.UtcNow;
                    string timeStamp = dto.ToString(Generics.DateFormat);

                    await sw.WriteAsync(
                        value: String.Format("[{0}] '{1}' CALLED BY '{2}' ID '{3}' WITH ARGUMENTS '{4}'\n",
                            timeStamp,
                            e.Command.Name,
                            $"{e.Context.Member.Username}#{e.Context.Member.Discriminator}",
                            e.Context.Member.Id,
                            e.Context.RawArgumentString));
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
#pragma warning restore IDE0063
    }
}
