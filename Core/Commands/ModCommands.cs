using System;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;
using QBort.Core.Database;


namespace QBort.Core.Commands
{
    ///<summary>These commands are mod actions. Queue bans, unbans, play count manipulation and guild registration</summary 
    public class ModCommands : ModuleBase<SocketCommandContext>
    {
        private SocketGuildUser _user = null;
        private EmbedBuilder _embed;
        private EmbedFieldBuilder _field;

        [Command("ban")]
        [Summary(": Ban a player from the queue\nCan pass a reason as a second argument.\nCan use either @mention or Discord ID\nex. `ban 123456789 reason.`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task BanPlayer(string userID, [Remainder] string reason = "")
        {

            await Context.Channel.TriggerTypingAsync();
            _embed = new EmbedBuilder().WithTitle("Ban Player");

            try  //Check if userID is an @mention or a discordID and assigns them appropriately.
            {
                if (ulong.TryParse(userID, out ulong _userID))
                    _user = Context.Guild.GetUser(_userID);
                else
                    _user = Context.Guild.GetUser(Context.Message.MentionedUsers.FirstOrDefault().Id);

                    reason = string.Concat(DateTime.Now, " ", Context.User.Username,": ", 
                        string.Equals(reason, "") ? "Reason left empty." : reason);
    
                if (_user is not null)
                {
                    if (Guild.BanPlayer(Context.Guild.Id, _user.Id, reason) > 0)
                    {
                        _embed.WithDescription(string.Concat(_user.Username," has been banned from the queue.\nReason: ",reason)) 
                              .WithColor(Color.DarkPurple);
                        await _user.SendMessageAsync(string.Concat("You have been banned from ", Context.Guild.Name, "'s queues.\n",reason));
    
                    }
                    else
                        _embed.WithDescription("Could not ban player... Uh oh...")
                              .WithColor(Color.DarkRed);
                }
                else
                    _embed.WithDescription("Player not found.")
                          .WithColor(Color.DarkRed);
    
                await Context.Channel.SendMessageAsync(embed: _embed.Build());
            }
            catch (Exception e)
            {
                Log.Error("\nSomething went wrong***********************************\n" + Messages.FormatError(e));
            }
        }

        [Command("unban")]
        [Summary(": Removes a player from the banned list. Can use either @mention or Discord ID.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task UnbanPlayer(string userID)
        {
            _embed = new EmbedBuilder().WithTitle("Unban Player");

            try  //Check if userID is an @mention or a discordID and assigns them appropriately.
            {
                if (ulong.TryParse(userID, out ulong _userID))
                    _user = Context.Guild.GetUser(_userID);
                else
                    _user = Context.Guild.GetUser(Context.Message.MentionedUsers.FirstOrDefault().Id);

                if (_user is not null)
                {
                    if (Guild.UnbanPlayer(Context.Guild.Id, _user.Id) > 0)
                    {
                        _embed.WithDescription($"The ban on {_user.Username} has been lifted.")
                              .WithColor(Color.DarkBlue);
                        await _user.SendMessageAsync($"Your ban from {Context.Guild.Name} has been lifted by {Context.User.Username}.");
                    }
                    else
                        _embed.WithDescription("Could not unban player.")
                              .WithColor(Color.DarkRed);
                }
                else
                    _embed.WithDescription("Player not found")
                          .WithColor(Color.DarkRed);

                await Context.Channel.SendMessageAsync(embed: _embed.Build());
            }
            catch (Exception e)
            {
                Log.Error("\nSomething went wrong***********************************\n" + Messages.FormatError(e));
            }
        }

        // TODO Use an embed to display gp+ and gp- results instead of a regular message. Because.
        [Command("gp+")]
        [Summary(": Adds one to the games played counter of provided user\nAccepts either @mentions or User IDs.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task IncreasePlayCount([Remainder] string userID)
        {
            _embed = new EmbedBuilder();            
            if (!Guild.GetLobbyStatus(Context.Guild.Id))
            {
                await Context.Channel.SendMessageAsync(embed: Messages.LobbyIsClosed.Build());
                return;
            }
            try
            {
                await Context.Channel.TriggerTypingAsync();
                bool group = false;
                int result;

                //Check if userID is an @mention(s) or a discordID and assigns them appropriately.
                if (ulong.TryParse(userID, out ulong _id))
                    _user = Context.Guild.GetUser(_id);
                else
                    if (Context.Message.MentionedUsers.Count > 1)
                        group = true;
                    else
                        _user = Context.Guild.GetUser(Context.Message.MentionedUsers.FirstOrDefault().Id);

                if (group)
                {
                    string results = string.Empty, 
                           succeeded = string.Empty,
                           failed = string.Empty;

                    foreach (var user in Context.Message.MentionedUsers)
                    {
                        result = Player.IncreasePlayCount(Context.Guild.Id, user.Id);

                        if (result > 0)
                            string.Concat(succeeded, user.Username, ", ");
                            // await Context.Channel.SendMessageAsync($"Game count for {user.Username} has been increased.");
                        else
                            string.Concat(failed, user.Username, ", ");
                           // await Context.Channel.SendMessageAsync($"There was an error processing this request.");
                    }
                    succeeded = succeeded.Remove(succeeded.LastIndexOf(','));
                    failed = failed.Remove(failed.LastIndexOf(','));
                }
                else
                    if (Player.IncreasePlayCount(Context.Guild.Id, _user.Id) > 0)
                        await Context.Channel.SendMessageAsync(embed: 
                            _embed.WithTitle("Increase Game Count").WithDescription($"{_user}'s game count has been increaasd to {Player.GetPlayCount(Context.Guild.Id, _user.Id)}.").Build());
                    else
                        await Context.Channel.SendMessageAsync($"There was an error processing this request.");
                
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                await Context.Channel.SendMessageAsync("Something went wrong...");
                return;
            }
        }

        [Command("gp-")]
        [Summary(": Subtracts one from the games played counter of provided user\nAccepts either @mentions or User IDs.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task DecreasePlayCount([Remainder] string userID)
        {
            _embed = new EmbedBuilder();
            if (!Guild.GetLobbyStatus(Context.Guild.Id))
            {
                await Context.Channel.SendMessageAsync(embed: Messages.LobbyIsClosed.Build());
                return;
            }
            try
            {
                await Context.Channel.TriggerTypingAsync();
                bool group = false;
                int result;
                string results = string.Empty, 
                       succeeded = string.Empty,
                       failed = string.Empty;

                //Check if userID is an @mention or a discordID and assigns them appropriately.
                if (ulong.TryParse(userID, out ulong _id))
                    _user = Context.Guild.GetUser(_id);
                else
                {
                    // TODO Add foreach to allow multiple users to be passed.
                    if (Context.Message.MentionedUsers.Count > 1)
                        group = true;
                    else
                        _user = Context.Guild.GetUser(Context.Message.MentionedUsers.FirstOrDefault().Id);
                }
                if (group)
                {
                    foreach (var user in Context.Message.MentionedUsers)
                    {
                        result = Player.DecreasePlayCount(Context.Guild.Id, user.Id);

                        if (result > 0)
                            string.Concat(succeeded, ", ");
                            // await Context.Channel.SendMessageAsync($"Game count for {user.Username} has been increased.");
                        else
                            string.Concat(failed, ", ");
                           // await Context.Channel.SendMessageAsync($"There was an error processing this request.");
                    }
                }
                else
                    if (Player.DecreasePlayCount(Context.Guild.Id, _user.Id) > 0)
                        await Context.Channel.SendMessageAsync($"Game count for {_user.Username} has been decreased.");
                    else
                        await Context.Channel.SendMessageAsync($"There was an error processing this request.");
                
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                await Context.Channel.SendMessageAsync("Something went wrong...");
                return;
            }
        }


 //       [Command("inactive")]
        [Summary(": Sets the mentioned player from the currently active queue to inactive. The player will be able to become active again by reacting to the queue message again.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetPlayerInactive([Remainder] string player)
        {
            _embed = new EmbedBuilder();
            if (!Guild.GetLobbyStatus(Context.Guild.Id))
                { await Context.Channel.SendMessageAsync(embed: Messages.LobbyIsClosed.Build()); return;}
            
            _user = Context.Guild.GetUser(Context.Message.MentionedUsers.FirstOrDefault().Id);
        }

        [Command("admin")]
        [Summary(": Sets the mentioned player from the currently active queue to inactive. The player will be able to become active again by reacting to the queue message again.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task AdminCommand([Remainder] string args)
        {

        }
        [Command("register")]
        [Summary(": Registers a server with the bot.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RegisterGuild()
        {
            _embed = new EmbedBuilder().WithTitle("Server Registration");

            string check = Database.Database.RegisterGuild(Context.Guild.Id);
            switch (check)
            {
                case "added":
                    await Context.Channel.SendMessageAsync(embed: 
                        _embed.WithDescription("Server has been successfully registered.").WithColor(Color.DarkTeal).Build());
                    break;

                case "exists":
                    await Context.Channel.SendMessageAsync(embed: 
                        _embed.WithDescription("The server id already exists.").WithColor(Color.Gold).Build());
                    break;

                default:
                    await Context.Channel.SendMessageAsync(embed: 
                        _embed.WithDescription(string.Concat("There was an error registering the server: ",check)).WithColor(Color.DarkRed).Build());
                    break;
            }
        }
    }
}
