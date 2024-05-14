
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using MySqlConnector;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        private static MySqlConnection GetConnection()
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
            {
                Server = databaseData!.Server,
                Port = databaseData.Port,
                UserID = databaseData.User,
                Database = databaseData.Database,
                Password = databaseData.Password,
                Pooling = true
            };

            return new MySqlConnection(builder.ConnectionString);
        }

        public static async Task CreateDatabaseConnection()
        {
            using MySqlConnection connection = GetConnection();
            try
            {
                await connection.OpenAsync();
                await CreateTable(connection);
                IsDbConnected = true;
                await LoadLinkCodes();
                await LoadLinkedPlayers();
                Perform_SendConsoleMessage("[Discord Utilities] The database has been connected!", ConsoleColor.Green);
                await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] Unable to connect to the database: {ex.Message}", ConsoleColor.Red);
            }
        }

        public static async Task CreateTable(MySqlConnection connection)
        {
            try
            {
                using var cmd = new MySqlCommand(
                @"CREATE TABLE IF NOT EXISTS Discord_Utilities (
                    steamid VARCHAR(32) PRIMARY KEY UNIQUE NOT NULL,
                    discordid VARCHAR(32) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS Discord_Utilities_Codes (
                    steamid VARCHAR(32) PRIMARY KEY UNIQUE NOT NULL,
                    code VARCHAR(32) NOT NULL,
                    created TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                
                CREATE TABLE IF NOT EXISTS DU_time (
                    steamid VARCHAR(32) PRIMARY KEY UNIQUE NOT NULL,
                    firstjoin TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    lastseen TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    playedtime INT NOT NULL DEFAULT 0
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;", connection);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] There was an error when creating linked players database: {ex.Message}", ConsoleColor.Red);
            }
        }

        public async Task UpdateOrLoadPlayerData(CCSPlayerController player, string SteamID, int playedTime, bool load = true)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    string sqlLoad = @"
                                INSERT INTO `DU_time` (`steamid`, `lastseen`)
                                VALUES (
                                    @steamid,
                                    @lastseen
                                )
                                ON DUPLICATE KEY UPDATE
                                    `steamid` = @steamid,
                                    `lastseen` = @lastseen;

                                SELECT
                                    `firstjoin`,
                                    `lastseen`,
                                    `playedtime`
                                FROM
                                    `DU_time`
                                WHERE
                                    `steamid` = @steamid;
                            ";

                    string sqlUpdate = @"
                                INSERT INTO `DU_time` (`steamid`, `lastseen`, `playedtime`)
                                VALUES (
                                    @steamid,
                                    @lastseen,
                                    @playedtime
                                )
                                ON DUPLICATE KEY UPDATE
                                    `lastseen` = @lastseen,
                                    `playedtime` = @playedtime;
                            ";

                    using (var cmd = new MySqlCommand(load ? sqlLoad : sqlUpdate, connection))
                    {
                        cmd.Parameters.AddWithValue("@steamid", SteamID);
                        cmd.Parameters.AddWithValue("@lastseen", DateTime.Now);
                        cmd.Parameters.AddWithValue("@playedtime", playedTime);

                        if (load)
                        {
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    var time = reader.GetInt32("playedtime");
                                    var firstJoin = reader.GetDateTime("firstjoin");
                                    var lastSeen = reader.GetDateTime("lastseen");

                                    Server.NextFrame(() =>
                                    {
                                        if (playerData.ContainsKey(player))
                                        {
                                            playerData[player].PlayedTime = time;
                                            playerData[player].FirstJoin = firstJoin;
                                            playerData[player].LastSeen = lastSeen;

                                            if (Config.Link.Enabled)
                                            {
                                                if (linkedPlayers.ContainsKey(player.AuthorizedSteamID!.SteamId64))
                                                {
                                                    if (playerData.ContainsKey(player))
                                                        playerData[player].IsLinked = true;
                                                    _ = LoadPlayerData(player.AuthorizedSteamID.SteamId64.ToString(), linkedPlayers[player.AuthorizedSteamID.SteamId64]);
                                                }
                                            }
                                        }
                                    });
                                }
                            }
                        }
                        else
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] There was an error when loading/updating player data: {ex.Message}", ConsoleColor.Red);
            }
        }

        public async Task InsertPlayerData(string steamid, string discordid)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    string sql = "INSERT INTO Discord_Utilities (steamid, discordid) VALUES (@steamid, @discordid) ON DUPLICATE KEY UPDATE steamid = @steamid";
                    using (var cmd = new MySqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@steamid", steamid);
                        cmd.Parameters.AddWithValue("@discordid", discordid);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                if (!linkedPlayers.ContainsKey(ulong.Parse(steamid)))
                    linkedPlayers.Add(ulong.Parse(steamid), ulong.Parse(discordid));
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] An error occurred while entering data into the database: {ex.Message}", ConsoleColor.Red);
            }
        }

        public async Task InsertNewCode(string steamid, string code)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    string sql = "INSERT INTO Discord_Utilities_Codes (steamid, code) VALUES (@steamid, @code) ON DUPLICATE KEY UPDATE steamid = @steamid";
                    using (var cmd = new MySqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@steamid", steamid);
                        cmd.Parameters.AddWithValue("@code", code);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                if (!linkCodes.ContainsKey(code))
                    linkCodes.Add(code, steamid);
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] An error occurred while entering new code into the database: {ex.Message}", ConsoleColor.Red);
            }
        }

        public static async Task LoadLinkedPlayers()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    string sql = "SELECT * FROM Discord_Utilities";
                    using (var cmd = new MySqlCommand(sql, connection))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string steamid = reader.GetString("steamid");
                                string discordid = reader.GetString("discordid");
                                if (!linkedPlayers.ContainsKey(ulong.Parse(steamid)))
                                    linkedPlayers.Add(ulong.Parse(steamid), ulong.Parse(discordid));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] There was an error when loading the data: {ex.Message}", ConsoleColor.Red);
            }
        }

        public static async Task LoadLinkCodes()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    var sql = "SELECT * FROM Discord_Utilities_Codes";

                    using (var cmd = new MySqlCommand(sql, connection))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string steamid = reader.GetString("steamid");
                                string code = reader.GetString("code");
                                if (!linkCodes.ContainsKey(code))
                                    linkCodes.Add(code, steamid);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] An error occurred while loading code: {ex.Message}", ConsoleColor.Red);
            }
        }

        public async Task RemoveCode(string data)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    //string sql = bySteamid ? "DELETE FROM Discord_Utilities_Codes WHERE steamid = @data" : "DELETE FROM Discord_Utilities_Codes WHERE code = @data";
                    string sql = "DELETE FROM Discord_Utilities_Codes WHERE code = @data";
                    using (var cmd = new MySqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@data", data);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                if (linkCodes.ContainsKey(data))
                    linkCodes.Remove(data);
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] An error occurred while removing code from the database: {ex.Message}", ConsoleColor.Red);
            }
        }
        public async Task RemovePlayerData(string steamid)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    string sql = "DELETE FROM Discord_Utilities WHERE steamid = @steamid";
                    using (var cmd = new MySqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@steamid", steamid);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                if (linkedPlayers.ContainsKey(ulong.Parse(steamid)))
                    linkedPlayers.Remove(ulong.Parse(steamid));
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] An error occurred while removing player from the database: {ex.Message}", ConsoleColor.Red);
            }
        }
    }
}
