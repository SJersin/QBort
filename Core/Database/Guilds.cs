/*
    This class holds the methods for the various sql queries
    regarding Guilds and their settings. The settings
    calls from here return mostly from the GuildSettings table.
*/

using System;
using System.Collections.Generic;
using System.Data;
using QBort.Enums;

namespace QBort.Core.Database
{
    internal class Guild
    {
        #region Table Seeding Functions

        /// <summary>
        /// Adds the guild to the guild table and seeds default values.
        /// </summary>
        /// <param name="GuildId">The guild whose record to seed.</param>
        /// <returns>
        /// 1 if the guild was successfully added to the table.
        /// Not 1 if the attempt was unsuccessful.
        /// </returns>
        internal static int AddGuild(ulong GuildId)
        {
            const string query = "INSERT INTO Guilds (GuildId, IsOpen, GameName, GameMode, RecallGroup, IsActive, SubLv) "
                + "VALUES (@GuildId, @IsOpen, @GameName, @GameMode, @RecallGroup, @IsActive, @SubLv)";

            // Here we are setting the parameter values that will be
            // replaced in the query in the ExecuteWrite method.
            var args = new Dictionary<string, object> {
                { "@GuildId", GuildId },
                { "@IsOpen", Convert.ToString(0) },
                { "@GameName", "Kami Quest 64" },
                { "@GameMode", "I win."},
                { "@RecallGroup", "Empty"},
                { "@IsActive", Convert.ToString(1)},
                { "@SubLv", Convert.ToString(0)}

            };

            return Database.ExecuteWrite(query, args);
        }
        /// <summary>
        /// Adds the guild to the guildsettings table and seeds default values.
        /// </summary>
        /// <param name="GuildId">The guild whose settings record to seed.</param>
        /// <returns>
        /// 1 if the guild was successfully added to the table.
        /// Not 1 if the attempt was unsuccessful.
        /// </returns>
        internal static int AddGuildSettings(ulong GuildId)
        {

            const string query = "INSERT INTO GuildSettings (GuildId, GroupSize, MaxGroupSize, BotPrefix, Reaction, QueMsgRoom, "
            + "PullMsgRoom, ModSettingsRoom, UserSettingsRoom, QueMsgId, PullMsgId, Role, CleanBot, PullMsgFormat, FIFO) "
            + "VALUES(@GuildId, @GroupSize, @MaxGroupSize, @BotPrefix, @Reaction, @QueMsgRoom, @PullMsgRoom, @ModSettingsRoom, "
            + "@UserSettingsRoom, @QueMsgId, @PullMsgId, @Role, @CleanBot, @PullMsgFormat, @FIFO);";

            var args = new Dictionary<string, object> {
                { "@GuildId", GuildId },
                { "@GroupSize", Convert.ToString(8)},
                {"@MaxGroupSize",Convert.ToString(10)},
                {"@BotPrefix", "+"},
                {"@Reaction", "👍"},
                {"@QueMsgRoom", Convert.ToString(0)},
                {"@PullMsgRoom", Convert.ToString(0)},
                {"@ModSettingsRoom", Convert.ToString(0)},
                {"@UserSettingsRoom", Convert.ToString(0)},
                {"@QueMsgId", Convert.ToString(0)},
                {"@PullMsgId", Convert.ToString(0)},
                {"@Role", string.Empty},
                {"@CleanBot", 0},
                {"@PullMsgFormat", GroupListFormat.Plain.ToString()},
                {"@FIFO", Convert.ToString(0)}
            };

            return Database.ExecuteWrite(query, args);
        }

