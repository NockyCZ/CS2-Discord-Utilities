
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using MySqlConnector;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        private MySqlConnection GetConnection()
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
            {
                Server = Config.Database.Host,
                Port = (uint)Config.Database.Port,
                UserID = Config.Database.User,
                Database = Config.Database.DatabaseName,
                Password = Config.Database.Password,
                Pooling = true
            };

            return new MySqlConnection(builder.ConnectionString);
        }

        public async Task CreateDatabaseConnection()
        {
            using MySqlConnection connection = GetConnection();
            try
            {
                await connection.OpenAsync();
                await CreateTable(connection);
                await CreateLinkCodesTable(connection);
                IsDbConnected = true;
                SendConsoleMessage("[Discord Utilities] The database has been connected!", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] Unable to connect to the database: {ex.Message}", ConsoleColor.Red);
            }
        }

        static async Task CreateTable(MySqlConnection connection)
        {
            try
            {
                using var cmd = new MySqlCommand(
                    @"CREATE TABLE IF NOT EXISTS Discord_Utilities (
                    steamid VARCHAR(32) UNIQUE NOT NULL,
                    discordid VARCHAR(32) NOT NULL,
                    UNIQUE (`steamid`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;", connection);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] There was an error when creating players database: {ex.Message}", ConsoleColor.Red);
            }
        }

        static async Task CreateLinkCodesTable(MySqlConnection connection)
        {
            try
            {
                using var cmd = new MySqlCommand(
                    @"CREATE TABLE IF NOT EXISTS Discord_Utilities_Codes (
                    steamid VARCHAR(32) UNIQUE NOT NULL,
                    code VARCHAR(32) NOT NULL,
                    UNIQUE (`steamid`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;", connection);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] There was an error when creating link database: {ex.Message}", ConsoleColor.Red);
            }
        }
        private async Task<Dictionary<ulong, string>> GetLinkedPlayers()
        {
            var linkedPlayers = new Dictionary<ulong, string>();
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
                                linkedPlayers.Add(ulong.Parse(steamid), discordid);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] There was an error when loading the data: {ex.Message}", ConsoleColor.Red);
            }
            return linkedPlayers;
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
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while entering data into the database: {ex.Message}", ConsoleColor.Red);
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
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while entering new code into the database: {ex.Message}", ConsoleColor.Red);
            }
        }

        public async Task<Dictionary<string, string>> GetCodesList()
        {
            var codesList = new Dictionary<string, string>();
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
                                codesList.Add(code, steamid);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while loading code: {ex.Message}", ConsoleColor.Red);
            }
            return codesList;
        }

        public async Task RemoveCode(string data, bool bySteamid)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    string sql = bySteamid ? "DELETE FROM Discord_Utilities_Codes WHERE steamid = @data" : "DELETE FROM Discord_Utilities_Codes WHERE code = @data";
                    using (var cmd = new MySqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@data", data);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while removing code from the database: {ex.Message}", ConsoleColor.Red);
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
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while removing player from the database: {ex.Message}", ConsoleColor.Red);
            }
        }
        private async Task LoadPlayerData(string steamid, ulong discordID)
        {
            var guild = BotClient!.GetGuild(ulong.Parse(Config.ServerID));
            if (guild == null)
            {
                SendConsoleMessage($"[Discord Utilities] Guild with id '{Config.ServerID}' was not found!", ConsoleColor.Red);
                return;
            }
            var user = guild.GetUser(discordID);
            if (user == null)
            {
                await RemovePlayerData(steamid);
                SendConsoleMessage($"[Discord Utilities] User with ID '{discordID}' was not found! Players has been removed from the Linked players.", ConsoleColor.DarkYellow);
                return;
            }

            await PerformLinkRole(discordID.ToString());
            LoadPlayerDiscordData(ulong.Parse(steamid), discordID);

            if (Config.ConnectedPlayers.Enabled)
                await AddConnectedPlayersRole(discordID);

            Server.NextFrame(() =>
            {
                PerformLinkPermission(ulong.Parse(steamid));
                if (Config.CustomFlagsAndRoles.Enabled)
                {
                    var player = GetTargetBySteamID64(ulong.Parse(steamid));
                    if (player == null || !player.IsValid)
                        return;

                    if (RolesToPermissions.Count != 0)
                    {
                        foreach (var item in RolesToPermissions)
                        {
                            if (item.Value.StartsWith('@'))
                            {
                                if (!AdminManager.PlayerHasPermissions(player, item.Value))
                                    PerformRoleToPermission(discordID, ulong.Parse(steamid), ulong.Parse(item.Key), item.Value);
                            }
                            else if (item.Value.StartsWith('#'))
                            {
                                if (!AdminManager.PlayerInGroup(player, item.Value))
                                    PerformRoleToPermission(discordID, ulong.Parse(steamid), ulong.Parse(item.Key), item.Value);
                            }
                            else
                            {
                                SendConsoleMessage($"[Discord Utilities] Invalid permission '{item.Value}'!", ConsoleColor.Red);
                                return;
                            }
                        }
                    }
                    if (PermissionsToRoles.Count != 0)
                    {
                        foreach (var item in PermissionsToRoles)
                        {
                            if (item.Key.StartsWith('@'))
                            {
                                if (AdminManager.PlayerHasPermissions(player, item.Key))
                                    _ = PerformPermissionToRole(discordID, ulong.Parse(item.Value));
                                else
                                {
                                    if (!Config.CustomFlagsAndRoles.removeRolesOnPermissionLoss)
                                        continue;
                                }
                            }
                            else if (item.Key.StartsWith('#'))
                            {
                                if (AdminManager.PlayerInGroup(player, item.Key))
                                    _ = PerformPermissionToRole(discordID, ulong.Parse(item.Value));
                                else
                                {
                                    if (!Config.CustomFlagsAndRoles.removeRolesOnPermissionLoss)
                                        continue;
                                    _ = PerformRemoveRole(discordID, ulong.Parse(item.Value));
                                }
                            }
                            else
                            {
                                SendConsoleMessage($"[Discord Utilities] Invalid permission '{item.Key}'!", ConsoleColor.Red);
                                return;
                            }
                        }
                    }
                }
            });
        }
    }
}
