using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using QBort.Core.Database;
using QBort.Core.Managers;

namespace QBort
{
    class EventHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private SocketUserMessage Message;
        private SocketCommandContext Context;
        private IMessageChannel Channel;
        private DataTable GuildSettings;
        private ulong QueMsgId;
        private string Role;
        private readonly string SetGame = "+help";

        public EventHandler(IServiceProvider Services)
        {
            _client = Services.GetRequiredService<DiscordSocketClient>();
            _commands = Services.GetRequiredService<CommandService>();
            _services = Services;
        }

        public Task InitializeAsync()
        {
            _client.MessageReceived += OnMessageReceived;
            _client.Ready += Ready_Event;
            _client.ReactionAdded += ReactionAdded;
            _client.ReactionRemoved += ReactionRemoved;

            return Task.CompletedTask;
        }

        private async Task Ready_Event()
        {
            Console.WriteLine($"{DateTime.Now} => [READY_EVENT] : {_client.CurrentUser.Username} is ready."); // Remember, consistancy is ImPoRtAnT.
            await _client.SetGameAsync(SetGame); // Shows the prefix and "help" under Username.
            await _client.SetStatusAsync(UserStatus.Online); //Set the bot as online (enumerator)
        }

        private async Task OnMessageReceived(SocketMessage _message)
        {
            try
            {
                // Ignore non-user messages, or messages from other bots
                if (_message is not SocketUserMessage) return;
                if (_message.Source != MessageSource.User) return;

                var m = _message as SocketUserMessage;
                var Context = new SocketCommandContext(_client, m);

                int ArgPos = 0;
                string _prefix = "+";
                try
                {
                    _prefix = Guild.GetPrefix(Context.Guild.Id);
                }
                catch (Exception e)
                {
                    Log.Error(Messages.FormatError(e));
                }
                if (!(m.HasStringPrefix(_prefix, ref ArgPos))) return; //Ignore non-prefixed messages or bot @mention(?)

                var Result = await _commands.ExecuteAsync(Context, ArgPos, _services); // Third arguement set IServices. Use null if not using an IService.
                if (!Result.IsSuccess && Result.Error != CommandError.UnknownCommand)
                {
                    var command = _commands.Search(Context, ArgPos).Commands.FirstOrDefault().Command;
                    Console.WriteLine($"{DateTime.Now} at Command: {command.Name} in {command.Module.Name}] {Result.ErrorReason}");

                    var embed = new EmbedBuilder(); // Creates embed object neccessary to display things.

                    embed.WithTitle("***ERROR***");
                    embed.WithDescription(Result.ErrorReason);

                    await Context.Channel.SendMessageAsync(embed: embed.Build()); //Must be embed: embed.Build()
                }
            }
            catch (Exception e) { Log.Error(Messages.FormatError(e)); }
        }
        private async Task ReactionAdded(
            Cacheable<IUserMessage, ulong> _message,
            Cacheable<IMessageChannel, ulong> channel,
            SocketReaction reaction)
        {
            #region "Agreement Message Reaction"
            /*
                The Queue Message is used to lay out the rules and guidelines established by the server moderators.
                The reaction to the message signifies that the user has read and agreed to abide by the rules set
                forth. If the user so chooses, at any time the reaction can be removed which will render the user
                indefinitely inactive until reacted to again.
            */
            // Get all relevant information
            try
            {
                GuildSettings = new DataTable();
                GuildSettings = Guild.GetGuildSettings(Context.Guild.Id);
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                return;
            }

            Message = await _message.GetOrDownloadAsync() as SocketUserMessage;
            Context = new SocketCommandContext(_client, Message);
            Channel = await channel.GetOrDownloadAsync();
            QueMsgId = Convert.ToUInt64(GuildSettings.Rows[0]["QueMsgId"]);
            Role = GuildSettings.Rows[0]["Role"].ToString();

            // If the reaction comes from a message that isn't the set reaction message, ignore it.

            if (Message.Id == QueMsgId)
                try
                {
                    var user = reaction.User.Value as SocketGuildUser;
                    if (user.IsBot) return;

                    // Register the user if they don't exist in the DB
                    if (user.Roles.FirstOrDefault(r => r.Name == Role) != default || string.IsNullOrWhiteSpace(Role))
                    {
                        if (!Player.Exists(Context.Guild.Id, user.Id))
                            Player.AddPlayer(Context.Guild.Id, user.Id);
                        Player.EditPlayerData(Context.Guild.Id, user.Id, "Agreed", "1");
                        Player.EditPlayerData(Context.Guild.Id, user.Id, "IsActive", "1");

                        int qpos = Player.GetQueuePosition(Context.Guild.Id, user.Id);
                        if (qpos < 1)
                        {
                            int count = ActiveStats.Secretary.Where(g => g.GuildId == Context.Guild.Id)
                                .FirstOrDefault().UserFIFOCounter + 1;
                            Player.EditPlayerData(Context.Guild.Id, user.Id, "QuePos", Convert.ToString(count));
                        }
                        Console.WriteLine("COUNT COUNTER IS ::::::::::::::::::     ", Convert.ToString(ActiveStats.Secretary.Where(
                            g => g.GuildId == Context.Guild.Id
                            )));
                    }
                }
                catch (Exception e)
                {
                    Log.Error(Messages.FormatError(e));
                }
            #endregion

            //else

        }


        private async Task ReactionRemoved(
            Cacheable<IUserMessage, ulong> _message,
            Cacheable<IMessageChannel, ulong> channel,
            SocketReaction reaction)
        {
            #region "Agreement Message On Reaction Removed event"
            /*
                The Queue Message is used to lay out the rules and guidelines established by the server moderators.
                The reaction to the message signifies that the user has read and agreed to abide by the rules set
                forth. If the user so chooses, at any time the reaction can be removed which will render the user
                indefinitely inactive until reacted to again.
            */

            // Get all relevant information
            Message = await _message.GetOrDownloadAsync() as SocketUserMessage;
            Context = new SocketCommandContext(_client, Message);
            Channel = await channel.GetOrDownloadAsync();
            GuildSettings = Guild.GetGuildSettings(Context.Guild.Id);
            QueMsgId = Convert.ToUInt64(GuildSettings.Rows[0]["QueMsgId"]);
            Role = GuildSettings.Rows[0]["Role"].ToString();

            if (Message.Id == QueMsgId)
                try
                {
                    var user = reaction.User.Value as SocketGuildUser;
                    if (user.IsBot) return;
                    if (!Player.Exists(Context.Guild.Id, user.Id)) return;

                    Player.EditPlayerData(Context.Guild.Id, user.Id, "Agreed", "0");
                    Player.EditPlayerData(Context.Guild.Id, user.Id, "IsActive", "0");

                    // var roll = Context.Guild.Roles.FirstOrDefault(r => r.Name == Role);
                    // await user.RemoveRoleAsync(roll);
                }
                catch (Exception e)
                {
                    Log.Error(Messages.FormatError(e));
                }
            #endregion
        }
    }
}
