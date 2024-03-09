using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Admin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public Dictionary<string, string> PermissionsToRoles = new();
        public Dictionary<string, string> RolesToPermissions = new();
        public void LoadManageRolesAndFlags()
        {
            string filePath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/DiscordUtilities/DiscordUtilities.json";
            if (File.Exists(filePath))
            {
                var jsonData = File.ReadAllText(filePath);
                dynamic jsonObj = JsonConvert.DeserializeObject(jsonData)!;

                JObject roleToPermission = jsonObj!["Manage Roles and Permissions"]["Role To Permission"];
                RolesToPermissions = roleToPermission.ToObject<Dictionary<string, string>>()!;

                JObject permissionToRole = jsonObj!["Manage Roles and Permissions"]["Permission To Role"];
                PermissionsToRoles = permissionToRole.ToObject<Dictionary<string, string>>()!;
            }
        }
        public async Task PerformPermissionToRole(ulong discordid, ulong roleid)
        {
            if (PermissionsToRoles.Count() == 0)
                return;

            try
            {
                var guild = BotClient!.GetGuild(ulong.Parse(Config.ServerID));
                if (guild == null)
                {
                    SendConsoleMessage($"[Discord Utilities] Guild with id '{Config.ServerID}' was not found!", ConsoleColor.Red);
                    return;
                }

                var user = guild.GetUser(discordid);
                if (user == null)
                    return;

                var role = guild.GetRole(roleid);
                if (role == null)
                {
                    SendConsoleMessage($"[Discord Utilities] Role with id '{roleid}' was not found (Permission To Role)!", ConsoleColor.Red);
                    return;
                }
                if (!user.Roles.Any(id => id == role))
                {
                    await user.AddRoleAsync(role);
                }
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while getting players role in Role To Permission: {ex.Message}", ConsoleColor.Red);
            }

            return;
        }
        public Task PerformRoleToPermission(ulong discordid, ulong steamid, ulong roleid, string permission)
        {
            try
            {
                var guild = BotClient!.GetGuild(ulong.Parse(Config.ServerID));
                if (guild == null)
                {
                    SendConsoleMessage($"[Discord Utilities] Guild with id '{Config.ServerID}' was not found!", ConsoleColor.Red);
                    return Task.CompletedTask;
                }

                var user = guild.GetUser(discordid);
                if (user == null)
                    return Task.CompletedTask;
                var role = guild.GetRole(roleid);
                if (role == null)
                {
                    SendConsoleMessage($"[Discord Utilities] Role with id '{roleid}' was not found (Role To Permission)!", ConsoleColor.Red);
                    return Task.CompletedTask;
                }
                if (user.Roles.Any(id => id == role))
                {
                    Server.NextFrame(() =>
                    {
                        var player = GetTargetBySteamID64(steamid);
                        if (player == null || !player.IsValid)
                            return;

                        if (permission.StartsWith('@'))
                            AdminManager.AddPlayerPermissions(player, permission);
                        else
                            AdminManager.AddPlayerToGroup(player, permission);
                    });
                }
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while getting players role in Role To Permission: {ex.Message}", ConsoleColor.Red);
            }

            return Task.CompletedTask;
        }
    }
}