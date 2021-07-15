// MinimumRole.cs
// This is an attributes for commands to use to signify what role is required, minimum, in order to use the command.
//
// EMIKO

using System;

namespace BigSister.Commands
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MinimumRole : Attribute
    {
        public Role MinRole;

   // ( Y )
   //(  . .)
   //o(") (")

        public MinimumRole(Role minimumRole)
            => MinRole = minimumRole;
    }
}
