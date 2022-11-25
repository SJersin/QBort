using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using QBort.Core.Database;
using System;

namespace QBort.Core.Commands
{
    public class HelpCommand : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _Service;
        public HelpCommand(CommandService Service)
        {
            _Service = Service;
        }

        [Command("help")]
        [Alias("command", "commands")]
        [Summary(": Displays all of the bot's commands, or displays info about a specific command.")]
        public async Task Help([Remainder] string command = "")
        {
            string prefix = Guild.GetPrefix(Context.Guild.Id); //Set the bot prefix string to a variable
            if (command == "")
            {
                var rand = new Random();
                var colors =  new byte[3];
                rand.NextBytes(colors);
                var embed = new EmbedBuilder()
                {
                    Color = new Color((int) colors[0], (int) colors[1], (int) colors[2]),   //random colors for funsies.
                    Description = $"Here are the commands. \nUse {prefix}help [command] for more information on the command."
                };

                foreach (var module in _Service.Modules)
                {
                    // Don't display my secret testing commands. Mine! Hissssss!
                    if (string.Equals(module.Name, "TestCommands", StringComparison.CurrentCultureIgnoreCase)) continue; 

                    // Don't display mod commands to non-mod +help users... IDK!
                    if (!Context.Guild.GetUser(Context.User.Id).GetPermissions(Context.Channel as IGuildChannel)
                            .Has(ChannelPermission.ManageChannels) 
                        && string.Equals(module.Name, "ModCommands", StringComparison.CurrentCultureIgnoreCase)) continue;
                    
                    string description = "";
                    foreach (var cmd in module.Commands)
                        description += cmd.Aliases.FirstOrDefault()+"\n";

                    // If there is a description, populate the values.
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        embed.AddField(x =>
                        {
                            x.Name = module.Name;
                            x.Value = description;
                            x.IsInline = false; // Renders the embed containers (or elements/fields) vertically or horizontally. True = Horizontal | False = Vertical
                        });
                    }
                }
                await ReplyAsync(embed: embed.Build());

            }
            else
            {
                var result = _Service.Search(Context, command);

                if (!result.IsSuccess) // If the command is not found
                {
                    await ReplyAsync($"Sorry, command **{command}** could not be found.");
                    return;
                }

                var embed = new EmbedBuilder()
                {
                    Color = new Color(114, 137, 218),
                    Description = $"Here are the **{command}** commands."
                };

                foreach (var match in result.Commands)
                {
                    var cmd = match.Command;
                    embed.AddField(x =>
                    {
                        x.Name = string.Join(", ", cmd.Aliases);
                        x.Value = $"Parameters {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n"
                                + $"Summary {cmd.Summary}";
                        x.IsInline = false;
                    });
                }

                await ReplyAsync(embed: embed.Build());
            }
        }
    }
}
