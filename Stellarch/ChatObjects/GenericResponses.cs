// GenericResponses.cs
// These are full generic, standardized responses for situations.
//
// EMIKO

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace BigSister.ChatObjects
{
    public static class GenericResponses
    {
        /// <summary>Gets a message signifying a user doesn't have the permissions to use a command.</summary>
        public static DiscordEmbed GetMessageInsufficientPermissions(string mention,
                                                                      string minRole, 
                                                                      string command)
        {
            string description = Generics.NegativeDirectResponseTemplate(mention,
                body: $"you do not have the permissions to use the command '{Formatter.Bold(command)}'. Required role: {Formatter.Bold(minRole)}...");

            return Generics.GenericEmbedTemplate(Generics.NegativeColor, description, title: @"Insufficient permission");
        }

        /// <summary>Handles invalid arguments supplied to a command.</summary>
        public static async Task HandleInvalidArguments(CommandContext ctx) 
        {
            string description = Generics.NegativeDirectResponseTemplate(ctx.Member.Mention,
                body: $"I did not understand the arguments supplied to me. Please check that you have the right arguments...\n\n");

            var embedBuilder = Generics.GenericEmbedTemplate(Generics.NegativeColor, description,
                title: $"Invalid arguments: !{ctx.Command.Name}");

            await ctx.Channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        /// <summary>Sends a generic command error.</summary>
        public static async Task SendGenericCommandError(DiscordChannel channel,
                                                         string mention,
                                                         string title,
                                                         string body)
        {
            var deb = new DiscordEmbedBuilder(
                Generics.GenericEmbedTemplate(
                    color: Generics.NegativeColor,
                    description: Generics.NegativeDirectResponseTemplate(mention, body),
                    title: title));

            await channel.SendMessageAsync(embed: deb.Build());
        }

        /// <summary>Sends a message signifying if a list was changed or not changed.</summary>
        public static async Task SendMessageChangedNotChanged(DiscordChannel channel, 
                                                          string title, 
                                                          string mention, 
                                                          string body, 
                                                          Dictionary<DiscordChannel, bool> successChanged, 
                                                          string verb, 
                                                          string invertedVerb)
        {
            var deb = new DiscordEmbedBuilder(
                Generics.GenericEmbedTemplate(
                    color: Generics.NeutralColor,
                    description: Generics.NeutralDirectResponseTemplate(mention, body),
                    title: title));

            // ----------------
            // Changed:
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendJoin(' ', // Select all the values that were changed
                from a in successChanged
                where a.Value
                select a.Key.Mention);

            string capitalizedVerb = String.Concat(char.ToUpper(verb[0]), verb[1..]);
            string capitalizedInvertedVerb = String.Concat(char.ToUpper(invertedVerb[0]), invertedVerb[1..]);

            // Check if anything was added and then make a field if anything was.
            if (stringBuilder.Length > 0)
                deb.AddField(capitalizedVerb, value: stringBuilder.ToString());

            // ----------------
            // Not changed:
            stringBuilder = new StringBuilder();
            stringBuilder.AppendJoin(' ', // Select all the values that were not changed
                from a in successChanged
                where !a.Value
                select a.Key.Mention);

            // Check if anything was added and then make a field if anything was.
            if (stringBuilder.Length > 0)
                deb.AddField(capitalizedInvertedVerb, value: stringBuilder.ToString());

            // ----------------
            // Send something

            await channel.SendMessageAsync(embed: deb.Build());
        }

        public static async Task SendMessageSettingChanged(DiscordChannel channel,
                                                           string mention, 
                                                           string title,
                                                           string valueName,
                                                           string newVal)
        {
            var deb = new DiscordEmbedBuilder(
                Generics.GenericEmbedTemplate(
                    color: Generics.PositiveColor,
                    description: Generics.PositiveDirectResponseTemplate(mention, $"I changed the setting '{valueName}' to have the value '{newVal}'."),
                    title: title));

            await channel.SendMessageAsync(embed: deb.Build());
        }
    }
}

//                                                                          ./ (//**//(((/((((/,,,,.                                                                                   
//                                                                    .    #%%##/****((//((/(***/(//(((((((((((##(//*,,.                                                              
//                                                              .  . .     (%% (#/**/***//(((*,***//////////((/(##(#%##########(((((///*,,.                                            
//                                                       .....     .     * (//*,.   .    .           ... .,.*,///(((((((((((////((/**/////(#%(,                                      
//                                                ....... ....    . .                                    , *,,., *(/*(((//*/*......,, *(((%#((/,                                 
//                                           .    .......         .     ..  .     ...                .                 .  ., ....,/ (//(/****//(/.                             
//                                       ./*..    .          ....     . ...   ..   . ........  .....   ....                .         ...,   .,****,*,*///(/                           
//                                      (% (, ........  . ...,.,.  .....  ...,.   .. . ....    .                            .,,.,.,, **//*///,                         
//                                     #%%/,            ....    .,. ...  .,,,.....  .. .,.  ..  . .       .. .                  .                  ...,..*/((*                        
//                                    (#%#*      . ..   ...  ..... ......,,.,...      ...        .     .  ..     .                                    .,//**/(*                       
//                                   / (#(,        ... .  ... ........,,...,...      ...           ......             .           . .                ..,,. .,,*/,                      
//                                 ./#/..     ., ..  .....   .,.,,.....,,.. .       .......   ..              ... .     ...                                .*//(.                     
//                                 /% (, ... .........  ...,., ...........  ..................,.  . .                        ....., **#.                    
//                                .#/,    ..... ....   ...... .,...,,......   ...  .  ... ........ .....      ...               .   ..                      ..,*/*                    
//                               .#(*.   ..,...      .....,.      .... . ..  .    ..........,..,,.......     ...                       ..                   .**/(/                    
//                             .##/*,   ........   ...,,,..  ...       .... .    ....... ...,**////**,.         .              .    . ...               ....,,*/((.                   
//                          , (##%#(/     ......,...... ........ .        .,.     ..  .*(#(((//////////((((*.                          ....              ..,*,,*///*                   
//                       ,###(((/,.*(##(((((##(*   .,,,...,,....  . .. ..,.       *((///*******,********////(*                .  . ..  ..            . ..,,.,***(//.                  
//                    .(% &%#/,*##((//*/////////*......  *.,,,,, .   .  .... .  ,#((//**********,,,,*********///,                .      .                    .. .,,*,                  
//                  ,####(,/##((/*,,*******/*,. ..,.   ,*.,,..   .        .. *(//*************,,,****,,,*,,,**//, .... .. .    ....     ..                     ,**/,                  
//                 (###*,##(((/*,,,,,******,.  . ....,,,/,*,..  ,....  ..  ..*((/*,,,,,,,,,*,,,,,,,,,,,,,,,,,,,**  *,......        .                        ..,,.//(.                 
//               *%##/,###(//*,,,,,,,,,**/*,..,. ...,,*..,.,.,.   ...  .     .,(#(/*,,,,,,,,,,,,,,,,,,,,,,,,,,,*/. ,,........      ...***.....                 ,*,//*                 
//              /%#(**##((/,,,,,,,,,*****, . .,,.,./,,,... .,.....,.   . .*(%%%#%&%(*,,,,,,,,,,,,,,,,,,,,,,,,,*/(.    ....     ...     ....,.                 .,,*/(/.                
//             *#(  *##((*,,,,,,,,,,***. .  .,, .....,/*.,. .,*, ..   .    ,((#&&%%#/*,,,,.,,,,,,,,,,,,,,,,..,*//.  ,     ,.   .                               ,///((,                
//             *(   /#(*,,,,,,,,,,,,,****.  .,....,,,,,,,,,, . .,,...    *%&%%%&&&&#(*,,,,,,,,,,,..,****/((,*///. .*,.   ...  .,,,.,..     .. .... .           .////(/                
//             *##,,#/*,,,,,,,,,,,,,,*,,.,.... .,,,,..., .,,. ..   ....  ,/%&@&&&%#(*,,.,..,,,.,.,,**(***(((#&%* ,       .,.  ....             ..,. ....        ./((((.               
//             ,##,,/,,,,,,,,,*,,,,,**,.   .,*/***,.,,..,,,,,,.,.,.,...   ,**(&&%#/,,..**/*,.,,... .,,,..,*/#*..,         ..(#,         ..         ...          ./((((.               
//             .#%,.(***,,,,,,,,,,,***,  ...,,,.,*,,,..*,,,,. *.,..  *.   .*#%%&&%(,...,/(,......,/(##(#####* ..    ...   ,&&&&/.            .,.   . ...         ./((#*               
//              (#(.*/*****,,,,,,,,,**,.......,........,..... .,,,, ./,.   .*&&&&&#* ..*(*.  .,(#%&#&&&@&%. *. .........   .#%#(/.  ..    .*,,,,...               *(((/               
//              /##(.*/***,,,**,,..,***  .,,........  .  .,... .,,.,.... .   ./&&%(*../(#* .*#%#(%#&&&%*   .,...          ....*/#%/,*/***,(,...... *,....          *(#(               
//              *##((* ,//*/#%&%#*(%&%(. .     ..    ...., ,***..,.. .,.,.     .(&&&#(*((*/((%%(&&#*    .,,,,,.,.              ,/(#%(**(###/*,,....    ,..         .(##.              
//              ,#((//*,   ..,*///*,.         .        . .... .,,.    ....,,*,,    ..,****,*,..    ..,,..,**..... .              .*%&%%%%##(///,,,. .   ...         /##/              
//              .#(((/((#%#(*.   .,/((,              .., .     ..,.   .    ,. .,.....,/(((*,..,..    .....,,,.....                 ,(%&&%##(**.....*,.              ./##              
//               %##(***/((//,  ....       ...       ......      ,,....    .     ..,,..     ....        .....    .                  ./%%%%(*,.  ... ,,,.             ,(#*             
//               *##(////(/,     .                ..     .  ......   .   .*,.      ..                   ....                          ,((***/,. .., .*,.       .     .(#(             
//                (#/,.,/((,     .                ...   ...,.   . ., .,.    .    ..,.     .       ..,....                .             .((#/((//,,. ......,   ..      /##.            
//                .##(///(*                           .,,. ...    ..            .      .      .   .... .                 .           ..,/%/*((/#(../*..,,. .          *##/            
//                  *#####(*.                     ..*/.   ... ,.  .     .  .,.          ..                                 ..       .. ..*/***,,/(  *,  ..  .....     ,(##,           
//                    ./ (((*.                    ...,,.  .....  .   .               .,, ...            ..                    .       ..  ./*****... .,,. .   .          (##/           
//                      /#/,                      ..,,  .  .  .         .                .                                                ,*/*......,. .....      ./ (#(,          
//                       %%#*.                     ..,.           .    .  ,,. ,,.                                           .            , ,**,.,.. .. . ..    .       ,(##(.         
//                       #(*.                      ....        .                 ..  .                                       .             ,**,,*,*,..*..,.            *(###/.        
//                       (//*,                                 .     ..                   .                                             .. ,/*,.., *, .,. ...    .     ,///(#*        
//                      .% (/*,.                                                         .                                                  ./#(,.. .. .*, .            *((((##.       
//                      *##/*,,,.                                                        .. . ...                                         .*(##(*,,,.  ...             .**/(##/       
//                     .#(/(((/*.                                                               ...                                      . *((#%(.,.,.. .. ..              ./(#*      
//                    * (###(**/,,,                                                                                                     ..**,*//##,..*,,.,,..            .,,/(###      
//                  .(####/(*.,..     ..                                                                                             ..,*,.//(((#* .,,,,.,..                .,/(*     
//                 /###//#/,**.       ..                                                                                             .*,/(,#(#(/(*  ,*,..,.  .     .       .,//((,    
//               *###(/(/*((/*.        .,,,.                                                                                        ..*//.(*((#((*  ,**.,,, .  .         .,***/##/    
//            .(##(((//*/(.,,,,,.      .,,***,, .                          . ....                                                     .,.,...,,/// .,*,..,..             .**////((.   
//          *%%#((//(***,               **(/*,.,,.                       ......  .    .                                       .. .    .  .**////** ,//**,,.   .          ./(((((##*   
//        *@@&#(/*/,*/..,               .*..   ,,.                ...   ...           ...                               .     .. ...   ..    .,,*,.*(//***.,,.        .   .///((##/   
//      (@@&&%#/*/(**///.                .,.  ..,,  .      .    ..,... ..                  . .      .                    .     ... .   .,,.  ....,/,//*,*/*,,.        .    .**,/(((,  
//    (@@&&&&%#(*,//*...                .........     .. ...  ......                         .  ..  ..,..                       . ...   ....   ,.*/**/((/**,,. ..  .  .    .*,,**((*  
//    (@&&&&%/*//((/**,..                 ....        .. ..  .......                             ... .....       .                        ... ...,.,,   ,***,,*,.   . .     .*/**//*  
// .% &%%#(/*#((///*,/((*,.               ..          ..,.       ..                                                 .                 ...         ..,,.      ..,,,. .....    ..,*//((. 
// .%% ((% &% &%#(///,(/*,,.               ...          ....   . . .                                                                    ...  ... .. . ,,,,.       ...,,,,,,     ...*/((, 
// .&@@@@@@@&%#/. ***,.                .....         .........                                                            .          ..   .    . . ....,.          .....    . *//**(, 
// .&@@@@&%#(*,,                    .  ....      ..   .,.....,                       . ..                                  .       .   .   ..  .   ......,,.                 .,**.*(/ 
// .&@@&%#(,                ..                   .     .,.....                                                                               . ..  ...........                 .,*//(.
// .%%#(*.                ...          .....           .,,,,,.                           .                       .         .                     .  .                            .,*/,
// .%% (/.                .            .....             ., *,.,                           .                          .         .                                              .,, **(((*
// .%% (.               .             .......           ..,, ..,.                         ..                           .                                                       ., ..., */ (
//  %%/.                             .................                     .                                                                                            , *(((
// .%%#*            .              .  ...  ..    ..    .......                          ..      .                                                                              ..,   .
//  #/.                    . ..      .    .      .    .      .                          ..                      .       .                                                      ,*##(((
// .#(,,,.         ..    ....             .... .  ...               .                   ..                        .     ..                                                      .,*/((
// .%%#/,        ....    .  .             ..... ....                                     .                                .                                                       .,/(
//  (#%/.        .           .             ....              .  .            .              .                                                                               ..    ...,
// .&&% (.                                 ....,,,     .       .....      .                                                    ..*(*/**..,/###
// .&&(.                   .               .....                                                                                                                       ......,//*/**(*
//  / .....         . .              .......                                                                                                                      ,., *,/**,*(((/.
//  (*,,.  .                                ..  .. ..                                                                                                                   ..,//*///(/(, 
//                                             . .   .    ..,,///((##*  
//                            .               .....   ...  .                                                                                                         . .., *////*   
//                            .                   .........
//                                               .       .
//         .                                                           .
//           .              ..  .  ..                                  .
//           ...          ..                                         .                                                                                                                
//             ....  .   ....   .     .                                                                                        .           .                                         
//                      .......             .                    .. .    .                                          .      .      .    ......                    . ..   . . .                         .              .. .      ....    .   .                                    
//                       . ..... ....     . ,.                       .., .........., .....     ..,. ..  .                  ...           ..  . ..     .            .                                   
//                                              .........      . .............     .., ....                .                                      
//                                         .  ... ..    . ..........   . ... ........  .   . .        .              ., ... .  ..  .                                              
//                               .             ....  . ..., ****/*,**/, ..., .... . .    ......,. . . .      ....... .      ..   .                                        
//                                           ,.     .. / (//#(((///*//.,,*,,..,,.. .  . ..... . ..  ....     .                ,...                                                      
//                                        ,/*.   .,*////***,,*,,,**,,,,,,.,,,,,...... ....... .. . ,..                       ..                                                       
//                               .      ,/,  .* *, *, .......................,.,,, ...... . ......, ... ... .                      ......,.   ..,, ....                        . ................. ., ... ..                            .                                                    
//                                                        ...............  ........    . ......     .                            .        ...............       ....                                    .                                            
//                                                                     ...     ....                                                  .  .                                             
//                                                                      ......                       .   .                    ...                                           
//                                                                       ..                    .                 .               .................                                     
//                                                                                 ...                                 ..                                                             
//                                                                                                                      .,
//                                                                                       ...,.,,,.             .         ,.
//                                                                                                             .         .
//                                                                                                     ...                                                                            



