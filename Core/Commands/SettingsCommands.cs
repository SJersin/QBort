using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using QBort.Core.Database;
using QBort.Enums;

namespace QBort.Core.Commands
{
    // TODO update all these ugly channel messages with pretty embeds.
    public class SettingsCommands : ModuleBase<SocketCommandContext>
    {
        private EmbedBuilder _embed;
        private EmbedFieldBuilder _field;

        [Command("set-listf")]
        [Summary(": Sets the pull list's display formatting by passing a number indicating the style to use.\nGeneric List - 0\nSingle Column - 1\nDouble Column - 2\nex: `set-listf 2`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task PullListFormatSetting([Remainder] string format = "")
        {
            _ = Context.Channel.TriggerTypingAsync();

            if (UInt16.TryParse(format.Trim(), out ushort nformat))
            {
                if (nformat < 0 || nformat > 2)
                { _ = Context.Channel.SendMessageAsync(embed: Messages.InvalidParameter.Build()); return; }

                _embed = new EmbedBuilder().WithTitle("Set List Format");
                string result = nformat switch
                {
                    0 => "Plain",
                    1 => "SingleColumn",
                    2 => "DoubleColumn",
                    _ => throw new System.IndexOutOfRangeException(),
                };

                if (Settings.SetPullMsgFormat(Context.Guild.Id, result) != 1)
                    _embed.WithDescription("Couldn't change the format setting. D:");
                else
                    _embed.WithDescription($"The list format has been changed to use a {result} format.");
            }
            else if (format.Trim().ToLower().Contains("column"))
            {
                if (format.Trim().ToLower().Contains("single"))
                    if (Settings.SetPullMsgFormat(Context.Guild.Id, "SingleColumn") != 1)
                        _embed.WithDescription("Couldn't change the format setting. D:");
                    else
                        _embed.WithDescription($"The list format has been changed to use a SingleColumn format.");
                else if (format.Trim().ToLower().Contains("double"))
                    if (Settings.SetPullMsgFormat(Context.Guild.Id, "DoubleColumn") != 1)
                        _embed.WithDescription("Couldn't change the format setting. D:");
                    else
                        _embed.WithDescription($"The list format has been changed to use a DoubleColumn format.");
            }
            else
            {
                if (Settings.SetPullMsgFormat(Context.Guild.Id, "Plain") != 1)
                    _embed.WithDescription("Couldn't change the format setting. D:");
                else
                    _embed.WithDescription($"The list format has been changed to use a Plain format.");
            }
            await Context.Channel.SendMessageAsync(embed: _embed.Build());
        }

        [Command("pulltype")]
        [Summary(": How to pull out... players.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task PullMethodTypeSetting(string type = "")
        {

            _ = Context.Channel.TriggerTypingAsync();
            _embed = new EmbedBuilder().WithTitle("User Pool Selection Method:");
            string[] _typedesc = { "'Fairly(tm)' Random", "In order" };

            int method = Guild.GetPullMethod(Context.Guild.Id);
            if (string.IsNullOrWhiteSpace(type))
            {
                _embed.WithDescription(string.Concat("Group pulls are currently set to be ", _typedesc[method], '.'));
                await Context.Channel.SendMessageAsync(embed: _embed.Build());
                return;
            }
            else if (UInt16.TryParse(type.Trim(), out ushort _type)) // If the value passed is a number
            {
                if (_type < 2 && method != _type) // If the passed value is between 0 and 1
                {
                    if (Settings.SetPullMethod(Context.Guild.Id, Convert.ToString(_type)) == 1)
                        _embed.WithDescription(string.Concat("Group pulls have been set to ", _typedesc[_type], '.'));
                    else
                        _embed.WithDescription("There was an issue changing the method's setting...");
                }
                else if (_type == method) // If the passed value is the same as the current guild setting
                    _embed.WithDescription("Sure. Why not. Just for you.");
                else
                    // Don't know how we got here...
                    _embed.WithDescription("Uhm... What's that doing there?");
            }
            else if (type.ToLower().Contains("order"))
            {
                if (Settings.SetPullMethod(Context.Guild.Id, Convert.ToString(1)) == 1)
                    _embed.WithDescription(string.Concat("Group pulls have been set to ", _typedesc[1], '.'));
                else
                    _embed.WithDescription("There was an issue changing the method's setting...");
            }
            else if (type.ToLower().Contains("rando"))
            {
                if (Settings.SetPullMethod(Context.Guild.Id, Convert.ToString(0)) == 1)
                    _embed.WithDescription(string.Concat("Group pulls have been set to ", _typedesc[0], '.'));
                else
                    _embed.WithDescription("There was an issue changing the method's setting...");
            }
            else
                _embed.WithDescription("I'm sorry, I didn't understand that");

            await Context.Channel.SendMessageAsync(embed: _embed.Build());
        }

        [Command("set-ochan")]
        [Summary(": For a Guild specifying the channel, sets the channel they would like to use the `open` command exclusively in.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task SetQueueChannel()
        {
            _ = Context.Channel.TriggerTypingAsync();

            _embed = new EmbedBuilder().WithTitle("Set Open Queue Channel");
            if (Guild.SetQueueMessageRoom(Context.Guild.Id, Context.Channel.Id) == 1)
                _embed.WithDescription($"Queue channel set to {Context.Channel.Name}.").WithColor(Color.Gold);
            else
                _embed.WithDescription("The channel could not be set.").WithColor(Color.Red);

            await Context.Channel.SendMessageAsync(embed: _embed.Build());
        }
        [Command("set-pchan")]
        [Summary(": Sets the channel ID for a Guild specifying the channel they would like to use the `new` command exclusively in.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task SetPullChannel()
        {
            _ = Context.Channel.TriggerTypingAsync();

            _embed = new EmbedBuilder().WithTitle("Set Group Pull Channel");
            if (Guild.SetPullMessageRoom(Context.Guild.Id, Context.Channel.Id) == 1)
                _embed.WithDescription($"Queue pull channel set to {Context.Channel.Name}.").WithColor(Color.Gold);
            else
                _embed.WithDescription("The channel could not be set.").WithColor(Color.Red);

            await Context.Channel.SendMessageAsync(embed: _embed.Build());
        }

        [Command("set-react")]
        [Summary(": sets the reaction for users to react to.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task SetGuildReact(string repeat)
        {
            _ = Context.Channel.TriggerTypingAsync();

            _embed = new EmbedBuilder().WithTitle("Set Queue Reaction Emote");
            if (Guild.SetReaction(Context.Guild.Id, repeat) == 1)
                _embed.WithDescription($"Queue message react set to {repeat}.").WithColor(Color.Gold);
            else
                _embed.WithDescription("The emote could not be set.").WithColor(Color.Red);

            await Context.Channel.SendMessageAsync(embed: _embed.Build());
        }

        [Command("set-role")]
        [Summary(": sets the role the bot will the bot will check that users have.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task SetGuildRole(IRole role)
        {
            _ = Context.Channel.TriggerTypingAsync();

            _embed = new EmbedBuilder().WithTitle("Set Specific Role");
            if (Guild.SetRole(Context.Guild.Id, role.Name) == 1)
                _embed.WithDescription($"Specific role set to {role.Name}.").WithColor(Color.Gold);
            else
                _embed.WithDescription("The role could not be set.").WithColor(Color.Red);

            await Context.Channel.SendMessageAsync(embed: _embed.Build());
        }

        [Command("game")]
        [Alias("Game")]
        [Summary(": Sets the game for which will the queue will be playing.\nex: `game Paladins`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetGame([Remainder] string game = "")
        {
            _ = Context.Channel.TriggerTypingAsync();

            _embed = new EmbedBuilder().WithTitle("Set Game").WithDescription("This isn't working yet.\nError Code: Dev404");

            await Context.Channel.SendMessageAsync(embed: _embed.Build());
        }

        [Command("mode")]
        [Alias("Mode")]
        [Summary(": Sets the mode or game type for the game for which will the queue will be playing.\nex: `mode TDM`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetGameMode([Remainder] string game = "")
        {
            _ = Context.Channel.TriggerTypingAsync();
            _embed = new EmbedBuilder().WithTitle("Set game mode").WithDescription("This isn't working yet.\nError Code: Dev404");

            await Context.Channel.SendMessageAsync(embed: _embed.Build());
        }

        [Command("about")]
        [Alias("aboot")]
        [Summary(": Gets the bots version. Maybe more... later.")]
        public async Task GetBotInfo()
        {
            _embed = new EmbedBuilder().WithTitle("About Q-υωυ-Bort").WithDescription("Thank you for caring!").WithFooter("Muchos luvu~<3");
            _embed.AddField(
                new EmbedFieldBuilder().WithName("Current Version:").WithValue(Version.CurrentVersion));
            _embed.AddField(DevMessages.ChangeLogLink);
            _embed.AddField(DevMessages.BotDocumentationLink);

            await Context.Channel.SendMessageAsync(embed: _embed.Build());
        }

        [Command("support")]
        [Summary(": Gots sum extra luvs to share..?")]
        public async Task ShamelessSolicitation()
        {
            _embed = new EmbedBuilder().WithTitle(DevMessages.SupportThankYouMessage)
                .WithDescription(DevMessages.DonationUsagePledge).WithFooter("Muchos luv~<3");
            _field = new EmbedFieldBuilder().WithName("Patreon:").WithValue(DevMessages.PatreonLink);
            _embed.AddField(_field);
            _field = new EmbedFieldBuilder().WithName("YouTube:").WithValue(DevMessages.YouTubeLink);
            _embed.AddField(_field);
            _field = new EmbedFieldBuilder().WithName("Twitch:").WithValue(DevMessages.TwitchLink);
            _embed.AddField(_field);
            _field = new EmbedFieldBuilder().WithName("Discord:").WithValue(DevMessages.DiscordLink);
            _embed.AddField(_field);
            _field = new EmbedFieldBuilder().WithName("GitHub:").WithValue(DevMessages.GitHubLink);
            _embed.AddField(_field);
            await Context.Channel.SendMessageAsync(embed: _embed.Build());
        }
    }
}
