using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using QBort.Core.Database;
using QBort.Enums;


/*
    The Rebirth into QBort!
 */

namespace QBort.Core.Commands
{
    public class QueueCommands : ModuleBase<SocketCommandContext>
    {
        private ulong GuildId;
        private EmbedBuilder _embed;
        private EmbedFieldBuilder _field;
        private SocketGuildUser _leader;

        /// See about creating temporary channels for use instead of having to create bot specific rooms
        /// can be expanded to include voice channels. Can make this very vancy.
        [Command("open")]
        [Alias("Create, create")]
        [Summary(": Create a new queue for users to join.\nYou must pass a user role @mention.\nEx: `open @customs`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task OpenQueue(IRole role, [Remainder] string message = "")
        {
            GuildId = Context.Guild.Id;
            _ = Context.Channel.TriggerTypingAsync();
            try
            {
                if (Context.Channel.Id != Guild.GetQueueMessageRoom(Context.Guild.Id))
                { await Context.Channel.SendMessageAsync(embed: Messages.WrongChannelWarning.Build()); return; }
                if (Guild.GetLobbyStatus(GuildId))
                { await Context.Channel.SendMessageAsync(embed: Messages.LobbyIsOpen.Build()); return; }
                try
                {
                    Guild.ChangeLobbyStatus(GuildId);
                }
                catch (Exception e)
                {
                    Log.Error(Messages.FormatError(e));
                    await Context.Channel.SendMessageAsync("Unable to open server queue.");
                    return;
                }

                // Create message with reaction for queue
                _embed = new EmbedBuilder()
                      .WithColor(Color.DarkGreen)
                      .WithTitle($"The queue is now open! React to this message to register for the queue.")
                      .WithTimestamp(DateTime.Now);

                _field = new EmbedFieldBuilder()
                      .WithName("Click or Tap on the reaction to join queue.")
                      .WithValue("Remember to be respectful towards other people and follow the rules that have been established by the community!");

                _embed.AddField(_field);
                string gem; // Guild EMote
                // Start checks

                var SendEmbedTask =
                    Context.Channel.SendMessageAsync(embed: _embed.Build());

                using (var dt = Guild.GetGuildSettings(GuildId))
                    gem = dt.Rows[0]["Reaction"].ToString();


                var ReactionMessage = await SendEmbedTask;    // Sends the embed for people to react to and stores the message.
                try
                {
                    Guild.SetQueueMessageId(GuildId, Convert.ToString(ReactionMessage.Id));

                    if (Emote.TryParse(gem, out Emote emote))
                        await ReactionMessage.AddReactionAsync(emote);
                    else if (Emoji.TryParse(gem, out Emoji emoji))
                        await ReactionMessage.AddReactionAsync(emoji);
                    else
                        await ReactionMessage.AddReactionAsync(new Emoji("👍"));
                }
                catch (Exception e)
                {
                    Log.Error(Messages.FormatError(e));
                    await Context.Channel.SendMessageAsync("There was an issue parsing the emote.");
                    await ReactionMessage.AddReactionAsync(new Emoji("👍"));
                }
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                await Context.Channel.SendMessageAsync("Queue did not open properly.");
            }
        }

        [Command("close")]
        [Summary(": Close off and clear the queue.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task CloseQueue()
        {
            GuildId = Context.Guild.Id;

            try
            {
                // Lobby is closed
                if (!Guild.GetLobbyStatus(GuildId)) { await Context.Channel.SendMessageAsync(embed: Messages.LobbyIsClosed.Build()); return; }
                // Wrong channel
                if (Context.Channel.Id != Guild.GetQueueMessageRoom(Context.Guild.Id)) { await Context.Channel.SendMessageAsync(embed: Messages.WrongChannelWarning.Build()); return; }

                if (Guild.ChangeLobbyStatus(GuildId) == 1)
                {
                    Task<IMessage>[] _deletemetasks = {
                            Context.Channel.GetMessageAsync(Guild.GetPullMessageId(GuildId)), //DeletePullMessageTask,
                            Context.Channel.GetMessageAsync(Guild.GetQueueMessageId(GuildId)) }; //DeleteQueueMessageTask };
                    List<IMessage> _deletethese = new();
                    List<string> problems = new();

                    _embed = new EmbedBuilder().WithTitle("The customs queue has been closed!")
                        .WithColor(Color.DarkRed)
                        .WithDescription("Thank you everyone who joined in today's session!!").WithCurrentTimestamp();
                    var SendEmbedTask =
                        Context.Channel.SendMessageAsync(embed: _embed.Build());

                    using (var player = Guild.GetAllPlayersList(GuildId))
                        foreach (DataRow i in player.Rows)
                            if (Player.ResetPlayStats(GuildId, Convert.ToUInt64(i["PlayerId"])) != 1)
                                problems.Add(string.Concat("Player Id: ", i["PlayerId"].ToString(), "\nCould not reset player stats."));

                    Guild.ClearPlayCounts(GuildId);
                    if (problems.Count > 0)
                    {
                        string ohno = $"There were {problems.Count} problems logged while closing {Context.Guild.Name}'s queue. They are:";
                        foreach (var problem in problems)
                            ohno = string.Concat(ohno, $"\n\t{problem}");
                        Log.Warning(ohno);
                    }
                    Guild.SetPullMessageId(GuildId, 0);
                    try
                    {
                        foreach (var item in _deletemetasks)
                            _deletethese.Add(await item);

                        foreach (var msg in _deletethese)
                            await msg.DeleteAsync();
                        await SendEmbedTask;
                    }
                    catch (Exception e)
                    {
                        Log.Error(Messages.FormatError(e));
                    }
                }
                else
                    await Context.Channel.SendMessageAsync(embed: Messages.LobbyIsClosed.Build());
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
            }
        }

        [Command("list")]
        [Summary(": Provides the list of everyone that is currently active in an open queue.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task ShowQueueList()
        {
            GuildId = Context.Guild.Id;

            //TODO Make the list embed after guild registration and agreement?
            _embed = new EmbedBuilder();
            if (!Guild.GetLobbyStatus(GuildId))
            { await Context.Channel.SendMessageAsync(embed: Messages.LobbyIsClosed.Build()); return; }
            string NameList = string.Empty;

            using (var PlayersToList = Guild.GetActivePlayersList(GuildId))
            {
                int activePlayers = PlayersToList.Rows.Count;
                foreach (DataRow p in PlayersToList.Rows)
                    NameList += Context.Guild.GetUser(Convert.ToUInt64(p["PlayerId"])).DisplayName + ": " + Convert.ToString(p["PlayCount"]) + " | ";
            }
            NameList = NameList.Remove(NameList.LastIndexOf('|') - 1);

            _field = new EmbedFieldBuilder().WithName("Active users: ").WithValue(NameList);
            try
            {
                int activePlayers = Guild.GetActivePlayerCount(GuildId);
                if (activePlayers > 0)
                    _embed.WithTitle($"There are {activePlayers} players in the list")
                       .WithCurrentTimestamp().AddField(_field);
                else
                    _embed.WithTitle("The q-υωυ-e is Empty.").WithDescription("This makes QBort sad... :(");

                await Context.Channel.SendMessageAsync(embed: _embed.Build());
            }
            catch (Exception e) // Something bad has happened.
            {
                Log.Error(Messages.FormatError(e));
            }
        }

        [Command("new")]
        [Summary("Gets and displays [x] number of players who have the lowest\n " +
            "number of games played for the next lobbies.\nIf no number is provided, " +
            "the default will be used.\nA second argument can be passed for the password" +
            " when passing a number.\nEx: `new [password]` or `new [x] [password]`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task NewGroup(string arg = null, [Remainder] string password = null)
        {
            GuildId = Context.Guild.Id;
            List<Player> PlayerList = new();
            int GroupSize = 0, GuildMaxGroupSize = 0;

            var typing =
                Context.Channel.TriggerTypingAsync();

            #region Start checks

            if (!Guild.GetLobbyStatus(GuildId))
            { await Context.Channel.SendMessageAsync(embed: Messages.LobbyIsClosed.Build()); return; }
            if (Context.Channel.Id != Guild.GetPullMessageRoom(GuildId))
            { await Context.Channel.SendMessageAsync(embed: Messages.WrongChannelWarning.Build()); return; }

            // Check for first argument: Is it a different group size number or just a password?

            using (var _gs = Guild.GetGuildSettings(GuildId))
            {
                try
                {
                    GroupSize = Convert.ToInt16(_gs.Rows[0]["GroupSize"]);
                    GuildMaxGroupSize = Convert.ToInt16(_gs.Rows[0]["MaxGroupSize"]);
                }
                catch (Exception e)
                {
                    Log.Error(Messages.FormatError(e));
                }
            }
            // Parse arguements for custom size and/or password.
            try
            {
                if (Int16.TryParse(arg, out short CustomGroupSize))
                    if (CustomGroupSize < GuildMaxGroupSize)
                        GroupSize = CustomGroupSize;
                    else
                        GroupSize = GuildMaxGroupSize;
                else
                {
                    password = arg;
                }
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
            }
            #endregion

            using (var ActiveList = Guild.GetActivePlayersList(GuildId))
                foreach (DataRow player in ActiveList.Rows)
                    PlayerList.Add(new Player(Convert.ToUInt64(player["PlayerId"]), Convert.ToInt16(player["PlayCount"])));

            if (GroupSize == 0)
            {
                await Context.Channel.SendMessageAsync("There are no players in the active list.");
                return;
            }
            // Check active player count against registered group size.
            // Will overwrite the group size if the active playerbase size is the lower number.
            if (PlayerList.Count < GroupSize)
            {
                GroupSize = PlayerList.Count;
                await Context.Channel.SendMessageAsync(embed: Messages.LowActivePlayerWarning.Build());
            }

            var random = new Random();
            int index = 0;
            string mentions = string.Empty;
            List<string> list = new();
            List<ulong> recall = new();
            _leader = Context.User as SocketGuildUser;

            _embed = new EmbedBuilder().WithTitle($"{_leader.Username}'s Next Lobby Group:")
                .WithDescription($"Here are the next {GroupSize} players for {_leader.Username}'s lobby.\nThe password is: ` {password} `\n*This is an unbalanced team list, and not indicative of your party.\nOnly join this lobby if your name is in this list!*")
                .WithColor(Color.DarkGreen);

            ulong oldpulledmessage = Guild.GetPullMessageId(GuildId);
            if (oldpulledmessage != 0)
                try  // Start Deleting the old stuff
                {
                    var DeleteOldPullMessageTask =
                        Context.Channel.GetMessageAsync(oldpulledmessage).Result.DeleteAsync();
                }
                catch (Exception e)
                {
                    Log.Error(Messages.FormatError(e));
                }
                finally
                {

                }

            List<Task<IUserMessage>> DMs = new(); // using var or new() is acceptable here, I just didn't want to. EDIT: The formatted wanted to.
            try
            {
                ulong x;
                while (GroupSize > 0)
                {
                    // If the count of the list of players with the lowest play count, just throw them all in.
                    if (PlayerList.FindAll(p => p.PlayCount == PlayerList[0].PlayCount).Count <= GroupSize)
                    {
                        foreach (var player in PlayerList.FindAll(p => p.PlayCount == PlayerList[0].PlayCount))
                        {
                            GroupSize--;
                            recall.Add(player.PlayerId);
                        }
                        foreach (var user in recall)
                            PlayerList.Remove(PlayerList.Find(p => p.PlayerId == user));
                    }
                    else
                    {
                        index = random.Next(0, PlayerList.FindAll(p => p.PlayCount == PlayerList[0].PlayCount).Count);
                        x = PlayerList[index].PlayerId;

                        if (!recall.Contains(x))
                            recall.Add(x);
                        PlayerList.Remove(PlayerList.Find(p => p.PlayerId == x));
                        GroupSize--;
                    }
                }

                foreach (var r in recall)
                {
                    list.Add(Context.Guild.GetUser(r).DisplayName);
                    try
                    {
                        mentions += $"{Context.Guild.GetUser(r).Mention} "; // @mentions the players
                        DMs.Add(Context.Guild.GetUser(r)
                            .SendMessageAsync($"You are in `{_leader.Username}'s` lobby. The password is: ` {password} ` ."));
                    }
                    catch (Exception e)
                    {
                        Log.Error(Context.Guild.GetUser(r).DisplayName + " - foreach recall catch.\n" + Messages.FormatError(e)); continue;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                // continue;
            }

            foreach (ulong user in recall)
                Player.IncreasePlayCount(GuildId, user);

            try
            {
                GroupListFormat format;
                var listform = Guild.GetPullMessageFormat(Context.Guild.Id);
                format = (GroupListFormat)Enum.Parse(typeof(GroupListFormat), listform ?? "Plain");
                // if (string.Equals(listform, GroupListFormat.SingleColumn))
                //     format = GroupListFormat.SingleColumn;
                // else if (string.Equals(listform, GroupListFormat.DoubleColumn))
                //     format = GroupListFormat.DoubleColumn;
                // else
                //     format = GroupListFormat.Plain;

                foreach (var _fields in Messages.PlayerGroupList(format, list))
                    _embed.AddField(_fields);

                var SendEmbedTask = Context.Channel.SendMessageAsync(mentions, embed: _embed.Build());

                // Start calling all of our awaited tasks.
                foreach (var dm in DMs)
                    await dm;
                var Messagae = await SendEmbedTask;

                Guild.SetRecallGroup(GuildId, recall.ToArray());
                Guild.SetPullMessageId(GuildId, Messagae.Id); // Use this for storing called games' message id to delete later. Also, I'm aware of the typo. It's been with this project since creation. It has tenure.
                await Context.Message.DeleteAsync();
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
            }
        }

        [Command("recall")]
        [Summary(": Re-pings the last pulled group with a provided message.\nEx: `recall This is an after thought.`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RecallList([Remainder] string msg = "")
        {
            GuildId = Context.Guild.Id;
            if (!Guild.GetLobbyStatus(GuildId))
            { await Context.Channel.SendMessageAsync(embed: Messages.LobbyIsClosed.Build()); return; }

            string[] group = Guild.RecallGroup(GuildId).Split(',');
            string mentions = "";
            //TODO Finish pulling, seperating and assigning ulong Ids
            if (group.Length > 0)
            {
                foreach (string player in group)
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(player))
                            mentions += $"{Context.Guild.GetUser(Convert.ToUInt64(player)).Mention} "; // @mentions the players
                    }
                    catch (Exception e)
                    {
                        Log.Error(Messages.FormatError(e));
                    }
                if (msg != "")
                    mentions += $"\n\n{msg}";
            }

            try
            {
                await Context.Channel.SendMessageAsync(mentions);
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
            }
        }

        [Command("replace")]
        [Summary(": Calls a new player to replace one that is unable to participate after they have been called. Used by passing @mentions\nEx: `replace @Johnny @May`\n"
        + "will remove Johnny and May from the group and replace them each with another randomly pulled player.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task ReplacePlayer([Remainder] string mentions = "")
        {
            try
            {
                List<ulong> UsersToReplace = new(),
                            RecallList = new();

                List<Player> ReplacementPlayersList = new(),
                             ReplacementPlayers = new();
                int ReplacementCount = Context.Message.MentionedUsers.Count;
                GuildId = Context.Guild.Id;

                if (ReplacementCount < 1) // No mentions passed; no one to replace.
                    return;
                else
                    foreach (var _utr in Context.Message.MentionedUsers)
                        UsersToReplace.Add(_utr.Id); // get users to replace

                //TODO Add logic for setting replaced players inactive?
                // get recall list
                foreach (string player in Guild.RecallGroup(Context.Guild.Id).Split(','))
                    RecallList.Add(ulong.Parse(player));

                // get active players list
                using (var ActivePlayerListTable = Guild.GetActivePlayersList(Context.Guild.Id))
                {
                    if (ActivePlayerListTable.Rows.Count < 1)
                    {
                        await Context.Channel.SendMessageAsync("No suitable replacement players found."); return;
                    }

                    // check count of active players against to_replace count (mentioned users)
                    // More users than we can actively replace.
                    if (ReplacementCount > ActivePlayerListTable.Rows.Count)
                    {
                        await Context.Channel.SendMessageAsync("There are not enough players to replace everyone."); return;
                    }
                    else if (ReplacementCount <= ActivePlayerListTable.Rows.Count)
                    {
                        foreach (DataRow player in ActivePlayerListTable.Rows)
                            ReplacementPlayersList.Add(new Player(ulong.Parse(player["PlayerId"].ToString()), int.Parse(player["PlayCount"].ToString())));

                        // Remove the users being replaced from the list
                        foreach (var utr in UsersToReplace)
                            ReplacementPlayersList.Remove(ReplacementPlayersList.Find(x => x.PlayerId == utr));

                        // Remove users already in the game
                        foreach (var ingame in RecallList)
                            ReplacementPlayersList.Remove(ReplacementPlayersList.Find(x => x.PlayerId == ingame));
                        // We have enough users to fill, so lets fill.
                        if (ReplacementCount < ReplacementPlayersList.Count)
                        {
                            var rand = new Random();
                            foreach (var user in UsersToReplace)
                            {
                                int i = rand.Next(ReplacementPlayersList.Count);
                                ReplacementPlayers.Add(ReplacementPlayersList[i]);
                                ReplacementPlayersList.RemoveAt(i);
                            }
                        }
                        else // We have just enough users, so simplify the logic.
                            ReplacementPlayers = ReplacementPlayersList;
                    }
                }

                string MentionString = string.Empty,
                       BeingReplaced = string.Empty,
                       IsReplacement = string.Empty;
                if (ReplacementCount == 1) // Only one user to replace
                {
                    var _user = Context.Guild.GetUser(UsersToReplace[0]);
                    MentionString = string.Concat(_user.Mention, " ");
                    BeingReplaced = string.Concat(_user.DisplayName, "\n");
                    RecallList.Remove(_user.Id);
                    Player.DecreasePlayCount(GuildId, _user.Id);
                }
                else // More than one user to replace
                    foreach (var utr in UsersToReplace)
                    {
                        var _user = Context.Guild.GetUser(utr);
                        MentionString = string.Concat(MentionString, _user.Mention, " ");
                        BeingReplaced = string.Concat(BeingReplaced, _user.DisplayName, "\n");
                        RecallList.Remove(_user.Id);
                        Player.DecreasePlayCount(GuildId, _user.Id);
                    }
                MentionString = string.Concat(MentionString, "is being replaced with ");

                try
                {
                    foreach (var player in ReplacementPlayers)
                    {
                        var _user = Context.Guild.GetUser(player.PlayerId);
                        MentionString = string.Concat(MentionString, _user.Mention, " ");
                        IsReplacement = string.Concat(IsReplacement, _user.DisplayName, "\n");
                        RecallList.Add(_user.Id);
                        Player.IncreasePlayCount(GuildId, _user.Id);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(Messages.FormatError(e));
                }
                try
                {
                    _embed = new EmbedBuilder()
                          .WithTitle($"**{Context.User.Username}** Roster Rotation!")
                          .WithDescription("Pay attention to the changes listed!!");

                    _field = new EmbedFieldBuilder().WithName("Players sitting out:")
                          .WithValue(string.IsNullOrEmpty(BeingReplaced) ? "No one it seems..." : BeingReplaced);
                    _embed.AddField(_field);

                    _field = new EmbedFieldBuilder().WithName("Players now in the play group:")
                          .WithValue(string.IsNullOrEmpty(IsReplacement) ? "No one it seems..." : IsReplacement);
                    _embed.AddField(_field);

                    Guild.SetRecallGroup(GuildId, RecallList.ToArray());
                    await Context.Channel.SendMessageAsync(MentionString, embed: _embed.Build());
                }
                catch (Exception e)
                {
                    Log.Error(Messages.FormatError(e));
                }
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
            }
        }

        [Command("swap")]
        [Summary(": Replaces a player in the current group with a specifc player.\nExample: `swap @jersin @sum1btr`\nwill remove jersin and replace them with sum1btr.")]
        public async Task SwapPlayer([Remainder] string mentionedUsers = "")
        {
            if (mentionedUsers == "")
            { await Context.Channel.SendMessageAsync("Done."); return; }

            var Players = Context.Message.MentionedUsers;
            if (Context.Message.MentionedUsers.Count < 2) // No mentions passed; no one to replace.
            { await Context.Channel.SendMessageAsync("Swap with whom???????"); return; }
            else
            {
                List<SocketGuildUser> _users = new();

                foreach (var player in Players)
                    _users.Add((SocketGuildUser)player);

                if (Players.Count != 2)
                { await Context.Channel.SendMessageAsync("What kind of swap is this supposed to be? Two players only, mate! No more, no less!"); return; }

                var RecallGroup = Array.ConvertAll(Guild.RecallGroup(Context.Guild.Id).Split(','), x => ulong.Parse(x)).ToList();
                string filling_in, sitting_out;

                if (RecallGroup.Contains(_users.First().Id) && RecallGroup.Contains(_users.Last().Id))
                {
                    await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                        .WithTitle("Swap command..?").WithDescription("You're still in, just trade seats... or something!").Build());
                    return;
                }
                else if (RecallGroup.Contains(_users.First().Id))
                {
                    sitting_out = _users.First().DisplayName;
                    RecallGroup.Remove(_users.First().Id);
                    Player.DecreasePlayCount(Context.Guild.Id, _users.First().Id);
                    filling_in = _users.Last().DisplayName;
                    RecallGroup.Add(_users.Last().Id);
                    Player.IncreasePlayCount(Context.Guild.Id, _users.Last().Id);
                }
                else if (RecallGroup.Contains(_users.Last().Id))
                {
                    sitting_out = _users.Last().DisplayName;
                    RecallGroup.Remove(_users.Last().Id);
                    Player.DecreasePlayCount(Context.Guild.Id, _users.Last().Id);
                    filling_in = _users.First().DisplayName;
                    RecallGroup.Add(_users.First().Id);
                    Player.IncreasePlayCount(Context.Guild.Id, _users.First().Id);
                }
                else
                {
                    await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                        .WithTitle("Swap command..?").WithDescription("You're *not* in, but like... trade seats... or something?")
                        .WithFooter(string.Concat("Blame: ", Context.User.Username), ", ok?").Build());
                    return;
                }
                try
                {
                    _embed = new EmbedBuilder()
                          .WithTitle($"**{Context.User.Username}** Roster Rotation!")
                          .WithDescription("Pay attention to the changes listed!!");
                    _field = new EmbedFieldBuilder().WithName("Players sitting out:")
                          .WithValue(string.IsNullOrEmpty(sitting_out) ? "No one it seems..." : sitting_out);
                    _embed.AddField(_field);
                    _field = new EmbedFieldBuilder().WithName("Players now in the play group:")
                          .WithValue(string.IsNullOrEmpty(filling_in) ? "No one it seems..." : filling_in);
                    _embed.AddField(_field);

                    var SendEmbedTask
                        = Context.Channel.SendMessageAsync(embed: _embed.Build());

                    if (Guild.SetRecallGroup(GuildId, RecallGroup.ToArray()) < 1)
                        Log.Error(string.Concat("There was an issue updating the recall list in the swap command for guild:\n> ", Context.Guild.Name, "\n[U:", Context.Guild.Id, "]"));
                    await SendEmbedTask;
                }
                catch (Exception e)
                {
                    Log.Error(Messages.FormatError(e));
                    throw new TaskCanceledException();
                }
            }
        }

        [Command("status")]
        [Summary(": Returns the status of the guild's queue.")]
        public async Task QueueStatus()
        {
            await Context.Channel.SendMessageAsync(string.Concat("The queue is ", Guild.GetLobbyStatus(Context.Guild.Id) ? "open." : "closed."));
        }

        /* Map Commands

            [Command("mapvote")]
            [Summary("Randomly chooses x number of maps from a pool and puts them to a vote.")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task MapVote()
            {
                // Adding Map Vote here. Don't ask.


                var one = new Emoji("1️⃣"),
                    two = new Emoji("2️⃣"),
                    three = new Emoji("3️⃣");

                List<string> maps = new List<string>();

                // Create and display embed for maps selected for next game.
                var voteEmbed = new EmbedBuilder()
                    .WithTitle("Next Game Map Vote")
                    .WithDescription("Vote here!")
                    .WithColor(Color.Blue);

                var random = new Random();

                HashSet<int> numbers = new HashSet<int>();
                while (numbers.Count <= Config.bot.NumberOfVotes)
                {
                    int index = random.Next(0, MapList.List.Length);
                    numbers.Add(index);
                }

                foreach (int number in numbers)
                {
                    maps.Add(MapList.List[number]);
                }

                List<EmbedFieldBuilder> vote_fields = new List<EmbedFieldBuilder>();

                for (int i = 0; i < Config.bot.NumberOfVotes; i++)
                {
                    EmbedFieldBuilder votes = new EmbedFieldBuilder();
                    votes.WithName(maps[i])
                        .WithValue($"`Map {i + 1}`")
                        .WithIsInline(true);

                    vote_fields.Add(votes);
                }

                foreach (EmbedFieldBuilder _field in vote_fields)
                    vote_embed.AddField(_field);

                //// Log.Information("MAP VOTE => Building and sending _embed.");
                Caches.Messages.MapVoteMessage = await Context.Channel.SendMessageAsync(embed: vote_embed.Build());
                //// Log.Information("MAP VOTE => Embed successfully sent.");
                await Caches.Messages.MapVoteMessage.AddReactionAsync(one);
                await Caches.Messages.MapVoteMessage.AddReactionAsync(two);
                await Caches.Messages.MapVoteMessage.AddReactionAsync(three);
            }

            [Command("map")]
            [Summary("Randomly chooses a map from a pool and demands that it is used.")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Map()
            {
                // Changed to just randomly pull a single map.


                var random = new Random();
                int index = random.Next(0, MapList.List.Length);

                // Create and display embed for maps selected for next game.
                var voteEmbed = new EmbedBuilder()
                    .WithTitle($"{MapList.List[index]}")
                    .WithDescription("The spirits have spoken.").WithColor(Color.Blue);

                List<EmbedFieldBuilder> vote_fields = new List<EmbedFieldBuilder>();

                Caches.Messages.MapVoteMessage = await Context.Channel.SendMessageAsync(embed: vote_embed.Build());

            }

        */
        // [Command("testing")]
        [Summary(": IGNORE ME!")]
        private async Task GenerateReactEmbed()
        {
            await Context.Channel.TriggerTypingAsync();
            // Timer timer = new Timer(120000); // Timer for auto-posting list embed

            // Start checks
            _embed = new EmbedBuilder();
            _field = new EmbedFieldBuilder();

            // Create message with reaction for queue
            //TODO Reword most of this.
            _embed.WithColor(Color.DarkGreen)
                .WithTitle($"This is the react message for people to 'register' for the queue for the guild the message is in.")
                .WithTimestamp(DateTime.Now);

            //
            _field.WithName("Click or Tap on the reaction to join queue.")
                .WithValue("Basic rules and stuff that users should abide by go here. This");

            //Something to do with sending a message in a specified channel
            var chan = Context.Guild.GetChannel(Guild.GetQueueMessageRoom(GuildId));
            _embed.AddField(_field);
            var ReactionMessage = await Context.Channel.SendMessageAsync(embed: _embed.Build());    // Sends the embed for people to react to.

            Guild.SetQueueMessageId(GuildId, ReactionMessage.Id.ToString());
            if (Emote.TryParse(Guild.GetReaction(GuildId), out Emote emote))
                await ReactionMessage.AddReactionAsync(emote);
            else if (Emoji.TryParse(Guild.GetReaction(GuildId), out Emoji emoji))
                await ReactionMessage.AddReactionAsync(emoji);
            else
                await ReactionMessage.AddReactionAsync(new Emoji("👍"));

            // Guild.ChangeLobbyStatus(GuildId);

        }

    }
}
