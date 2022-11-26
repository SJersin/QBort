/*
    Player Notes Table Scheme

GuildId INTEGER
PlayerId INTEGER
NoteId INTEGER
NoteDate VARCHAR(50)
Note VARCHAR(5000)

*/

using System;
using System.Collections.Generic;
using System.Data;

namespace QBort.Core.Database
{
    class PlayerNotes
    {
        private static readonly string SetNoteQuery = "INSERT INTO Players(GuildId, PlayerId, NoteId, NoteDate, Note) Values (@GuildId, @PlayerId, @NoteId, @NoteDate, @Note)";
        private readonly Dictionary<string, object> Values = new();
        private int _noteCount = 0;

        private int GetPlayerNoteCount(ulong GuildId, ulong PlayerId)
        {
            string query = $"SELECT count(NoteId) AS 'Count' FROM PlayerNotes WHERE GuildId = {GuildId} AND PlayerId = {PlayerId}";
            using var dt = Database.ExecuteRead(query);
            if (dt != null)
                return Convert.ToInt16(dt.Rows[0]["Count"]);
            else return -1;
        }
        string[] GetPlayerNotes(ulong GuildId, ulong PlayerId)
        {
            string query = $"SELECT NoteId, Note FROM PlayerNotes WHERE GuildId = {GuildId} AND PlayerId = {PlayerId}";
            string[] notes;
            int _noteCount = GetPlayerNoteCount(GuildId, PlayerId);
            if (_noteCount < 0)
            {
                Log.Error("GetPlayerNotes has experienced an error. Return code -1");
                return null;
            }
            else if (_noteCount == 0)
            {
                return new string[] { "There are no notes for this player." };
            }
            else
                using (var dt = Database.ExecuteRead(query))
                    if (dt != null)
                    {
                        if (dt.Rows.Count > 1)
                        {
                            notes = new string[dt.Rows.Count];
                            int count = 0;
                            foreach (DataRow item in dt.Rows)
                            {
                                notes[count] = item["Note"].ToString();
                                count++;
                            }
                        }
                        else
                            notes = new string[] { dt.Rows[0]["Note"].ToString() };
                        return notes;
                    }
                    else return null;
        }
        /**
            <summary></summary>
            <param name="GuildId">The Guild the note is from.</param>
            <param name="PlayerId">The Id of the player the note is about.</param>
            <param name="NotedBy">The<paramref="Username" />of the person who wrote the note.</param>
            <param name="Note">The desciptive note being noted by the noter on the notee</param>
            <returns>Disappointment... or an int... we'll see.</returns>
        */
        int SetPlayerNote(ulong GuildId, ulong PlayerId, string NotedBy, string Note)
        {
            _noteCount = GetPlayerNoteCount(GuildId, PlayerId);
            Note = string.Concat("[Per: ", NotedBy, "] : ", Note);
            #region Add Values
            Values.Add("@GuildId", GuildId);
            Values.Add("@PlayerId", PlayerId);
            Values.Add("@NoteId", _noteCount);
            Values.Add("@NoteDate", DateTime.Now.ToShortDateString());
            Values.Add("@Note", Note);
            #endregion
            return Database.ExecuteWrite(SetNoteQuery, Values);
        }
    }
}
