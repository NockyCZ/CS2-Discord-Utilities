
using CounterStrikeSharp.API;
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
                SendConsoleMessage("[Discord Utilities] Database has been connected!", ConsoleColor.Green);
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
                id INT AUTO_INCREMENT PRIMARY KEY,
                steamid VARCHAR(64) NOT NULL,
                discordid VARCHAR(64) NOT NULL
            )", connection);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task LoadPlayerData(string steamid)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    string sql = "SELECT discordid FROM Discord_Utilities WHERE steamid = @steamid";
                    using (var cmd = new MySqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@steamid", steamid);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string discordID = reader.GetString("discordid");
                                if (!linkedPlayers.ContainsKey(ulong.Parse(steamid)))
                                {
                                    linkedPlayers.Add(ulong.Parse(steamid), discordID);
                                    await PerformLinkRole(discordID);
                                    LoadPlayerDiscordData(ulong.Parse(steamid), ulong.Parse(discordID));

                                    Server.NextFrame(() =>
                                    {
                                        PerformLinkPermission(ulong.Parse(steamid));
                                        if (Config.CustomFlagsAndRoles.Enabled)
                                        {
                                            var player = GetTargetBySteamID64(ulong.Parse(steamid));
                                            if (RolesToPermissions.Count != 0)
                                            {
                                                foreach (var item in RolesToPermissions)
                                                {
                                                    if (!item.Value.StartsWith('@') || item.Value.StartsWith('#'))
                                                    {
                                                        SendConsoleMessage($"[Discord Utilities] Invalid permission '{item.Value}'!", ConsoleColor.Red);
                                                        return;
                                                    }
                                                    else
                                                    {
                                                        if (!AdminManager.PlayerHasPermissions(player, item.Value))
                                                            PerformRoleToPermission(ulong.Parse(discordID), ulong.Parse(steamid), ulong.Parse(item.Key), item.Value);
                                                    }
                                                }
                                            }
                                            if (PermissionsToRoles.Count != 0)
                                            {
                                                foreach (var item in PermissionsToRoles)
                                                {
                                                    if (!item.Key.StartsWith('@') || item.Key.StartsWith('#'))
                                                    {
                                                        SendConsoleMessage($"[Discord Utilities] Invalid permission '{item.Value}'!", ConsoleColor.Red);
                                                        return;
                                                    }
                                                    else
                                                    {
                                                        if (AdminManager.PlayerHasPermissions(player, item.Key))
                                                            _ = PerformPermissionToRole(ulong.Parse(discordID), ulong.Parse(item.Value));
                                                    }
                                                }
                                            }
                                        }
                                    });
                                    if (Config.ConnectedPlayers.Enabled)
                                        await AddConnectedPlayersRole(ulong.Parse(discordID));

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] There was an error loading the data: {ex.Message}", ConsoleColor.Red);
            }
        }

        public async Task InsertPlayerData(string steamid, string discordid)
        {
            try
            {
                if (await IsPlayerExistsInDatabase(steamid))
                    return;

                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    string sql = "INSERT INTO Discord_Utilities (steamid, discordid) VALUES (@steamid, @discordid)";
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

        private async Task<bool> IsPlayerExistsInDatabase(string steamid)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                string sql = "SELECT COUNT(*) FROM Discord_Utilities WHERE steamid = @steamid";
                using (var cmd = new MySqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@steamid", steamid);
                    var result = await cmd.ExecuteScalarAsync();

                    if (result == null || result == DBNull.Value)
                    {
                        return false;
                    }

                    return Convert.ToInt32(result) > 0;
                }
            }
        }
        private async Task<string> CheckIsPlayerLinked(string discordId)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                string sql = "SELECT steamid FROM Discord_Utilities WHERE discordid = @discordid";
                using (var cmd = new MySqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@discordid", discordId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return reader.GetString(0);
                        }
                        else
                        {
                            return "";
                        }
                    }
                }
            }
        }
    }
}
