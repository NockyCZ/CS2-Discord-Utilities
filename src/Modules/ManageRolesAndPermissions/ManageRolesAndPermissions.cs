
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ManageRolesAndPermissions
{
    public class ManageRolesAndPermissions : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "[Discord Utilities] Manage Roles and Permissions";
        public override string ModuleAuthor => "SourceFactory.eu";
        public override string ModuleVersion => "1.0.0";
        private IDiscordUtilitiesAPI? DiscordUtilities { get; set; }
        public Config Config { get; set; } = null!;
        public void OnConfigParsed(Config config) { Config = config; }
        public Dictionary<string, string> PermissionsToRoles = new();
        public Dictionary<string, string> RolesToPermissions = new();
        public override void OnAllPluginsLoaded(bool hotReload)
        {
            GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers += DiscordUtilitiesEventHandler;
        }
        public override void Unload(bool hotReload)
        {
            GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers -= DiscordUtilitiesEventHandler;
        }
        public override void Load(bool hotReload)
        {
            LoadManageRolesAndFlags();
        }
        public void LoadManageRolesAndFlags()
        {
            PermissionsToRoles.Clear();
            RolesToPermissions.Clear();

            string filePath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/DU_ManageRolesAndPermissions/DU_ManageRolesAndPermissions.json";
            if (File.Exists(filePath))
            {
                var jsonData = File.ReadAllText(filePath);
                dynamic deserializedJson = JsonConvert.DeserializeObject(jsonData)!;

                var roleToPermission = deserializedJson["Role To Permission"].ToObject<Dictionary<string, string>>();
                if (roleToPermission != null)
                    RolesToPermissions = roleToPermission;

                var permissionToRole = deserializedJson["Permission To Role"].ToObject<Dictionary<string, string>>();
                if (permissionToRole != null)
                    PermissionsToRoles = permissionToRole;
            }
        }

        private void DiscordUtilitiesEventHandler(object? _, IDiscordUtilitiesEvent @event)
        {
            switch (@event)
            {
                case LinkedUserDataLoaded linkedUser:
                    OnLinkedUserDataLoaded(linkedUser.User, linkedUser.player);
                    break;
                default:
                    break;
            }
        }

        private void OnLinkedUserDataLoaded(UserData user, CCSPlayerController player)
        {
            var permissionsList = new List<string>();
            if (RolesToPermissions.Count != 0)
            {
                foreach (var item in RolesToPermissions)
                {
                    if (user.RolesIds.Contains(ulong.Parse(item.Key)))
                    {
                        if (item.Value.StartsWith('@'))
                        {
                            if (!AdminManager.PlayerHasPermissions(player, item.Value))
                                permissionsList.Add(item.Value);
                        }
                        else if (item.Value.StartsWith('#'))
                        {
                            if (!AdminManager.PlayerInGroup(player, item.Value))
                                permissionsList.Add(item.Value);
                        }
                        else
                        {
                            DiscordUtilities!.SendConsoleMessage($"[Discord Utilities] Invalid permission '{item.Value}'!", MessageType.Error);
                        }
                    }
                }
            }

            var rolesList = new List<string>();
            var rolesToRemove = new List<string>();
            if (PermissionsToRoles.Count != 0)
            {
                foreach (var item in PermissionsToRoles)
                {
                    if (item.Key.StartsWith('@'))
                    {
                        if (AdminManager.PlayerHasPermissions(player, item.Key))
                        {
                            if (!user.RolesIds.Contains(ulong.Parse(item.Value)))
                                rolesList.Add(item.Value);
                        }
                        else
                        {
                            if (user.RolesIds.Contains(ulong.Parse(item.Value)))
                                rolesToRemove.Add(item.Value);
                        }
                    }
                    else if (item.Key.StartsWith('#'))
                    {
                        if (AdminManager.PlayerInGroup(player, item.Key))
                        {
                            if (!user.RolesIds.Contains(ulong.Parse(item.Value)))
                                rolesList.Add(item.Value);
                        }
                        else
                        {
                            if (user.RolesIds.Contains(ulong.Parse(item.Value)))
                                rolesToRemove.Add(item.Value);
                        }
                    }
                    else
                    {
                        DiscordUtilities!.SendConsoleMessage($"[Discord Utilities] Invalid permission '{item.Key}'!", MessageType.Error);
                    }
                }
                if (Config.removeRolesOnPermissionLoss && rolesToRemove.Count() > 0)
                    PerformRemoveRole(user, rolesToRemove);
                if (rolesList.Count() > 0)
                    PerformPermissionToRole(user, rolesList);
                if (permissionsList.Count() > 0)
                    PerformRoleToPermission(player, permissionsList);
            }
        }

        public void PerformPermissionToRole(UserData user, List<string> rolesIds)
        {
            DiscordUtilities!.AddRolesToUser(user, rolesIds);
        }

        public void PerformRoleToPermission(CCSPlayerController player, List<string> permissions)
        {
            foreach (var perm in permissions)
            {
                if (perm.StartsWith('@'))
                {
                    if (DiscordUtilities!.Debug())
                        DiscordUtilities.SendConsoleMessage($"[Discord Utilities] DEBUG: Flag '{perm}' has been added to player '{player.PlayerName}'", MessageType.Debug);
                    AdminManager.AddPlayerPermissions(player, perm);
                }
                else
                {
                    if (DiscordUtilities!.Debug())
                        DiscordUtilities.SendConsoleMessage($"[Discord Utilities] DEBUG: Group '{perm}' has been added to player '{player.PlayerName}'", MessageType.Debug);
                    AdminManager.AddPlayerToGroup(player, perm);
                }
            }
        }

        public void PerformRemoveRole(UserData user, List<string> rolesIds)
        {
            DiscordUtilities!.RemoveRolesFromUser(user, rolesIds);
        }

        private IDiscordUtilitiesAPI GetDiscordUtilitiesEventSender()
        {
            if (DiscordUtilities is not null)
            {
                return DiscordUtilities;
            }

            var DUApi = new PluginCapability<IDiscordUtilitiesAPI>("discord_utilities").Get();
            if (DUApi is null)
            {
                throw new Exception("Couldn't load Discord Utilities plugin");
            }

            DiscordUtilities = DUApi;
            return DUApi;
        }
    }
}