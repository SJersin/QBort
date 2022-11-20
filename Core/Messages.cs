using System;
using Discord;

namespace QBort
{
    class Messages
    {
        public static EmbedBuilder 
        LobbyIsClosed = 
            new EmbedBuilder().WithTitle("Notice!").WithDescription("There is no open q-υωυ-e silly.").WithColor(Color.Gold),
        LobbyIsOpen = 
            new EmbedBuilder().WithTitle("Notice!").WithDescription("The q-υωυ-e is already open, silly.").WithColor(Color.Gold),
        LowActivePlayerWarning = 
            new EmbedBuilder().WithTitle("Warning!").WithDescription("The active player count is below the set group size.").WithColor(Color.DarkRed),
        WrongChannelWarning = 
            new EmbedBuilder().WithTitle("THE WORLD IS ENDING!").WithDescription("This is the wrong channel for this command.").WithColor(Color.DarkGreen);
        
        internal static string FormatError(Exception e) 
        { return $"[{DateTime.Now.ToLongDateString()} | {DateTime.Now.ToLongTimeString()} {e.InnerException}] {e.Source}:\n{e.Message}\n{e.StackTrace}"; }
    }
}