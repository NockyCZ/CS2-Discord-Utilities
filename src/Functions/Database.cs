
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using MySqlConnector;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public static Dictionary<ulong, string> linkedPlayers = new Dictionary<ulong, string>();
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
                IsDbConnected = true;
                await LoadLinkedPlayers();
                SendConsoleMessage("[Discord Utilities] The database has been connected!", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] Unable to connect to the database: {ex.Message}", ConsoleColor.Red);
            }
        }

        static async Task CreateTable(MySqlConnection connection)
        {
            using var cmd = new MySqlCommand(
                @"CREATE TABLE IF NOT EXISTS Discord_Utilities (
                steamid VARCHAR(32) UNIQUE NOT NULL,
                discordid VARCHAR(32) NOT NULL,
                UNIQUE (`steamid`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;", connection);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task UpdateDatabase()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    string sql = "ALTER TABLE Discord_Utilities DROP COLUMN id, MODIFY COLUMN steamid VARCHAR(32) COLLATE utf8mb4_unicode_ci UNIQUE NOT NULL, MODIFY COLUMN discordid VARCHAR(32) NOT NULL, ADD UNIQUE INDEX unique_steamid (steamid);";
                    using (var cmd = new MySqlCommand(sql, connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                    SendConsoleMessage($"[Discord Utilities] The database has been successfully updated!", ConsoleColor.Green);
                }
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] There was an error when updating the database: {ex.Message}", ConsoleColor.Red);
            }
        }

        private async Task LoadLinkedPlayers()
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
        private async Task LoadPlayerData(string steamid)
        {
            var discordID = linkedPlayers[ulong.Parse(steamid)];

            var guild = BotClient!.GetGuild(ulong.Parse(Config.ServerID));
            if (guild == null)
            {
                SendConsoleMessage($"[Discord Utilities] Guild with id '{Config.ServerID}' was not found!", ConsoleColor.Red);
                return;
            }
            var user = guild.GetUser(ulong.Parse(discordID));
            if (user == null)
            {
                linkedPlayers.Remove(ulong.Parse(steamid));
                await RemovePlayerData(steamid);
                SendConsoleMessage($"[Discord Utilities] User with ID '{discordID}' was not found! Players has been removed from the Linked players.", ConsoleColor.DarkYellow);
                return;
            }

            await PerformLinkRole(discordID);
            LoadPlayerDiscordData(ulong.Parse(steamid), ulong.Parse(discordID));

            if (Config.ConnectedPlayers.Enabled)
                await AddConnectedPlayersRole(ulong.Parse(discordID));

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
                                    PerformRoleToPermission(ulong.Parse(discordID), ulong.Parse(steamid), ulong.Parse(item.Key), item.Value);
                            }
                            else if (item.Value.StartsWith('#'))
                            {
                                if (!AdminManager.PlayerInGroup(player, item.Value))
                                    PerformRoleToPermission(ulong.Parse(discordID), ulong.Parse(steamid), ulong.Parse(item.Key), item.Value);
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
                                    _ = PerformPermissionToRole(ulong.Parse(discordID), ulong.Parse(item.Value));
                                else
                                {
                                    if (!Config.CustomFlagsAndRoles.removeRolesOnPermissionLoss)
                                        continue;
                                }
                            }
                            else if (item.Key.StartsWith('#'))
                            {
                                if (AdminManager.PlayerInGroup(player, item.Key))
                                    _ = PerformPermissionToRole(ulong.Parse(discordID), ulong.Parse(item.Value));
                                else
                                {
                                    if (!Config.CustomFlagsAndRoles.removeRolesOnPermissionLoss)
                                        continue;
                                    _ = PerformRemoveRole(ulong.Parse(discordID), ulong.Parse(item.Value));
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
