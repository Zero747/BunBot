// Permissions.cs
//
// Contains structures for handling permissions:
//  enum Role - contains the roles we want to recognize
//  class UserPermissions - contains information about a specific user's permissions based on what is recognized by the bot
//  static class Permissions - contains methods for checking if a user has desired permissions.
//


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using BigSister.ChatObjects;
using BigSister.Commands;

namespace BigSister
{
    [Flags]
    public enum Role
    {
        None = 0,
        Colonist = 1,
        Troubleshooter = 2,
        CS = 4,
        Mod = 8,
        SeniorMod = 16,
        BotManager = 32,
        Admin = 64
    }

    public static class Permissions
    {
        public static async Task<bool> HandlePermissionsCheck(CommandContext ctx)
        {
            MinimumRole minRole;

            // Let's make sure this isn't null.
            if (!(ctx.Command.CustomAttributes is null))
            {
                var linq = ctx.Command.CustomAttributes.OfType<MinimumRole>();

                // Let's make sure there's actually the attribute we want.
                if (linq.Count() > 0)
                {   // The attribute we want is here. 
                    minRole = linq.First();
                }
                else
                {   // It's not there for some reason, so let's throw an exception.
                    throw new Exception("Unable to load MinimumRole attribute.");
                }
            }
            else
            {   // It's null, so let's throw an exception.
                throw new Exception("Unable to load MinimumRole attribute.");
            }

            return await HandlePermissionsCheck(
                member: ctx.Member, 
                chan: ctx.Channel, 
                minRole: minRole,
                shouldRespondToRejection: true,
                commandName: ctx.Command.Name);
        }

        /// <summary>Handle checking permissions for a command.</summary>
        /// <param name="member">The member whose roles need to be checked</param>
        /// <param name="chan">The channel the check takes place in.</param>
        /// <param name="minRole">The minimum role required to use a command.</param>
        /// <param name="shouldRespondToRejection">If it should send a rejection message when minimum role isn't met. Defaults to true.</param>
        /// <param name="commandName">The command name. Defaults to empty.</param>
        /// <returns>True if the user has the permissions to run a command.</returns>
        public static async Task<bool> HandlePermissionsCheck(DiscordMember member, 
                                                              DiscordChannel chan, 
                                                              MinimumRole minRole, 
                                                              bool shouldRespondToRejection = true, 
                                                              string commandName = @"")
        {
            bool pass_returnVal;

            var userPerms = new UserPermissions(member.Roles);

            // Let's make sure they're not muted or have no role first of all.
            if (userPerms.Muted || userPerms.UserRoles == Role.None)
            {   // They are muted or have no roles.
                pass_returnVal = false;
            }
            else
            {   // They are not muted and do have roles, so let's continue.
                bool userHasPerms;

                userHasPerms = userPerms.IsRoleOrHigher(minRole.MinRole);

                pass_returnVal = userHasPerms;

                if (!pass_returnVal && shouldRespondToRejection)
                {   // If they didn't pass but aren't muted and they do have roles, and we want to respond let's handle a response.
                    await HandleRejection(chan, member.Mention, minRole.MinRole, commandName);
                }
            }

            return pass_returnVal;
        }

        /// <summary>Handle a rejection that occurs due to insufficient permissions.</summary>
        public static async Task HandleRejection(DiscordChannel chan, string mention, Role minRole, string commandName)
        {
            await chan.SendMessageAsync(
                embed: GenericResponses.GetMessageInsufficientPermissions
                (
                    mention: mention,
                    minRole: UserPermissions.GetRoleString(minRole),
                    command: commandName
                ));
        }
    }

    public class UserPermissions
    {
        public readonly Role UserRoles;
        public readonly bool Muted;

        public UserPermissions(IEnumerable<DiscordRole> roles)
        {
            UserRoles = GetRoles(roles, out Muted);
        }

        /// <summary>Checks if the user is the desired role or higher.</summary>
        /// <param name="desiredRole">Desired role.</param>
        public bool IsRoleOrHigher(Role desiredRole)
            => UserRoles >= desiredRole;

        /// <summary>Get all the roles the user has.</summary>
        /// <returns>A [Flag] Role containing the roles of the user.</returns>
        public static Role GetRoles(IEnumerable<DiscordRole> roles, out bool muted)
        {
            Role role_returnVal = Role.None;
            muted = false;

            foreach (DiscordRole role in roles)
            {
                switch (role.Id)
                {
                    // ----------------------------------------------
                    case 793897526377840660: // Rimworld Colonist  
                    case 841237358527250433: // Dev Server Colonist

                        role_returnVal |= Role.Colonist;
                        break;
                    // ----------------------------------------------
                    case 793897521755062324: // Rimworld Troubleshooter
                    case 750985581701496872: // Dev Server Troubleshooter

                        role_returnVal |= Role.Troubleshooter;
                        break;
                    // ----------------------------------------------
                    case 326891962697383936: // Rimworld Community Support
                    case 795637726371512330: // Rimworld NEW Community Support
                    case 673765694982193173: // Dev Server Community Support

                        role_returnVal |= Role.CS;
                        break;
                    // ----------------------------------------------
                    case 214527027112312834: // Rimworld Moderator
                    case 795636990221221908: // Rimworld NEW MODERATOR
                    case 690206811961294934: // Rimworld . Role 
                    case 564291273729769492: // Dev Server Moderator

                        role_returnVal |= Role.Mod;
                        break;
                    // ----------------------------------------------
                    case 521006886451937310: // Rimworld Senior Moderator
                    case 673765727605358614: // Dev Server Senior Moderator

                        role_returnVal |= Role.SeniorMod;
                        break;
                    // ----------------------------------------------
                    case 503752769757511690: // Rimworld Bot Manager
                    case 673765760656605194: // Dev Server Bot Manager

                        role_returnVal |= Role.BotManager;
                        break;
                    // ----------------------------------------------
                    case 214524811433607168: // Rimworld Admin 
                    case 564292110182776843: // Dev Server Admin

                        role_returnVal |= Role.Admin;
                        break;
                    // ----------------------------------------------
                    case 793897525509095477: // Rimworld Muted
                    case 778929358491353138: // Dev Server Muted

                        muted = true;
                        break;
                }
            }

            return role_returnVal;
        }

        /// <summary>Turn a Role into a user-friendly string.</summary>
        public static string GetRoleString(Role role)
        {
            string roleString_returnVal = role switch
            {
                Role.Colonist       => @"Colonist",
                Role.Troubleshooter => @"Troubleshooter",
                Role.CS             => @"Community Support",
                Role.Mod            => @"Moderator",
                Role.SeniorMod      => @"Senior Moderator",
                Role.BotManager     => @"Bot Manager",
                Role.Admin          => @"Admin",
                                  _ => String.Empty,
            };
            return roleString_returnVal;
        }
    }
}
