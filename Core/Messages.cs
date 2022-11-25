using System;
using System.Collections.Generic;
using Discord;
using QBort.Enums;

namespace QBort
{
    /// <summary>
    /// The class that hosts all static system messages. Any new messages and formatters should be registurd here first.
    /// </summary>
    class Messages
    {
        // new EmbedBuilder().WithTitle().WithDescription().WithColor()         <---- CopyPasta
        internal static EmbedBuilder
        InvalidParameter =
            new EmbedBuilder().WithTitle("Oops!").WithDescription("The provided parameter doesn't seem to be valid.\nTry again?").WithColor(Color.DarkGrey),
        LobbyIsClosed =
            new EmbedBuilder().WithTitle("Notice!").WithDescription("There is no open q-υωυ-e silly.").WithColor(Color.Gold),
        LobbyIsOpen =
            new EmbedBuilder().WithTitle("Notice!").WithDescription("The q-υωυ-e is already open, silly.").WithColor(Color.Gold),
        LowActivePlayerWarning =
            new EmbedBuilder().WithTitle("Warning!").WithDescription("The active player count is below the set group size.").WithColor(Color.DarkRed),
        WrongChannelWarning =
            new EmbedBuilder().WithTitle("THE WORLD IS ENDING!").WithDescription("This is the wrong channel for this command.").WithColor(Color.DarkGreen);

        /// <summary>
        /// Formats the passed exception for readable logging.
        /// </summary>
        /// <returns>
        /// A <typeparamref name="string"/> object containing a formatted exception report.
        /// </returns>
        /// <param name="e">
        /// The <typeparamref name="Exception" /> to be formatted.
        /// </param>
        internal static string FormatError(Exception e)
        { return $"[{DateTime.Now.ToLongDateString()} | {DateTime.Now.ToLongTimeString()} {e.InnerException}] {e.Source}:\n{e.Message}\n{e.StackTrace}"; }

        /// <summary>
        /// Formats the provided group lists based upon the guilds current settings.
        /// </summary>
        /// <returns>
        /// An array of <typeparamref name="EmbedFieldBuilder" /> containing the formatted lists of players for a specific guild.
        /// </returns>
        /// <param name="format">
        /// The <paramref name="GroupListType" /> enumeration that defines how to format the group lists.
        /// </param>
        /// <param name="players">
        /// The <typeparamref name="List" /> of type <typeparamref name="string" /> contain the group of players display names.
        /// </param>
        internal static EmbedFieldBuilder[] PlayerGroupList(GroupListFormat format, List<string> players)
        {
            if (format == GroupListFormat.SingleColumn)
                try
                {
                    string column = string.Empty;
                    foreach (string player in players)
                    {
                        column = string.Concat(column, player, '\n');
                    }
                    var _field = new EmbedFieldBuilder().WithName("Next up is:")
                        .WithValue(column).WithIsInline(true);
                    EmbedFieldBuilder[] _fields = { _field };
                    return _fields;
                }
                catch
                {
                    string lazygroup = string.Join(", ", players);
                    var _field = new EmbedFieldBuilder().WithName("Next up is:")
                        .WithValue(lazygroup ?? "Uhohs").WithIsInline(true);
                    EmbedFieldBuilder[] _fields = { _field };
                    return _fields;
                }
            else if (format == GroupListFormat.DoubleColumn)
                try
                {
                    string p1 = string.Empty, p2 = string.Empty;
                    bool sw = true;
                    foreach (var player in players)
                    {
                        if (sw)
                            p1 = string.Concat(p1, player, Environment.NewLine);
                        else
                            p2 = string.Concat(p2, player, Environment.NewLine);
                        sw = !sw;
                    }

                    if (p1.Length < (players.Count / 2))
                        p1 = string.Concat(p1, "There's no one here.");
                    if (p2.Length < (players.Count / 2))
                        p2 = string.Concat(p2, "There's no too here.");

                    var _fieldone = new EmbedFieldBuilder().WithName("[1]").WithValue(p1).WithIsInline(true);
                    var _fieldtoo = new EmbedFieldBuilder().WithName("[2]").WithValue(p2).WithIsInline(true);
                    EmbedFieldBuilder[] _fields = { _fieldone, _fieldtoo };
                    return _fields;
                }
                catch
                {
                    string lazygroup = string.Join(", ", players);
                    var _field = new EmbedFieldBuilder().WithName("Next up is:")
                            .WithValue(lazygroup ?? "Uhohs").WithIsInline(true);
                    EmbedFieldBuilder[] _fields = { _field };
                    return _fields;
                }
            else
            {
                string lazygroup = string.Join(", ", players);
                var _field = new EmbedFieldBuilder().WithName("Next up is:")
                    .WithValue(lazygroup ?? "Uhohs").WithIsInline(true);
                EmbedFieldBuilder[] _fields = { _field };
                return _fields;
            }

            // return new EmbedFieldBuilder().WithName("You shouldn't see this...").WithValue("Now where's that spanner...");
        }
    }
}
