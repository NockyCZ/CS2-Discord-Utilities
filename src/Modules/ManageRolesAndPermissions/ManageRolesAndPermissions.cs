
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;

namespace ManageRolesAndPermissions
{
    public class ManageRolesAndPermissions : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "[Discord Utilities] Manage Roles and Permissions";
        public override string ModuleAuthor => "SourceFactory.eu";
        public override string ModuleVersion => "1.5";
        private IDiscordUtilitiesAPI? DiscordUtilities { get; set; }
        public Config Config { get; set; } = new();
        public void OnConfigParsed(Config config) { Config = config; }

        public class RoleGroupData
        {
            public List<string> flags { get; set; } = new();
            public uint immunity { get; set; } = 0;
            public Dictionary<string, bool> command_overrides { get; set; } = new();
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers += DiscordUtilitiesEventHandler;
            DiscordUtilities!.CheckVersion(ModuleName, ModuleVersion);
            if (DiscordUtilities.Debug())
            {
                DiscordUtilities.SendConsoleMessage($"A total of '{Config.PermissionToRole.Count()}' Permissions To Roles have been loaded", MessageType.Debug);
                DiscordUtilities.SendConsoleMessage($"A total of '{Config.RoleToPermission.Count()}' Roles To Permissions Roles have been loaded", MessageType.Debug);
            }
        }
        public override void Unload(bool hotReload)
        {
            GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers -= DiscordUtilitiesEventHandler;
        }

        private void DiscordUtilitiesEventHandler(object? _, IDiscordUtilitiesEvent @event)
        {
            switch (@event)
            {
                case LinkedUserDataLoaded data:
                    OnLinkedUserDataLoaded(data.User, data.player);
                    break;
                case LinkedUserRolesUpdated data:
                    OnLinkedUserRolesUpdated(data.User, data.removedRoles);
                    break;
                default:
                    break;
            }
        }

        private void OnLinkedUserRolesUpdated(UserData user, List<string>? removedRoles)
        {
            if (!Config.removePermissionsOnRoleLoss || removedRoles == null || removedRoles.Count == 0)
                return;

            var steamId = DiscordUtilities!.GetLinkedPlayers().FirstOrDefault(x => x.Value == user.ID).Key;
            var player = Utilities.GetPlayerFromSteamId(steamId);

            if (player == null || !player.IsValid)
                return;

            var permsToRemoveByRole = new List<string>();
            foreach (var role in removedRoles)
            {
                if (Config.RoleToPermission.ContainsKey(role))
                    permsToRemoveByRole.Add(role);
            }

            if (permsToRemoveByRole.Count == 0)
                return;

            foreach (var role in permsToRemoveByRole)
            {
                if (Config.RoleToPermission.TryGetValue(role, out var data))
                {
                    AdminManager.RemovePlayerPermissions(player, data.flags.ToArray());
                    foreach (var cmdOverride in data.command_overrides)
                    {
                        AdminManager.SetPlayerCommandOverride(player, cmdOverride.Key, !cmdOverride.Value);
                    }
                }
            }
            AdminManager.SetPlayerImmunity(player, 0);
        }

        private void OnLinkedUserDataLoaded(UserData user, CCSPlayerController player)
        {
            if (Config.RoleToPermission.Count > 0)
            {
                List<string> flags = new();
                uint? maxImmunity = new();

                user.RolesIds.ForEach(roleID =>
                {
                    if (Config.RoleToPermission.TryGetValue(roleID.ToString(), out var roleGroupData))
                    {
                        if (roleGroupData != null)
                        {
                            if (!maxImmunity.HasValue)
                                maxImmunity = roleGroupData.immunity;
                            else
                                maxImmunity = roleGroupData.immunity > maxImmunity ? roleGroupData.immunity : maxImmunity;

                            if (roleGroupData.flags.Count > 0)
                            {
                                StringBuilder sb = new();
                                int count = 0;
                                foreach (var flag in roleGroupData.flags)
                                {
                                    if (!AdminManager.PlayerHasPermissions(player.AuthorizedSteamID, flag))
                                    {
                                        flags.Add(flag);
                                        if (count > 0)
                                            sb.Append($", '{flag}'");
                                        else
                                            sb.Append($"'{flag}'");
                                        count++;
                                    }
                                }
                                if (DiscordUtilities!.Debug())
                                    DiscordUtilities.SendConsoleMessage($"Flags {sb} has been added to player '{player.PlayerName}'", MessageType.Debug);
                            }
                            if (roleGroupData.command_overrides.Count > 0)
                            {
                                foreach (var cmd in roleGroupData.command_overrides)
                                {
                                    AdminManager.SetPlayerCommandOverride(player.AuthorizedSteamID, cmd.Key, cmd.Value);
                                }
                            }
                        }
                    }
                });
                if (maxImmunity.HasValue)
                    AdminManager.SetPlayerImmunity(player, maxImmunity.Value);
                if (flags.Count > 0)
                    AdminManager.AddPlayerPermissions(player.AuthorizedSteamID, [.. flags]);
            }

            if (Config.PermissionToRole.Count != 0)
            {
                var rolesList = new List<string>();
                var rolesToRemove = new List<string>();

                foreach (var item in Config.PermissionToRole)
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
                        DiscordUtilities!.SendConsoleMessage($"Invalid permission '{item.Key}'!", MessageType.Error);
                    }
                }

                if (Config.removeRolesOnPermissionLoss && rolesToRemove.Count() > 0)
                    PerformRemoveRole(user, rolesToRemove);
                if (rolesList.Count() > 0)
                    PerformPermissionToRole(user, rolesList);
            }
        }

        public void PerformPermissionToRole(UserData user, List<string> rolesIds)
        {
            DiscordUtilities!.AddRolesToUser(user.ID, rolesIds);
        }

        public void PerformRemoveRole(UserData user, List<string> rolesIds)
        {
            DiscordUtilities!.RemoveRolesFromUser(user.ID, rolesIds);
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