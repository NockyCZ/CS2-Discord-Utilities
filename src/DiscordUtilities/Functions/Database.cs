using System.Collections.Concurrent;

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
                await LoadLinkedPlayers();
                Perform_SendConsoleMessage("The database has been connected!", ConsoleColor.Green);
                await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"Unable to connect to the database: '{ex.Message}'", ConsoleColor.Red);
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
                Perform_SendConsoleMessage($"There was an error when creating linked players database: '{ex.Message}'", ConsoleColor.Red);
            }
        }

        public async Task UpdateOrLoadPlayerData(CCSPlayerController player, string SteamID, int playedTime, bool load = true, string ipAddress = "")
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

                                    // Looked up here (thread pool) with the IP captured on the game thread, opening the GeoIP database is file I/O
                                    string[]? country = null;
                                    if (!string.IsNullOrEmpty(ipAddress) && ulong.TryParse(SteamID, out var countrySteamId))
                                        country = LookupPlayerCountry(ipAddress, countrySteamId);

                                    Server.NextFrame(() =>
                                    {
                                        if (player.IsValid && player.AuthorizedSteamID != null && playerData.ContainsKey(player.Slot))
                                        {
                                            playerData[player.Slot].PlayedTime = time;
                                            playerData[player.Slot].FirstJoin = firstJoin;
                                            playerData[player.Slot].LastSeen = lastSeen;

                                            if (Config.Link.Enabled)
                                            {
                                                if (linkedPlayers.TryGetValue(player.AuthorizedSteamID!.SteamId64, out var linkedDiscordId))
                                                {
                                                    if (playerData.ContainsKey(player.Slot))
                                                        playerData[player.Slot].IsLinked = true;
                                                    // Without the bot it is done later, on the first 'Ready' (LoadConnectedLinkedPlayers)
                                                    if (IsBotConnected)
                                                        _ = LoadPlayerData(player.AuthorizedSteamID.SteamId64.ToString(), linkedDiscordId);
                                                }
                                            }
                                            if (country != null)
                                                UpdatePlayerCountry(player.AuthorizedSteamID!.SteamId64, country);
                                            PlayerDataLoaded(player);
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
                Perform_SendConsoleMessage($"There was an error when loading/updating player data: '{ex.Message}'", ConsoleColor.Red);
            }
        }

        public async Task InsertPlayerData(string steamid, string discordid, string username)
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
                if (linkedPlayers.TryAdd(ulong.Parse(steamid), ulong.Parse(discordid)))
                {
                    Server.NextFrame(() =>
                    {
                        var player = Utilities.GetPlayerFromSteamId(ulong.Parse(steamid));
                        if (player != null)
                        {
                            _ = LoadPlayerData(steamid, ulong.Parse(discordid));
                            if (Config.Link.LinkIngameSettings.SendLinkedMessageToPlayer)
                                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AccountLinked", username]}");
                            if (Config.Link.LinkIngameSettings.SendLinkedMessageToAll)
                                Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AccountLinkedAll", player.PlayerName, username]}");
                            if (!string.IsNullOrEmpty(Config.Link.LinkIngameSettings.LinkedSound))
                                player.ExecuteClientCommand($"play {Config.Link.LinkIngameSettings.LinkedSound}");
                        }
                    });
                }

            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while entering data into the database: '{ex.Message}'", ConsoleColor.Red);
            }
        }

        public static async Task LoadLinkedPlayers()
        {
            // Loaded into a new collection and swapped in, so players are never seen as unlinked during a refresh
            var loadedPlayers = new ConcurrentDictionary<ulong, ulong>();
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
                                if (ulong.TryParse(steamid, out var steamId64) && ulong.TryParse(discordid, out var discordId64))
                                    loadedPlayers.TryAdd(steamId64, discordId64);
                            }
                        }
                    }
                }
                linkedPlayers = loadedPlayers;
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"There was an error when loading the data: '{ex.Message}'", ConsoleColor.Red);
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
                linkedPlayers.TryRemove(ulong.Parse(steamid), out _);
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while removing player from the database: '{ex.Message}'", ConsoleColor.Red);
            }
        }
    }
}
