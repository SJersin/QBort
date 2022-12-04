/*
    This class holds the methods for the various sql queries
    to get and change data reguarding "Players"


    Table Schema:
      GuildSettings -
        GuildId INTEGER,            Primary Key
        GroupSize INTEGER,          Default number of users to pull from active list.
        MaxGroupSize INTEGER,       Maximum number of users to pull from active list.
        BotPrefix VARCHAR(5),       Character(s) used for the bot to respond to. Should allow per server customization.
        Reaction VARCHAR(50),       Unicode or Discord unique name for the reaction to agree to server terms. Should allow per server customization.
        QueMsgRoom INTEGER,         A specified room Id for the "open" command. If set, will only allow "open" to used in the specified room.
        PullMsgRoom INTEGER,        A specified room Id for the "new" command. If set, will only allow "new" to used in the specified room.
        ModSettingsRoom INTEGER,    A specified room Id for mod commands. If set, will only allow mod commands to used in the specified room.
        UserSettingsRoom INTEGER,   A specified room Id for user commands. If set, will only allow user commands to used in the specified room.
        QueMsgId INTEGER,           The id of the message that is created and sent by the bot when the "open" command is used.
        PullMsgId INTEGER,          The id of the message that is created and sent by the bot when the "new" command is used.
        Reaction VARCHAR(50),      Name of the role to give users when they react to the server terms message
        SubLv INTEGER               If I ever feel to incorporate subscription tiers. Capitalism, Ho!

      Guilds -
        GuildId INTEGER,            Primary Key
        IsOpen INTEGER,             Boolean value for the status of the guilds queue. 0 for false (queue closed), 1 for true (queue open).
        GameName VARCHAR(50),       String value for the name of the game the queue will be hosting.
        GameMode VARCHAR(50)        String value for the name of the game mode the next pull will be using.
        RecallGroup VARCHAR(100)

      Players -
        GuildId INTEGER,            Primary Key
        PlayerId INTEGER,           Primary Key
        PlayCount INTEGER,          The number of games the user has played.
        IsActive INTEGER,           Boolean value for the active status of the user.
        SpecialGames INTEGER,       Boolean value to indicate if the user want to participate in "special rules" games.
        IsBanned INTEGER,           Boolean value for the banned status of the user.
        BanReason VARCHAR(500),     Reason for the banning.
        Agreed INTEGER              Boolean value for the agrement
*/

using System;
using System.Collections.Generic;
using System.Data;
using QBort.Core.Structures;

namespace QBort.Core.Database
{
    internal class Player
    {
        ///<summary>The player's Id. Type: <paramref="ulong"/></summary>
        internal ulong PlayerId { get; }
        ///<summary>The player's game count. Type: <paramref="int"/></summary>
        internal int PlayCount { get; }

        ///<summary>
        ///A player object that holds the PlayerId and PlayCount values for a player retrieved from the database.
        ///</summary>
        ///<param name="id">The <paramref="ulong" /> id of the player.</param>
        ///<param name="c">The <paramref="int" /> count of the player's games played.</param>
        internal Player(ulong id, int c)
        {
            //TODO consider replacing this with Structures.PlayerData
            PlayerId = id;
            PlayCount = c;
        }

        #region  ********************** STATIC FUNCTIONS ***********************************
        internal static int AddPlayer(ulong GuildId, ulong PlayerId)

