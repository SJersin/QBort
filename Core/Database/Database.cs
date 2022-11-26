using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite; // NuGeT
using System.IO;
using System.Threading.Tasks;

/*
 *  Database must be able to manage individual guilds and their users.
 *  Table for holding each guilds individual settings.

    NOTICE FOR LOGGING TO A DB AND NOT A FILE:

    No asynchronous logger methods

    Logging should be so fast that it isn't worth the performance cost of asynchronous code.
    If a logging datastore is slow, don't write to it directly. Consider writing the log messages
    to a fast store initially, then moving them to the slow store later. For example, when logging
    to SQL Server, don't do so directly in a Log method, since the Log methods are synchronous.
    Instead, synchronously add log messages to an in-memory queue and have a background worker
    pull the messages out of the queue to do the asynchronous work of pushing data to SQL Server.

 */

namespace QBort.Core.Database
{
    internal class Database
    {
        #region     Properties
        private static readonly string DatabaseFile = ".data/qbort.db";
        private static readonly string ConnectionString = "DataSource=" + DatabaseFile;
        private static SQLiteCommand cmd;
        private static SQLiteConnection con;
        #endregion
        #region     Database Writing
        internal static int ExecuteWrite(string query, Dictionary<string, object> args)
        {
            int numberOfRowsAffected;

            //setup the connection to the database
            using (con = new SQLiteConnection(ConnectionString))
            {
                try
                {
                    con.Open();
                    //open a new command
                    using (cmd = new SQLiteCommand(query, con))
                    {
                        //set the arguments given in the query
                        foreach (var pair in args)
                        {
                            cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                        }
                        //execute the query and get the number of row affected
                        numberOfRowsAffected = cmd.ExecuteNonQuery();
                    }
                    return numberOfRowsAffected;
                }
                catch (Exception e)
                {
                    Log.Error(Messages.FormatError(e));
                    return -1;
                }
            }
        }
        internal static int ExecuteWrite(string query)
        {
            int numberOfRowsAffected;

            //setup the connection to the database
            using (con = new SQLiteConnection(ConnectionString))
            {
                con.Open();
                //open a new command
                using (cmd = new SQLiteCommand(query, con))
                {
                    //execute the query and get the number of row affected
                    numberOfRowsAffected = cmd.ExecuteNonQuery();
                }
                con.Close();
                return numberOfRowsAffected;
            }
        }
        #endregion
        #region     Database Reading
        /** <summary>
            Processes a query designed to extract data from the database.
            </summary>
            <param name="query">The<pararef="string">containing the query for the database.
            <returns> A<paramref="DataTable" />with the query results, or<paramref="null" />if nothing is found.
        */
        internal static DataTable ExecuteRead(string query)
        {
            if (string.IsNullOrEmpty(query.Trim()))
                return null;

            using (con = new SQLiteConnection(ConnectionString))
            {
                con.Open();
                using (cmd = new SQLiteCommand(query, con))
                {
                    try
                    {
                        var table = new DataTable();
                        using (var da = new SQLiteDataAdapter(cmd))
                            da.Fill(table);
                        con.Close();
                        return table;
                    }
                    catch (Exception e)
                    {
                        Log.Error(Messages.FormatError(e));
                        return null;
                    }
                }
            }
        }
        #endregion
        #region     Check Database and Create Database methods.
        private static void CreateDatabase()
        {
            Log.Information("Attempting to creaste guilds table.");
            Tables.CreateGuildsTable();
            Log.Information("Attempting to create players table.");
            Tables.CreatePlayersTable();
            Log.Information("Attempting to create guildsettings table.");
            Tables.CreateGuildSettingsTable();
            Log.Information("Attempting to create playernotes table.");
            Tables.CreatePlayerNotesTable();
        }
        internal static void CheckDatabase()
        {
            // Check database existance
            if (!File.Exists(DatabaseFile))
            {
                Log.Information("/-------------------------\\");
                Log.Information("|Database not in existance|");
                Log.Information("|Attempting to create one~|");
                Log.Information("\\-------------------------/");
                try
                {
                    Log.Information("Trying to create file...");
                    // File.Create(DatabaseFile);
                    SQLiteConnection.CreateFile(DatabaseFile);
                    Log.Information("Files created successfully..?");
                    CreateDatabase();
                    return;
                }
                catch (Exception e)
                {
                    Log.Fatal($"Database could not initialize successfully.");
                    Log.Error(Messages.FormatError(e));
                }
            }
            #region Database creation check or something

            // string GuildsTableReport, GuildSettingsTableReport, PlayersTableReport;
            // bool RowCountTest, ZeroEntryTest, test3;
            // var check = ExecuteRead("SELECT * FROM Guilds");

            // // If table exists, how many rows does it have? All tables should contain at least a zero entry that is essentially blank, null, or zero values.
            // if (check.Rows.Count == 0)  // Check Row count. If no rows returned what happened to the table?
            //     RowCountTest = false;
            // else RowCountTest = true;

            // // Check for zero entry in the table. If no entry, what happened to it?
            // if (Convert.ToUInt64(check.Rows[0]["GuildId"]) == 0)
            //     ZeroEntryTest = false;
            // else ZeroEntryTest = true;
            #endregion

        }
        internal static string RegisterGuild(ulong GuildId)
        {
            if (!Guild.GuildExists(GuildId))
            {
                int guild = Guild.AddGuild(GuildId),
                    sets = Guild.AddGuildSettings(GuildId);

                if (guild == 1 && sets == 1) return "added";
                else if (guild == 0 && sets == 1) return "Guild table entry creation failed";
                else if (guild == 1 && sets == 0) return "Guild settings entry creation failed";
                else return "Both table entry creations failed";
            }
            else return "exists";
        }
        #endregion

        // Query string sanatizing Function goes here...
    }
}
