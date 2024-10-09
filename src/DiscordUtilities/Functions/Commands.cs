using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Discord;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        [ConsoleCommand("css_du_who", "Get Player Data")]
        [CommandHelper(1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@du/who", "@du/admin")]
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
            var flags = data == null ? "None" : string.Join(", ", data.GetAllFlags());
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
                    cmdOverrides.Append("None");
                }
            }
            else
            {
                cmdOverrides.Append("None");
            }

            UserData? user = null;
            if (linkedPlayers.TryGetValue(target.AuthorizedSteamID!.SteamId64, out var userId))
                user = GetUserDataByUserID(userId);

            info.ReplyToCommand("========================================");
            info.ReplyToCommand("Discord Utilities • Player Data");
            info.ReplyToCommand(" ");
            info.ReplyToCommand($"• Player: {target.PlayerName}");
            info.ReplyToCommand($"• Linked: {linkedPlayers.ContainsKey(target.AuthorizedSteamID!.SteamId64)}");
            if (user != null)
            {
                info.ReplyToCommand($"• Discord User: {user.DisplayName}");
                info.ReplyToCommand($"• Discord User ID: {userId}");
            }
            info.ReplyToCommand($"• Flags: {flags}");
            info.ReplyToCommand($"• Command Overrides: {cmdOverrides}");
            info.ReplyToCommand($"• Immunity: {immunity}");
            info.ReplyToCommand("========================================");
        }

        [ConsoleCommand("css_du_link", "Link Players Manually")]
        [CommandHelper(2, usage: "<steamid64> <discord_userid>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@du/link", "@du/admin")]
        public void LinkPlayer_CMD(CCSPlayerController player, CommandInfo info)
        {
            var targetSteamId = info.GetArg(1);
            if (string.IsNullOrEmpty(targetSteamId) || !ulong.TryParse(targetSteamId, out var steamId))
            {
                info.ReplyToCommand($"[Discord Utilities] '{targetSteamId}' is not a valid SteamID!");
                return;
            }

            var targetUserId = info.GetArg(2);
            if (string.IsNullOrEmpty(targetUserId) || !ulong.TryParse(targetUserId, out var userId))
            {
                info.ReplyToCommand($"[Discord Utilities] '{targetUserId}' is not a valid Discord ID!");
                return;
            }

            if (!linkedPlayers.ContainsKey(steamId))
            {
                var guild = BotClient!.GetGuild(ulong.Parse(ServerId));
                if (guild == null)
                {
                    info.ReplyToCommand($"[Discord Utilities] Guild with id '{ServerId}' was not found!");
                    return;
                }

                var user = guild.GetUser(userId);
                if (user != null)
                {
                    _ = InsertPlayerData(steamId.ToString(), userId.ToString(), user.DisplayName);
                    _ = PerformLinkRole(user, guild.GetRole(ulong.Parse(Config.Link.LinkDiscordSettings.LinkRole)));
                    _ = CreateScheduledEventAsync("refreshlinkedplayers");
                    info.ReplyToCommand($"[Discord Utilities] Player '{steamId}' has been linked with the Discord User: '{user.DisplayName}' ({userId})");
                }
                else
                {
                    info.ReplyToCommand($"[Discord Utilities] User with the Discord ID '{userId}' was not found!");
                }
            }
            else
            {
                info.ReplyToCommand($"[Discord Utilities] SteamID '{steamId}' is already linked! (Discord User ID: {linkedPlayers[steamId]})");
            }
        }

        [ConsoleCommand("css_du_unlink", "Unlink Players Manually")]
        [CommandHelper(1, usage: "<steamid64>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@du/unlink", "@du/admin")]
        public void UnlinkPlayer_CMD(CCSPlayerController player, CommandInfo info)
        {
            var targetSteamId = info.GetArg(1);
            if (string.IsNullOrEmpty(targetSteamId) || !ulong.TryParse(targetSteamId, out var steamId))
            {
                info.ReplyToCommand($"[Discord Utilities] '{targetSteamId}' is not a valid SteamID!");
                return;
            }

            if (linkedPlayers.ContainsKey(steamId))
            {
                var userId = linkedPlayers[steamId];
                var user = GetUserDataByUserID(userId);
                if (user != null)
                    info.ReplyToCommand($"[Discord Utilities] Player '{steamId}' has been unlinked from the Discord User: '{user.DisplayName}' ({userId})");
                else
                    info.ReplyToCommand($"[Discord Utilities] Player '{steamId}' has been unlinked from the Discord User: '{userId}'");

                _ = RemovePlayerData(steamId.ToString());
                _ = RemoveLinkRole(userId);
                _ = CreateScheduledEventAsync("refreshlinkedplayers");
            }
            else
            {
                info.ReplyToCommand($"[Discord Utilities] SteamID '{steamId}' is not linked!");
            }
        }

        [ConsoleCommand("css_du_addtimedrole", "Add Timed Role to User")]
        [CommandHelper(3, usage: "<discord_userid> <role_id> <minutes>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void AddTimedRole_CMD(CCSPlayerController player, CommandInfo info)
        {
            if (!Config.TimedRoles)
            {
                info.ReplyToCommand($"[Discord Utilities] Timed Roles are disabled on this server!");
                info.ReplyToCommand($"[Discord Utilities] Use the command on a server where this feature is enabled.");
                return;
            }

            var targetUserId = info.GetArg(1);
            if (string.IsNullOrEmpty(targetUserId) || !ulong.TryParse(targetUserId, out var userId))
            {
                info.ReplyToCommand($"[Discord Utilities] '{targetUserId}' is not a valid User ID!");
                return;
            }

            var targetRoleId = info.GetArg(2);
            if (string.IsNullOrEmpty(targetRoleId) || !ulong.TryParse(targetRoleId, out var roleId))
            {
                info.ReplyToCommand($"[Discord Utilities] '{targetRoleId}' is not a valid Role ID!");
                return;
            }

            var targetMinutes = info.GetArg(3);
            if (string.IsNullOrEmpty(targetMinutes) || !int.TryParse(targetMinutes, out int minutes))
            {
                info.ReplyToCommand($"[Discord Utilities] '{targetMinutes}' is not a valid minutes!");
                return;
            }

            var guild = BotClient!.GetGuild(ulong.Parse(ServerId));
            if (guild == null)
            {
                info.ReplyToCommand($"[Discord Utilities] Guild with id '{ServerId}' was not found!");
                return;
            }

            var user = guild.GetUser(userId);
            if (user != null)
            {
                var role = guild.GetRole(roleId);
                if (role == null)
                {
                    info.ReplyToCommand($"[Discord Utilities] Role with id '{roleId}' was not found!");
                    return;
                }

                var endTime = DateTime.Now.AddMinutes(minutes);
                SetupAddNewTimedRole(user, role, endTime);
                _ = user.AddRoleAsync(role);
            }
            else
                info.ReplyToCommand($"[Discord Utilities] User with ID '{userId}' was not found on your Discord Server!");
        }

        [ConsoleCommand("css_du_addrole", "Add Role to User")]
        [CommandHelper(2, usage: "<discord_userid> <role_id>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@du/addrole", "@du/admin")]
        public void AddRole_CMD(CCSPlayerController player, CommandInfo info)
        {
            var targetUserId = info.GetArg(1);
            if (string.IsNullOrEmpty(targetUserId) || !ulong.TryParse(targetUserId, out var userId))
            {
                info.ReplyToCommand($"[Discord Utilities] '{targetUserId}' is not a valid User ID!");
                return;
            }

            var targetRoleId = info.GetArg(1);
            if (string.IsNullOrEmpty(targetRoleId) || !ulong.TryParse(targetRoleId, out var roleId))
            {
                info.ReplyToCommand($"[Discord Utilities] '{targetRoleId}' is not a valid Role ID!");
                return;
            }

            var guild = BotClient!.GetGuild(ulong.Parse(ServerId));
            if (guild == null)
            {
                info.ReplyToCommand($"[Discord Utilities] Guild with id '{ServerId}' was not found!");
                return;
            }

            var user = guild.GetUser(userId);
            if (user != null)
            {
                var role = guild.GetRole(roleId);
                if (role == null)
                {
                    info.ReplyToCommand($"[Discord Utilities] Role with id '{roleId}' was not found!");
                    return;
                }
                _ = user.AddRoleAsync(role);
                info.ReplyToCommand($"[Discord Utilities] User {user.DisplayName} ({userId}) has been assigned the role {role.Name}");
            }
            else
                info.ReplyToCommand($"[Discord Utilities] User with ID '{userId}' was not found on your Discord Server!");
        }

        [ConsoleCommand("css_du_removerole", "Remove Role from User")]
        [CommandHelper(2, usage: "<discord_userid> <role_id>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@du/removerole", "@du/admin")]
        public void RemoveRole_CMD(CCSPlayerController player, CommandInfo info)
        {
            var targetUserId = info.GetArg(1);
            if (string.IsNullOrEmpty(targetUserId) || !ulong.TryParse(targetUserId, out var userId))
            {
                info.ReplyToCommand($"[Discord Utilities] '{targetUserId}' is not a valid User ID!");
                return;
            }

            var targetRoleId = info.GetArg(1);
            if (string.IsNullOrEmpty(targetRoleId) || !ulong.TryParse(targetRoleId, out var roleId))
            {
                info.ReplyToCommand($"[Discord Utilities] '{targetRoleId}' is not a valid Role ID!");
                return;
            }

            var guild = BotClient!.GetGuild(ulong.Parse(ServerId));
            if (guild == null)
            {
                info.ReplyToCommand($"[Discord Utilities] Guild with id '{ServerId}' was not found!");
                return;
            }

            var user = guild.GetUser(userId);
            if (user != null)
            {
                var role = guild.GetRole(roleId);
                if (role == null)
                {
                    info.ReplyToCommand($"[Discord Utilities] Role with id '{roleId}' was not found!");
                    return;
                }
                _ = user.RemoveRoleAsync(role);
                info.ReplyToCommand($"[Discord Utilities] User {user.DisplayName} ({userId}) has been removed from the {role.Name} role");
            }
            else
                info.ReplyToCommand($"[Discord Utilities] User with ID '{userId}' was not found on your Discord Server!");
        }

        private void CreateCustomCommands()
        {
            if (Config.Link.Enabled)
            {
                var LinkCmds = Config.Link.LinkIngameSettings.LinkCommands;
                foreach (var cmd in LinkCmds)
                    AddCommand($"css_{cmd}", $"Discord Link Command ({cmd})", LinkProfile_CMD);

                var UnlinkCmds = Config.Link.LinkIngameSettings.UnlinkCommands;
                foreach (var cmd in UnlinkCmds)
                    AddCommand($"css_{cmd}", $"Discord Unlink Command ({cmd})", UnlinkProfile_CMD);
            }
        }

        public void UnlinkProfile_CMD(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid || player.AuthorizedSteamID == null)
                return;

            if (!IsDbConnected)
            {
                player.PrintToChat("Database is not connected! Contact the Administrator.");
                return;
            }
            if (!IsBotConnected)
            {
                player.PrintToChat("Discord BOT is not connected! Contact the Administrator.");
                return;
            }

            if (linkedPlayers.ContainsKey(player.AuthorizedSteamID.SteamId64))
            {
                var discordId = linkedPlayers[player.AuthorizedSteamID.SteamId64];
                var steamId = player.AuthorizedSteamID.SteamId64.ToString();
                _ = RemovePlayerData(steamId);
                _ = RemoveLinkRole(discordId);
                _ = CreateScheduledEventAsync("refreshlinkedplayers");
                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AccountUnliked"]}");
            }
            else
            {
                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.NotLinked"]}");
            }
        }
        public void LinkProfile_CMD(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid || player.AuthorizedSteamID == null)
                return;

            if (!IsDbConnected)
            {
                player.PrintToChat("Database is not connected! Contact the Administrator.");
                return;
            }
            if (!IsBotConnected)
            {
                player.PrintToChat("Discord BOT is not connected! Contact the Administrator.");
                return;
            }

            if (!linkedPlayers.ContainsKey(player.AuthorizedSteamID.SteamId64))
            {
                string code;
                if (linkCodes.ContainsValue(player.AuthorizedSteamID.SteamId64.ToString()))
                {
                    code = linkCodes.FirstOrDefault(x => x.Value == player.AuthorizedSteamID.SteamId64.ToString()).Key;
                }
                else
                {
                    code = GetRandomCode(Config.Link.CodeLength);
                    var steamId = player.AuthorizedSteamID.SteamId64.ToString();
                    _ = CreateScheduledEventAsync($"addcode;{EncodeSecretString(code)};{steamId}");
                }

                string localizedMessage = Localizer["Chat.LinkAccount", code];
                string[] linkMessage = localizedMessage.Split('\n');
                foreach (var msg in linkMessage)
                {
                    player.PrintToChat(msg);
                }
            }
            else
            {
                string localizedMessage = Localizer["Chat.AlreadyLinked"];
                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AlreadyLinked"]}");
            }
        }
    }
}