        #endregion
        /// <summary>
        /// Sets the banned status of a player to <paramref name="true" /> (or 1) for the guild.
        /// <para>
        /// This command BANS the player.
        /// </para>
        /// </summary>
        /// <param name="GuildId">The guild whose passing judgement</param>
        /// <param name="PlayerId">The player whose judgement is being passed.</param>
        /// <param name="reason">The explaination behind the judgening.</param>
        /// <returns>
        /// 1 if judgement was swift, precise, and delicious.
        /// Not 1 if judgement was vetoed by higher powers.
        /// </returns>
        internal static int BanPlayer(ulong GuildId, ulong PlayerId, string reason)
        {
            string query = $"UPDATE Players SET IsBanned = 1, BanReason = '{reason}' WHERE GuildId = '{GuildId}' AND PlayerId = '{PlayerId}'";
            return Database.ExecuteWrite(query);
        }
        /// <summary>
        /// Sets the banned status of a player to <paramref name="false" /> (or 0) for the guild.
        /// <para>
        /// This command removes the ban that was placed on the player.
        /// </para>
        /// </summary>
        /// <param name="GuildId">The guild whose passing judgement</param>
        /// <param name="PlayerId">The player whose judgement is being passed.</param>
        /// <param name="reason">The explaination behind the judgening.</param>
        /// <returns>
        /// 1 if judgement was swift, precise, and delicious.
        /// Not 1 if judgement was vetoed by higher powers.
        /// </returns>
        internal static int UnbanPlayer(ulong GuildId, ulong PlayerId)
        {
            string query = $"UPDATE Players SET IsBanned = 0 WHERE GuildId = '{GuildId}' AND PlayerId = '{PlayerId}'";
            return Database.ExecuteWrite(query);
        }
        /// <summary>
        /// Retrieves the count of active players in the guild.
        /// </summary>
        /// <param name="GuildId">The guild whose count to retrieve.</param>
        /// <returns>The number of active players in the current queue.</returns>
        internal static int GetActivePlayerCount(ulong GuildId)
        {
            return Database.ExecuteRead($"SELECT * From Players WHERE GuildId = '{GuildId}' And IsActive = 1 And IsBanned = 0").Rows.Count;
        }
        /// <summary>
        /// Gets the active players in the guild with the ordering:
        /// <para>
        ///     PlayCount ASC,
        ///     QuePos ASC
        /// </para>
        /// </summary>
        /// <param name="GuildId"></param>
        /// <returns>A <typeparamref name="DataTable" />-typed table containg all active and not-banned players. </returns>
        internal static DataTable GetActivePlayersList(ulong GuildId)
        {
            return Database.ExecuteRead($"SELECT * From Players WHERE GuildId = '{GuildId}' And IsActive = 1 And IsBanned = 0 ORDER BY PlayCount ASC, QuePos ASC");
        }
        /// <summary>
        /// Retrieves all the registered users for the guild.
        /// </summary>
        /// <param name="GuildId">The guild whose list to retrieve.</param>
        /// <returns>A <typeparamref name="DataTable" />-typed table containg all players. </returns>
        internal static DataTable GetAllPlayersList(ulong GuildId)
        {
            return Database.ExecuteRead($"SELECT * From Players WHERE GuildId = '{GuildId}'");
        }
        /// <summary>
        /// Checks if the current is currently registered with the bot.
        /// </summary>
        /// <param name="GuildId">The guild whose existance is in question.</param>
        /// <returns><paramref name="true" /> if the guild is registered with the bot or <paramref name="false" /> if the guild is not.</returns>
        internal static bool GuildExists(ulong GuildId)
        {
            var dt = Database.ExecuteRead("SELECT * From Players WHERE GuildId = " + GuildId);
            if (dt.Rows.Count < 1 || dt.Rows is null)
                return false;
            else
                return true;
        }
        internal static int SetRole(ulong GuildId, string set)
        {
            string query = $"UPDATE GuildSettings Set Role = '{set}' WHERE GuildId = '{GuildId}'";
            return Database.ExecuteWrite(query);
        }
        internal static int ChangeLobbyStatus(ulong GuildId)
        {
            int set;
            var dt = Database.ExecuteRead("SELECT IsOpen From Guilds WHERE GuildId = " + GuildId);
            if (dt.Rows.Count < 1 || dt.Rows is null)
                return -1;
            if (Convert.ToInt16(dt.Rows[0]["IsOpen"]) != 0)
                set = 0;
            else
                set = 1;
            string query = $"UPDATE Guilds Set IsOpen = '{set}' WHERE GuildId = '{GuildId}'";
            return Database.ExecuteWrite(query);
        }
        internal static DataTable GetGuildSettings(ulong GuildId)
        {
            string query = $"SELECT * FROM GuildSettings WHERE GuildId = '{GuildId}'";
            return Database.ExecuteRead(query);
        }
        internal static int CheckForGuild(ulong GuildId)
        {
            string query = $"Select * from Guilds where GuildId = '{GuildId}'";
            return Database.ExecuteRead(query).Rows.Count;
        }
        internal static int ClearPlayCounts(ulong GuildId)
        {
            string query = $"UPDATE Players Set PlayCount = 0 WHERE GuildId = '{GuildId}'";
            return Database.ExecuteWrite(query);
        }
        internal static int GetGroupSize(ulong GuildId)
        {
            var dt = Database.ExecuteRead("SELECT GroupSize From GuildSettings WHERE GuildId = " + GuildId);
            return Convert.ToInt16(dt.Rows[0]["GroupSize"]);
        }
        internal static int GetMaxGroupSize(ulong GuildId)
        {
            var dt = Database.ExecuteRead("SELECT MaxGroupSize From GuildSettings WHERE GuildId = " + GuildId);
            var size = Convert.ToInt16(dt.Rows[0]["MaxGroupSize"]);
            return size;
        }
        /// <summary>
        /// Gets the guild's pull method settings.
        /// </summary>
        /// <param name="GuildId">The guild whose existance is in question.</param>
        /// <returns>0 if the setting is 'Random' or 1 if the setting is 'Ordered'.</returns>
        internal static int GetPullMethod(ulong GuildId)
        {
            var dt = Database.ExecuteRead("SELECT FIFO From GuildSettings WHERE GuildId = " + GuildId);
            return Convert.ToInt16(dt.Rows[0]["FIFO"]);
        }
        internal static bool GetLobbyStatus(ulong GuildId)
        {
            var dt = Database.ExecuteRead("SELECT IsOpen From Guilds WHERE GuildId = " + GuildId);
            if (Convert.ToInt16(dt.Rows[0]["IsOpen"]) != 0)
                return true;
            else return false;
        }
        internal static string GetPrefix(ulong GuildId)
        {
            var dt = Database.ExecuteRead("SELECT BotPrefix From GuildSettings WHERE GuildId = " + GuildId);
            string results = Convert.ToString(dt.Rows[0]["BotPrefix"]);
            if (results == null || results == string.Empty)
                return "No Prefix Assigned";
            else return results;
        }
        internal static string GetReaction(ulong GuildId)
        {
            var dt = Database.ExecuteRead("SELECT Reaction From GuildSettings WHERE GuildId = " + GuildId);
            string results = Convert.ToString(dt.Rows[0]["Reaction"]);
            if (results == null || results == string.Empty)
                return "No Prefix Assigned";
            else return results;
        }
        internal static ulong GetQueueMessageRoom(ulong GuildId)
        {
            try
            {
                var dt = Database.ExecuteRead("SELECT QueMsgRoom From GuildSettings WHERE GuildId = " + GuildId);
                ulong results = Convert.ToUInt64(dt.Rows[0]["QueMsgRoom"]);
                return results;
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                return 0;
            }
        }
        internal static ulong GetPullMessageRoom(ulong GuildId)
        {
            try
            {
                var dt = Database.ExecuteRead("SELECT PullMsgRoom From GuildSettings WHERE GuildId = " + GuildId);
                ulong results = Convert.ToUInt64(dt.Rows[0]["PullMsgRoom"]);
                return results;
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                return 0;
            }
        }
        internal static ulong GetQueueMessageId(ulong GuildId)
        {
            try
            {
                var dt = Database.ExecuteRead("SELECT QueMsgId From GuildSettings WHERE GuildId = " + GuildId);
                ulong results = Convert.ToUInt64(dt.Rows[0]["QueMsgId"]);
                return results;
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                return 0;
            }
        }
        internal static ulong GetPullMessageId(ulong GuildId)
        {
            try
            {
                var dt = Database.ExecuteRead("SELECT PullMsgId From GuildSettings WHERE GuildId = " + GuildId);
                var results = Convert.ToUInt64(dt.Rows[0]["PullMsgId"]);
                return results;
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                return 0;
            }
        }
        internal static ulong GetModSettingsRoom(ulong GuildId)
        {
            try
            {
                var dt = Database.ExecuteRead("SELECT PullMsgRoom From Guilds WHERE GuildId = " + GuildId);
                ulong results = Convert.ToUInt64(dt.Rows[0]["PullMsgRoom"]);
                return results;
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                return 0;
            }
        }
        internal static ulong GetUserSettingsRoom(ulong GuildId)
        {
            try
            {
                var dt = Database.ExecuteRead("SELECT PullMsgRoom From Guilds WHERE GuildId = " + GuildId);
                ulong results = Convert.ToUInt64(dt.Rows[0]["PullMsgRoom"]);
                return results;
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                return 0;
            }
        }
        internal static string GetRole(ulong GuildId)
        {
            try
            {
                var dt = Database.ExecuteRead("SELECT Role From Guilds WHERE GuildId = " + GuildId);
                if (dt.Rows.Count != 0)
                    return Convert.ToString(dt.Rows[0]["Role"]);
                else
                    return "This guild has not yet assigned a role.";
            }
            catch (Exception e)
            {
                Log.Error(Messages.FormatError(e));
                return null;
            }
        }
        internal static string GetPullMessageFormat(ulong GuildId)
        {
            var dt = Database.ExecuteRead(string.Concat("SELECT PullMsgFormat From GuildSettings WHERE GuildId = ", GuildId));
            string results = Convert.ToString(dt.Rows[0]["PullMsgFormat"]);

            if (results == null || results == string.Empty)
                return "No Prefix Assigned";
            else return results;
        }
        internal static string RecallGroup(ulong GuildId)
        {
            return Database.ExecuteRead("SELECT * FROM Guilds WHERE GuildId = " + GuildId).Rows[0]["RecallGroup"].ToString();
        }
        internal static int SetQueueMessageId(ulong GuildId, string set)
        {
            string query = $"UPDATE GuildSettings Set QueMsgId = '{set}' WHERE GuildId = '{GuildId}'";
            return Database.ExecuteWrite(query);
        }
        internal static int SetPullMessageId(ulong GuildId, ulong set)
        {
            string query = $"UPDATE GuildSettings Set PullMsgId = '{set}' WHERE GuildId = '{GuildId}'";
            return Database.ExecuteWrite(query);
        }
        internal static int SetPullMessageRoom(ulong GuildId, ulong set)
        {
            string query = $"UPDATE GuildSettings Set PullMsgRoom = '{set}' WHERE GuildId = '{GuildId}'";
            return Database.ExecuteWrite(query);
        }
        internal static int SetQueueMessageRoom(ulong GuildId, ulong set)
        {
            string query = $"UPDATE GuildSettings Set QueMsgRoom = '{set}' WHERE GuildId = '{GuildId}'";
            return Database.ExecuteWrite(query);
        }
        internal static int SetRecallGroup(ulong GuildId, ulong[] list)
        {
            string group = string.Empty;
            foreach (var PlayerId in list)
                group = string.Concat(group, PlayerId, ',');
            if (group.EndsWith(','))
                group = group.Remove(group.LastIndexOf(','));
            string query = $"UPDATE Guilds Set RecallGroup = '{group}' WHERE GuildId = '{GuildId}'";
            return Database.ExecuteWrite(query);
        }
        internal static int SetReaction(ulong GuildId, string set)
        {
            string query = $"UPDATE GuildSettings Set Reaction = '{set}' WHERE GuildId = '{GuildId}'";
            return Database.ExecuteWrite(query);
        }
    }
}
