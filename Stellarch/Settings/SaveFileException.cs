// SaveFileException.cs
// An exception for when issues occur with the SaveFile system.
//
// EMIKO

/*
 * |￣￣￣￣￣￣￣￣|
 * |   GOOD JOB    |
 * |  YOU  FUCKED  | 
 * |       UP      |
 * | ＿＿＿＿＿＿＿| 
 * (\__/) || 
 * (•ㅅ•) || 
 * / 　 づ
 * 
 */

using System;
using System.Runtime.Serialization;

namespace BigSister.Settings
{
    class SaveFileException : Exception
    {
        public SaveFileException() { }
        public SaveFileException(string message) : base(message) { }
        public SaveFileException(string message, Exception innerException) : base(message, innerException) { }
        protected SaveFileException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

/*                              __
                     /\    .-" /
                    /  ; .'  .' 
                   :   :/  .'   
                    \  ;-.'     
       .--""""--..__/     `.    
     .'           .'    `o  \   
    /                    `   ;  
   :                  \      :  
 .-;        -.         `.__.-'  
:  ;          \     ,   ;       
'._:           ;   :   (        
    \/  .__    ;    \   `-.     
 bun ;     "-,/_..--"`-..__)    
     '""--.._:
*/