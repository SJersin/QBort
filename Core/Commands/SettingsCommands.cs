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
        private readonly EmbedBuilder _embed = new();
        private EmbedFieldBuilder _field;

        [Command("set-listf")]
        [Summary(": Sets the pull list's display formatting by passing a number indicating the style to use.\nGeneric List - 0\nSingle Column - 1\nDouble Column - 2\nex: `set-listf 2`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetPullListType(int format, [Remainder] string buffer = "")
        {
            _ = Context.Channel.TriggerTypingAsync();

            if (format < 0 || format > 2)
            { _ = Context.Channel.SendMessageAsync(embed: Messages.InvalidParameter.Build()); return; }

            _embed.WithTitle("Set List Format");
            string result = format switch
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

            await Context.Channel.SendMessageAsync(embed: _embed.Build());
        }

        [Command("set-ochan")]
        [Summary(": For a Guild specifying the channel, sets the channel they would like to use the `open` command exclusively in.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task SetQueueChannel()
        {
            _ = Context.Channel.TriggerTypingAsync();

            _embed.WithTitle("Set Open Queue Channel");
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

            _embed.WithTitle("Set Group Pull Channel");
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

            _embed.WithTitle("Set Queue Reaction Emote");
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

            _embed.WithTitle("Set Specific Role");
            if (Guild.SetRole(Context.Guild.Id, role.Name) == 1)
                _embed.WithDescription($"Specific role set to {Context.Channel.Name}.").WithColor(Color.Gold);
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

            _embed.WithTitle("Set Game");
            _embed.WithDescription("This isn't working yet.\nError Code: Dev404");

            await Context.Channel.SendMessageAsync(embed: _embed.Build());
        }

        [Command("mode")]
        [Alias("Mode")]
        [Summary(": Sets the mode or game type for the game for which will the queue will be playing.\nex: `mode Team Death Match`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetGameMode([Remainder] string game = "")
        {
            _ = Context.Channel.TriggerTypingAsync();

            _embed.WithTitle("Set game mode");
            _embed.WithDescription("This isn't working yet.\nError Code: Dev404");

            await Context.Channel.SendMessageAsync(embed: _embed.Build());
        }

        [Command("about")]
        [Alias("aboot")]
        [Summary(": Gets the bots version. Maybe more... later.")]
        public async Task GetBotInfo()
        {
            _embed.WithTitle("About Q-υωυ-Bort").WithDescription("Thank you for caring!").WithFooter("Muchos luvu~<3");
            _embed.AddField(
                _field.WithName("Current Version:").WithValue(Version.CurrentVersion));
            await Context.Channel.SendMessageAsync(embed: _embed.Build());
        }

        [Command("support")]
        [Summary(": Gots sum extra luvs to share..?")]
        public async Task ShamelessSolicitation()
        {
            _embed.WithTitle(DevMessages.SupportThankYouMessage).WithDescription(DevMessages.DonationUsagePledge).WithFooter("Muchos luv~<3");
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
