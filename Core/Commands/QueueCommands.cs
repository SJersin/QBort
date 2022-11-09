﻿using System;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using QBort.Core.Database;
using System.Data;
using Discord.WebSocket;


/*
    The Rebirth into Q-Bort!
 */

namespace QBort.Core.Commands
{
    public class QueueCommands : ModuleBase<SocketCommandContext>
    {
        private ulong GuildId;
        private EmbedBuilder _embed;
        private SocketGuildUser _leader;
        private EmbedFieldBuilder _field;

        /// See about creating temporary channels for use instead of having to create bot specific rooms
        /// can be expanded to include voice channels. Can make this very vancy.
        [Command("open")]
        [Alias("Create, create")]
        [Summary(": Create a new queue for users to join.\nYou must pass a user role @mention.\nEx: `create @customs`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task OpenQueue(IRole role, [Remainder] string message = "")
        {
            GuildId = Context.Guild.Id;
            await Context.Channel.TriggerTypingAsync();
            // Timer timer = new Timer(120000); // Timer for auto-posting list embed
            try
            {
                if (Context.Channel.Id != Guild.GetQueueMessageRoom(Context.Guild.Id))
                { await Context.Channel.SendMessageAsync(Messages.WrongChannelWarning); return; }
                if (Guild.GetLobbyStatus(GuildId))
                {
                    await Context.Channel.SendMessageAsync(Messages.LobbyIsOpen); return;
                }
                _embed = new EmbedBuilder();
                _field = new EmbedFieldBuilder();

                // Create message with reaction for queue 
                _embed.WithColor(Color.DarkGreen)
                    .WithTitle($"The queue is now open! React to this message to register for the queue.")
                    .WithTimestamp(DateTime.Now);
                _field.WithName("Click or Tap on the reaction to join queue.")
                    .WithValue("Remember to be respectful towards other people and follow the rules that have been established by the community!");
                _embed.AddField(_field);
                string gem; // Guild EMote 
                // Start checks

                var SendEmbedTask =
                    Context.Channel.SendMessageAsync(embed: _embed.Build());

                using (var dt = Guild.GetGuildSettings(GuildId))
                {
                    gem = dt.Rows[0]["Reaction"].ToString();
                }

                //Something to do with sending a message in a specified channel

                var ReactionMessage = await SendEmbedTask;    // Sends the embed for people to react to.
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
            try
            {
                Guild.ChangeLobbyStatus(GuildId);
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                await Context.Channel.SendMessageAsync("Couldn't change guild status.");
            }
        }

        [Command("close")]
        [Summary(": Close off and clear the queue.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task CloseQueue()
        {
            GuildId = Context.Guild.Id;
            // order this should close things:
            // Delete queue message
            // Set everyone to inactive
            // Send thank you embed

            try
            {
                if (!Guild.GetLobbyStatus(GuildId))
                { await Context.Channel.SendMessageAsync(Messages.LobbyIsClosed); return; }
                if (Context.Channel.Id != Guild.GetQueueMessageRoom(Context.Guild.Id))
                { await Context.Channel.SendMessageAsync(Messages.WrongChannelWarning); return; }

                if (Guild.ChangeLobbyStatus(GuildId) == 1)
                {
                    List<string> problems = new List<string>();
                    var embed = new EmbedBuilder().WithTitle("The customs queue has been closed!")
                        .WithColor(Color.DarkRed)
                        .WithDescription("Thank you everyone who joined in today's session!!").WithCurrentTimestamp();
                    var SendEmbedTask =
                        Context.Channel.SendMessageAsync(embed: embed.Build());
                    // Check to make sure that the reference is to both the proper channel and message ids.
                    
                    Task DeletePullMessageTask;
                    try
                    {
                        DeletePullMessageTask =
                            Context.Channel.GetCachedMessage(Guild.GetPullMessageId(GuildId)).DeleteAsync();
                    }
                    catch
                    {
                        DeletePullMessageTask = Task.CompletedTask;
                    }
                    try
                    {
                        using (var player = Guild.AllPlayersList(GuildId))
                            foreach (DataRow i in player.Rows)
                                if (Player.ResetPlayStats(GuildId, Convert.ToUInt64(i["PlayerId"])) != 1)
                                    problems.Add(i["PlayerId"].ToString());

                        Guild.ClearPlayCounts(GuildId);
                        string ohno = $"There are {problems.Count} problems logged. They are:";

                        foreach (var problem in problems)
                            ohno += $"\n{problem}";

                        Log.Error(ohno);
                    }
                    catch (Exception e)
                    {
                        Log.Error(Messages.FormatError(e) + "\n ^-v-^-v-^- Line 141 -^-v-^-v-^");
                    }

                    try
                    {
                        Guild.SetPullMessageId(GuildId, 0);
                        await SendEmbedTask;
                        await DeletePullMessageTask;
                    }
                    catch (Exception e)
                    {
                        Log.Error(Messages.FormatError(e) + "\n ^-v-^-v-^- Line 159 -^-v-^-v-^");
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync(Messages.LobbyIsClosed);
                }
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
            }
        }

        [Command("list")]
        [Summary(": Provides the list of everyone in the queue database.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task ShowQueueList()
        {
            GuildId = Context.Guild.Id;

            //TODO Make the list embed after guild registration and agreement.
            _embed = new EmbedBuilder();
            if (!Guild.GetLobbyStatus(GuildId))
            {
                await Context.Channel.SendMessageAsync(embed:
                        _embed.WithTitle("Oops!").WithDescription(Messages.LobbyIsClosed).Build());
                return;
            }

            string gem = Guild.GetReaction(GuildId);
            string list = string.Empty;

            using (var PlayersToList = Guild.ActivePlayersList(GuildId))
            {
                int activePlayers = PlayersToList.Rows.Count;
                foreach (DataRow p in PlayersToList.Rows)
                    list += Context.Guild.GetUser(Convert.ToUInt64(p["PlayerId"])).DisplayName + ": " + Convert.ToString(p["PlayCount"]) + " | ";
            }
            list = list.Remove(list.LastIndexOf('|') - 1);

            _field = new EmbedFieldBuilder().WithName("Active users: ").WithValue(list);

            try
            {
                int activePlayers = Guild.GetActivePlayerCount(GuildId);
                if (activePlayers > 0)
                {
                    _embed.WithTitle($"There are {activePlayers} players in the list")
                       .WithCurrentTimestamp();
                    _embed.AddField(_field);
                }
                else
                {
                    _embed.WithTitle("The q-υωυ-e is Empty.").WithDescription("{QueueBot} is sad!");
                }
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
            List<Player> PlayerList = new List<Player>();
            int GroupSize = 0, GuildMaxGroupSize = 0;

            var typing =
                Context.Channel.TriggerTypingAsync();

            #region Start checks

            if (!Guild.GetLobbyStatus(GuildId))
            { await Context.Channel.SendMessageAsync(Messages.LobbyIsClosed); return; }
            if (Context.Channel.Id != Guild.GetPullMessageRoom(GuildId))
            { await Context.Channel.SendMessageAsync(Messages.WrongChannelWarning); return; }

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

            using (var ActiveList = Guild.ActivePlayersList(GuildId))
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
                await Context.Channel.SendMessageAsync(Messages.LowActivePlayerWarning);
            }

            var random = new Random();
            HashSet<int> numbers = new HashSet<int>();
            int index = 0;
            string mentions = string.Empty;
            List<string> list = new List<string>();
            List<ulong> recall = new List<ulong>();
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

            List<Task<IUserMessage>> DMs = new List<Task<IUserMessage>>(); // using var is acceptable here, I just didn't want to.
            try
            {
                ulong x;
                while (GroupSize > 0)
                {

                    // If the count of the list of players with the lowest play count, just throw them all in.
                    if (PlayerList.FindAll(p => p.PlayCount == PlayerList[0].PlayCount).Count <= GroupSize)
                    {
                        foreach (var player in
                            PlayerList.FindAll(p => p.PlayCount == PlayerList[0].PlayCount))
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
                        await Context.Channel.SendMessageAsync(Context.Guild.GetUser(r).DisplayName + " - foreach recall catch.");
                        Console.WriteLine("346 Line Damnit");
                        Log.Error(Messages.FormatError(e)); continue;
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

            // 2-Column List starts here.

            string grouplist = string.Empty;


            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (i % 2 == 0 && !list[i].Equals(string.Empty))
                        grouplist += list[i];
                    else
                        grouplist += " - " + list[i] + '\n';
                }

                // If the last entry doesn't have a newline break, add it in so it doesn't look stupid.
                if (!grouplist.EndsWith('\n')) grouplist += "\n";

                grouplist += _leader.DisplayName + " - Kami"; //TODO Adapt for the use of a "constant" player. Add field to Guild table?

                EmbedFieldBuilder field = new EmbedFieldBuilder().WithName("Next up is:")
                    .WithValue(grouplist).WithIsInline(true);
                _embed.AddField(field);
                var SendEmbedTask = Context.Channel.SendMessageAsync(mentions, embed: _embed.Build());

                // Start calling all of our awaited tasks.
                foreach (var dm in DMs)
                    await dm;
                var Messagae = await SendEmbedTask;

                Guild.SetRecallGroup(GuildId, recall.ToArray());
                Guild.SetPullMessageId(GuildId, Messagae.Id); // Use this for storing called games message id to delete later. Also, I'm aware of the typo. It's been with this project since creation. It has tenure.
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
            {
                await Context.Channel.SendMessageAsync(Messages.LobbyIsClosed);
                return;
            }
            string recall = Guild.RecallGroup(GuildId);
            string[] group = recall.Split(',');
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

        [Command("replace")] // TODO FIXME LIKE YESTERDAY!!!
        [Summary(": Calls a new player to replace one that is unable to participate after they have been called. Used by passing @mentions\nEx: `replace @Johnny @May`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task ReplacePlayer([Remainder] string original = "")
        {
            await Context.Channel.SendMessageAsync("This command is closed due to renovations.");
            return;

        // Fix this code and remove the temp message above.
            try
            {
                List<ulong> UsersToReplace = new List<ulong>();
                List<Player> Replacements = new List<Player>();
                int count = Context.Message.MentionedUsers.Count;
                GuildId = Context.Guild.Id;

                if (count < 1) // No mentions passed; no one to replace.
                    return;
                else
                    foreach (var oldgroup in Context.Message.MentionedUsers)
                        UsersToReplace.Add(oldgroup.Id); // get users to replace

                // get active players
                var ActivePlayerTable = Guild.ActivePlayersList(GuildId);

                // pull old recall list
                var RecallGroup = Guild.RecallGroup(GuildId).Split(',');
                List<string> RecallList = new List<string>();
                foreach (string y in RecallGroup)
                    RecallList.Add(y);

                foreach (var y in UsersToReplace)
                    RecallList.Remove(Convert.ToString(y));

                List<Player> ActivePlayerList = new List<Player>();

                foreach (DataRow n in ActivePlayerTable.Rows) // Convert the table to a usable player list
                    try
                    {
                        ActivePlayerList.Add(new Player(Convert.ToUInt64(n["PlayerId"]), Convert.ToInt32(n["PlayCount"])));
                    }
                    catch (Exception e)
                    {
                        Log.Error(Messages.FormatError(e));
                    }
                ActivePlayerTable.Dispose();

                foreach (var user in RecallList) // remove users from the player list that are in the queue recall list.
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(user))
                            ActivePlayerList.Remove(ActivePlayerList.Find(p => p.PlayerId == Convert.ToUInt64(user.Trim())));
                    }
                    catch (Exception e)
                    {
                        Log.Error(Messages.FormatError(e));
                    }
                Player x;
                // GET THE NEW USERS
                if (UsersToReplace.Count > 0)
                    foreach (var o in UsersToReplace)
                    {
                        Random random = new Random();
                        int index = random.Next(0, ActivePlayerList.FindAll(p => p.PlayCount == ActivePlayerList[0].PlayCount).Count);
                        if (ActivePlayerList.Count > 0)
                        {
                            x = ActivePlayerList[index];
                            Replacements.Add(x);
                            RecallList.Add(x.PlayerId.ToString());
                            ActivePlayerList.Remove(x);
                            Player.IncreasePlayCount(GuildId, x.PlayerId);
                        }
                        else
                        {
                            await Context.Channel.SendMessageAsync("No suitable replacement players found.");
                            return;
                        }
                    }

                string msg = string.Empty;
                if (count == 1) // Only one user to replace
                {
                    msg += Context.Guild.GetUser(UsersToReplace[0]).Mention + " ";
                    Player.DecreasePlayCount(GuildId, Context.Guild.GetUser(UsersToReplace[0]).Id);
                }
                else // More than one user to replace
                    foreach (var o in UsersToReplace)
                    {
                        msg += Context.Guild.GetUser(o).Mention + " ";
                        Player.DecreasePlayCount(GuildId, o);
                    }

                msg += " is being replaced with";
                await Context.Channel.SendMessageAsync("Recalled List? " + RecallList[0].Trim());

                if (count == 1)
                {
                    msg += " " + Context.Guild.GetUser(Convert.ToUInt64(RecallList[0].Trim())).Mention + " ";
                }
                else
                {
                    foreach (var u in Replacements)
                    {
                        msg += " " + Context.Guild.GetUser(u.PlayerId).Mention + ",";
                    }
                    msg.Remove(msg.Length - 1);
                    msg += " ";
                }
                msg += "in **" + Context.User.Username + "**'s lobby!\n The updated player list is:\n";
                string recall = string.Empty;

                foreach (var p in RecallList)
                {
                    msg += $" {Context.Guild.GetUser(Convert.ToUInt64(p)).DisplayName} |";
                }
                msg.Remove(msg.LastIndexOf('|'));
                // msg += "."; 
                List<ulong> list = new List<ulong>();
                foreach (string id in RecallList)
                    list.Add(Convert.ToUInt64(id));

                Guild.SetRecallGroup(GuildId, list.ToArray());
                await Context.Channel.SendMessageAsync(msg);
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
            }
        }

        [Command("swap")]
        [Summary(": Not yet implemented.")]
        public async Task SwapPlayer([Remainder] string players = "")
        {
            await Context.Channel.SendMessageAsync("Don't be impatient.");
        }

        [Command("status")]
        [Summary(": Returns the status of the guild's queue.")]
        public async Task QueueStatus()
        {
            await Context.Channel.SendMessageAsync("The queue is " + (Guild.GetLobbyStatus(Context.Guild.Id) ? "open." : "closed."));
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

                List<EmbedFieldBuilder> voteFields = new List<EmbedFieldBuilder>();

                for (int i = 0; i < Config.bot.NumberOfVotes; i++)
                {
                    EmbedFieldBuilder votes = new EmbedFieldBuilder();
                    votes.WithName(maps[i])
                        .WithValue($"`Map {i + 1}`")
                        .WithIsInline(true);

                    voteFields.Add(votes);
                }

                foreach (EmbedFieldBuilder field in voteFields)
                    voteEmbed.AddField(field);

                //// Log.Information("MAP VOTE => Building and sending Embed.");
                Caches.Messages.MapVoteMessage = await Context.Channel.SendMessageAsync(embed: voteEmbed.Build());
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

                List<EmbedFieldBuilder> voteFields = new List<EmbedFieldBuilder>();

                Caches.Messages.MapVoteMessage = await Context.Channel.SendMessageAsync(embed: voteEmbed.Build());

            }

        */
        private async Task GenerateReactEmbed()
        {
            await Context.Channel.TriggerTypingAsync();
            // Timer timer = new Timer(120000); // Timer for auto-posting list embed

            // Start checks
            var embed = new EmbedBuilder();
            var field = new EmbedFieldBuilder();

            // Create message with reaction for queue
            //TODO Reword most of this. 
            embed.WithColor(Color.DarkGreen)
                .WithTitle($"This is the react message for people to 'register' for the queue for the guild the message is in.")
                .WithTimestamp(DateTime.Now);

            //
            field.WithName("Click or Tap on the reaction to join queue.")
                .WithValue("Basic rules and stuff that users should abide by go here. This");

            //Something to do with sending a message in a specified channel
            var chan = Context.Guild.GetChannel(Guild.GetQueueMessageRoom(GuildId));
            embed.AddField(field);
            var ReactionMessage = await Context.Channel.SendMessageAsync(embed: embed.Build());    // Sends the embed for people to react to.

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





