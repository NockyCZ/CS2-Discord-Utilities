
using System.Drawing;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;
using Newtonsoft.Json;

namespace ManageRolesAndPermissions
{
    public class ManageRolesAndPermissions : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "[Discord Utilities] Manage Roles and Permissions";
        public override string ModuleAuthor => "SourceFactory.eu";
        public override string ModuleVersion => "1.3";
        private IDiscordUtilitiesAPI? DiscordUtilities { get; set; }
        public Config Config { get; set; } = new();
        public void OnConfigParsed(Config config) { Config = config; }
        public Dictionary<string, string> PermissionsToRoles = new();
        public Dictionary<string, RoleGroupData> RolesToPermissions = new();

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
        }
        public override void Unload(bool hotReload)
        {
            GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers -= DiscordUtilitiesEventHandler;
        }
        private void OnBotLoaded()
        {
            LoadManageRolesAndFlags();
        }

        [ConsoleCommand("css_du_who", "Get Player Admin Data")]
        [CommandHelper(1, usage: "<#userid or name>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void GetPlayerData_CMD(CCSPlayerController player, CommandInfo info)
        {
            var targets = GetTarget(info);
            if (targets == null)
                return;

            var playersList = targets.Players.Where(p => p.IsValid && p != null && !p.IsHLTV && !p.IsBot).ToList();
            if (playersList.Count == 0)
                return;

            var target = playersList.FirstOrDefault();
            if (target == null)
                return;

            var data = AdminManager.GetPlayerAdminData(target);
            var flags = data == null ? "none" : string.Join(", ", data.GetAllFlags());
            var immunity = data == null ? 0 : data.Immunity;

            StringBuilder cmdOverrides = new();
            if (data != null)
            {
                if (data.CommandOverrides.Count > 0)
                {
                    int count = 0;
                    foreach (var cmd in data.CommandOverrides)
                    {
                        if (count > 0)
                            cmdOverrides.Append($", {cmd.Key} ({cmd.Value})");
                        else
                            cmdOverrides.Append($"{cmd.Key} ({cmd.Value})");
                        count++;
                    }
                }
                else
                {
                    cmdOverrides.Append("none");
                }
            }
            else
            {
                cmdOverrides.Append("none");
            }

            info.ReplyToCommand("========================================");
            info.ReplyToCommand("Discord Utilities • Player Data");
            info.ReplyToCommand(" ");
            info.ReplyToCommand($"• Player: {target.PlayerName}");
            info.ReplyToCommand($"• Flags: {flags}");
            info.ReplyToCommand($"• Command Overrides: {cmdOverrides}");
            info.ReplyToCommand($"• Immunity: {immunity}");
            info.ReplyToCommand("========================================");
        }

        private static TargetResult? GetTarget(CommandInfo info)
        {
            var matches = info.GetArgTargetResult(1);
            if (!matches.Any())
            {
                info.ReplyToCommand($"Target {info.GetArg(1)} was not found.");
                return null;
            }

            if (info.GetArg(1).StartsWith('@'))
                return matches;

            if (matches.Count() == 1)
                return matches;

            info.ReplyToCommand($"Multiple targets found for \"{info.GetArg(1)}\".");
            return null;
        }

        public void LoadManageRolesAndFlags()
        {
            PermissionsToRoles.Clear();
            RolesToPermissions.Clear();

            string filePath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/DU_ManageRolesAndPermissions/DU_ManageRolesAndPermissions.json";
            if (File.Exists(filePath))
            {
                try
                {
                    var jsonData = File.ReadAllText(filePath);
                    dynamic deserializedJson = JsonConvert.DeserializeObject(jsonData)!;

                    var roleToPermission = deserializedJson["Role To Permission"].ToObject<Dictionary<string, RoleGroupData>>();
                    if (roleToPermission != null)
                        RolesToPermissions = roleToPermission;

                    var permissionToRole = deserializedJson["Permission To Role"].ToObject<Dictionary<string, string>>();
                    if (permissionToRole != null)
                        PermissionsToRoles = permissionToRole;

                    if (DiscordUtilities != null && DiscordUtilities.Debug())
                    {
                        DiscordUtilities.SendConsoleMessage($"A total of '{PermissionsToRoles.Count()}' Permissions To Roles have been loaded", MessageType.Debug);
                        DiscordUtilities.SendConsoleMessage($"A total of '{RolesToPermissions.Count()}' Roles To Permissions Roles have been loaded", MessageType.Debug);
                    }
                }
                catch (Exception ex)
                {
                    DiscordUtilities!.SendConsoleMessage($"An error occurred while loading the Manage Roles and Permissions configuration: '{ex.Message}'", MessageType.Error);
                    throw new Exception($"An error occurred while loading the Manage Roles and Permissions configuration: {ex.Message}");
                }
            }
        }

        private void DiscordUtilitiesEventHandler(object? _, IDiscordUtilitiesEvent @event)
        {
            switch (@event)
            {
                case LinkedUserDataLoaded linkedUser:
                    OnLinkedUserDataLoaded(linkedUser.User, linkedUser.player);
                    break;
                case BotLoaded:
                    OnBotLoaded();
                    break;
                default:
                    break;
            }
        }

        private void OnLinkedUserDataLoaded(UserData user, CCSPlayerController player)
        {
            if (RolesToPermissions.Count > 0)
            {
                var groupData = RolesToPermissions.Where(x => user.RolesIds.Contains(ulong.Parse(x.Key))).FirstOrDefault();
                if (groupData.Key != null)
                {
                    var roleGroupData = groupData.Value;
                    if (roleGroupData.flags.Count > 0)
                    {
                        StringBuilder sb = new();
                        int count = 0;
                        foreach (var flag in roleGroupData.flags)
                        {
                            if (!AdminManager.PlayerHasPermissions(player.AuthorizedSteamID, flag))
                            {
                                if (count > 0)
                                    sb.Append($", '{flag}'");
                                else
                                    sb.Append($"'{flag}'");

                                AdminManager.AddPlayerPermissions(player.AuthorizedSteamID, flag);
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
                    AdminManager.SetPlayerImmunity(player.AuthorizedSteamID, roleGroupData.immunity);

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
                        DiscordUtilities!.SendConsoleMessage($"Invalid permission '{item.Key}'!", MessageType.Error);
                    }
                }
            }

            if (Config.removeRolesOnPermissionLoss && rolesToRemove.Count() > 0)
                PerformRemoveRole(user, rolesToRemove);
            if (rolesList.Count() > 0)
                PerformPermissionToRole(user, rolesList);
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