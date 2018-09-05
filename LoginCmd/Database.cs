using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;
using TShockAPI.DB;

namespace LoginCmd {
    class Database {
        public static IDbConnection db;
        private static Dictionary<int, string> playercmds = new Dictionary<int, string>();

        public static void DBConnect() {
            string lower = TShock.Config.StorageType.ToLower();
            if (!(lower == "mysql")) {
                if (lower == "sqlite")
                    db = new SqliteConnection(string.Format("uri=file://{0},Version=3", Path.Combine(TShock.SavePath, "Startcmd.sqlite")));
            } else {
                string[] strArray = TShock.Config.MySqlHost.Split(':');
                MySqlConnection mySqlConnection = new MySqlConnection();
                mySqlConnection.ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};", strArray[0], (strArray.Length == 1 ? "3306" : strArray[1]), TShock.Config.MySqlDbName, TShock.Config.MySqlUsername, TShock.Config.MySqlPassword);
                db = mySqlConnection;
            }
            SqlTableCreator sqlTableCreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : (IQueryBuilder)new MysqlQueryCreator());
            string name = "Startcmd";

            SqlColumn[] sqlColumnArray = new SqlColumn[2];
            SqlColumn sqlColumn = new SqlColumn("owner", MySqlDbType.Int32);
            sqlColumn.Primary = true;
            sqlColumn.Unique = true;

            sqlColumn.Length = 2;
            sqlColumnArray[0] = sqlColumn;

            sqlColumnArray[1] = new SqlColumn("commands", MySqlDbType.Text) {
                Length = 200
            };

            SqlTable table = new SqlTable(name, sqlColumnArray);
            sqlTableCreator.EnsureTableStructure(table);
        }

        public static void LoginCommands(TSPlayer player) {
            List<string> cmdList = new List<string>();
            using (QueryResult queryResult = db.QueryReader("SELECT commands FROM Startcmd WHERE owner=@0", player.User.ID)) {
                while (queryResult.Read()) {
                    string commands = queryResult.Get<string>("commands");
                    if (commands != "") {
                        foreach (string command in commands.Split(',')) {
                            for (int x = 0; x < command.Length; x++) {
                                if (command[x] == ' ') command.Remove(x, 1);
                            }
                            cmdList.Add(command);
                        }
                    }
                }
            }

            playercmds[player.User.ID] = String.Join(",", cmdList);

            foreach (string cmd in cmdList) {
                Commands.HandleCommand(player, cmd);
            }
        }

        public static string ListCMD(int owner) {
            if (playercmds.ContainsKey(owner)) return playercmds[owner];
            return "No commands found";
        }

        public static bool DelCMD(int owner, string cmd) {
            if (playercmds.ContainsKey(owner)) {
                string commands = playercmds[owner];
                List<string> cmdList = new List<string>();
                if (commands != "") {
                    foreach (string command in commands.Split(',').ToList()) {
                        for (int x = 0; x < command.Length; x++) {
                            if (command[x] == ' ') command.Remove(x, 1);
                        }
                        cmdList.Add(command);
                    }
                }

                string newcommands = "";
                for (int x = 0; x < cmdList.Count; x++) {
                    if (cmdList[x] == cmd) continue;
                    newcommands += cmdList[x];
                    if (x != cmdList.Count - 1) newcommands += ",";
                }
                playercmds[owner] = newcommands;
                db.Query("UPDATE Startcmd SET commands=@0 WHERE owner=@1;", newcommands, owner);
                return true;
            }
            TShock.Log.Error(string.Format("Database error: Failed to delete from Startcmd where owner = {0}.", owner));
            return false;
        }

        public static void DelAllCMD(int owner) {
            if (db.Query("DELETE FROM Startcmd WHERE owner=@0", owner) == 1) {
                playercmds[owner] = "";
                return;
            }
            TShock.Log.Error(string.Format("Database error: Failed to insert players where owner = {0}.", owner));
        }

        public static void AddCMD(int owner, string command) {
            if (playercmds[owner] == "") {
                db.Query("DELETE FROM Startcmd WHERE owner=@0", owner);
                db.Query("INSERT INTO Startcmd (owner, commands) VALUES (@0, @1);", owner, command);
                playercmds[owner] = command;
            } else if (playercmds[owner].Any()) {
                string commands = playercmds[owner] + "," + command;
                db.Query("UPDATE Startcmd SET commands=@0 WHERE owner=@1;", commands, owner);
                playercmds[owner] = commands;
            } else if (db.Query("INSERT INTO Startcmd (owner, commands) VALUES (@0, @1);", owner, command) == 1) {
                playercmds[owner] = command;
                return;
            }
            TShock.Log.Error(string.Format("Database error: Failed to insert players where owner = {0}.", owner));
        }
    }
}