        {
            const string query = "INSERT INTO Players(GuildId, PlayerId, PlayCount, QuePos, IsActive, SpecialGames, IsBanned, BanReason, Agreed) VALUES(@GuildId, @PlayerId, "
                + "@PlayCount, @QuePos, @IsActive, @SpecialGames, @IsBanned, @BanReason, @Agreed)";

            //here we are setting the parameter values that will be actually
            //replaced in the query in Execute method
            var args = new Dictionary<string, object>
            {
                // TODO Double check the default IsActive setting before Finalizing Release.
                { "@GuildId", GuildId },
                { "@PlayerId", PlayerId },
                { "@PlayCount", Convert.ToString(0) },
                { "@QuePos", Convert.ToString(0) },
                { "@IsActive", Convert.ToString(0) },
                { "@SpecialGames", Convert.ToString(0) },
                { "@IsBanned", Convert.ToString(0) },
                { "@BanReason", "Nothing yet." },
                { "@Agreed", Convert.ToString(0) }
            };

            return Database.ExecuteWrite(query, args);
        }
        internal static int ResetPlayStats(ulong GuildId, ulong PlayerId)
        {
            string query = $"UPDATE Players SET IsActive = 0, QuePos = 0, PlayCount = 0 WHERE GuildId = {GuildId} AND PlayerId = {PlayerId}";
            return Database.ExecuteWrite(query);
        }
        internal static int DecreasePlayCount(ulong GuildId, ulong PlayerId)
        {
            var dt = Database.ExecuteRead($"SELECT PlayCount FROM Players WHERE GuildId = {GuildId} AND PlayerId = {PlayerId}");
            int current = Convert.ToInt16(dt.Rows[0]["PlayCount"]);
            int newvalue = current - 1;

            string query = $"UPDATE Players SET PlayCount = {newvalue} WHERE GuildId = {GuildId} AND PlayerId = {PlayerId}";
            return Database.ExecuteWrite(query);
        }
        internal static int EditPlayerData(ulong GuildId, ulong PlayerId, string setting, string value)
        {
            // TODO Sanitize this string?
            string query = $"UPDATE Players SET {setting} = {value} WHERE GuildId = {GuildId} AND PlayerId = {PlayerId}";
            return Database.ExecuteWrite(query);
        }
        internal static bool Exists(ulong GuildId, ulong PlayerId)
        {
            try
            {
                string query = $"SELECT * FROM Players Where GuildId = {GuildId} AND PlayerId = {PlayerId}";
                if (Database.ExecuteRead(query).Rows.Count == 0)
                    return false;
                else
                    return true;
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e)); return true;
            }
        }
        internal static int GetActiveStatus(ulong GuildId, ulong PlayerId)
        {

            string query = $"SELECT * FROM Players WHERE GuildId = {GuildId} AND PlayerId = {PlayerId}";
            var dt = Database.ExecuteRead(query);

            try
            {
                return Convert.ToUInt16(dt.Rows[0]["IsActive"]);
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                return -1;
            }
        }
        internal static int GetPlayCount(ulong GuildId, ulong PlayerId)
        {
            string query = $"SELECT PlayCount FROM Players WHERE PlayerId = {PlayerId} AND GuildId = {GuildId}";
            var dt = Database.ExecuteRead(query);
            try
            {
                return Convert.ToUInt16(dt.Rows[0]["PlayCount"]);
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                return 0;
            }
        }
        internal static int GetQueuePosition(ulong GuildId, ulong PlayerId)
        {
            string query = $"SELECT QuePos FROM Players WHERE PlayerId = {PlayerId} AND GuildId = {GuildId}";
            var dt = Database.ExecuteRead(query);
            try
            {
                return Convert.ToUInt16(dt.Rows[0]["QuePos"]);
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                return -1;
            }
        }
        internal static DataTable GetPlayerData(ulong GuildId, ulong PlayerId)
        {
            string query = $"SELECT * FROM Players WHERE PlayerId = '{PlayerId}' AND GuildId = '{GuildId}'";
            try
            {
                return Database.ExecuteRead(query);
            }
            catch (Exception e)
            {
                Log.Error(
                    string.Concat("Guild: [", GuildId,
                            "] | Player: [", PlayerId,
                            "]\n", Messages.FormatError(e))
                    );
                return null;
            }
        }
        internal static int IncreasePlayCount(ulong GuildId, ulong PlayerId)
        {
            var dt = Database.ExecuteRead($"SELECT * FROM Players WHERE GuildId = {GuildId} AND PlayerId = {PlayerId}");
            int current = Convert.ToInt16(dt.Rows[0]["PlayCount"]);
            int _n = current + 1;
            string query = $"UPDATE Players SET PlayCount = {_n} WHERE GuildId = {GuildId} AND PlayerId = {PlayerId}";
            return Database.ExecuteWrite(query);
        }
        internal static int ChangeActiveStatus(ulong GuildId, ulong PlayerId)
        {
            string query = $"SELECT IsActive FROM Players WHERE PlayerId = {PlayerId} AND GuildId = {GuildId}";
            var dt = Database.ExecuteRead(query);
            if (dt is null || dt.Rows.Count == 0) return -1;

            int value = Convert.ToUInt16(dt.Rows[0]["IsActive"]);

            value = value != 0 ? 0 : 1;
            query = $"UPDATE Players SET IsActive = '{value}' WHERE PlayerId = '{PlayerId}' AND GuildId = '{GuildId}'";
            return Database.ExecuteWrite(query);
        }
        internal static DataTable GetConfirmedPlayers(ulong GuildId)
        {
            return Database.ExecuteRead("SELECT * FROM Players WHERE GuildId = " + GuildId + " WHERE Agreed = 1");
        }
        #endregion
    }
}
