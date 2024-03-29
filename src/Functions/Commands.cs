using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Discord;
using Newtonsoft.Json.Linq;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        private void CreateCustomCommands()
        {
            if (Config.Report.Enabled)
            {
                string[] Commands = Config.Report.ReportCommands.Split(',');
                foreach (var cmd in Commands)
                    AddCommand($"css_{cmd}", $"Report Players ({cmd})", ReportPlayers_CMD);
            }
            if (Config.Link.Enabled)
            {
                string[] LinkCmds = Config.Link.IngameLinkCommands.Split(',');
                foreach (var cmd in LinkCmds)
                    AddCommand($"css_{cmd}", $"Discord Link Command ({cmd})", LinkProfile_CMD);

                string[] UnlinkCmds = Config.Link.IngameUnlinkCommands.Split(',');
                foreach (var cmd in UnlinkCmds)
                    AddCommand($"css_{cmd}", $"Discord Unink Command ({cmd})", UnlinkProfile_CMD);
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
                linkedPlayers.Remove(player.AuthorizedSteamID.SteamId64);
                _ = RemovePlayerData(player.AuthorizedSteamID.SteamId64.ToString());
                _ = RemoveLinkRole(discordId);
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
                if (linkCodes.ContainsValue(player.AuthorizedSteamID.SteamId64))
                {
                    code = linkCodes.FirstOrDefault(x => x.Value == player.AuthorizedSteamID.SteamId64).Key;
                }
                else
                {
                    code = GetRandomCode(Config.Link.CodeLength);
                    linkCodes.Add(code, player.AuthorizedSteamID.SteamId64);
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
        public void ReportPlayers_CMD(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid)
                return;

            if (!IsBotConnected)
            {
                player.PrintToChat("Discord BOT is not connected! Contact the Administrator.");
                return;
            }

            if (GetTargetsForReportCount(player) == 0)
            {
                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ReportNoTargetsFound"]}");
                return;
            }
            var arg1 = info.GetArg(1);
            var arg2 = info.GetArg(2);
            switch (Config.Report.ReportMethod)
            {
                case 1:
                    OpenReportMenu_Players(player);
                    break;
                case 2:
                    if (string.IsNullOrEmpty(arg1) || string.IsNullOrEmpty(arg2))
                    {
                        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.InvalidReportUsage"]}");
                        return;
                    }
                    if (GetTargetByName(arg1, player) is var target && target != null)
                    {
                        if (target == player)
                        {
                            player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SelfReport"]}");
                            return;
                        }
                        SendReport(player, target, arg2);
                    }
                    break;
                case 3:
                    if (string.IsNullOrEmpty(arg1))
                    {
                        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.InvalidReportUsage"]}");
                        return;
                    }
                    SendReport(player, null!, arg1);
                    break;
                default:
                    if (string.IsNullOrEmpty(arg1))
                    {
                        OpenReportMenu_Players(player);
                        return;
                    }
                    target = GetTargetByName(arg1, player);
                    if (!string.IsNullOrEmpty(arg1) && string.IsNullOrEmpty(arg2))
                    {
                        if (target != null)
                        {
                            if (target == player)
                            {
                                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SelfReport"]}");
                                return;
                            }
                            OpenReportMenu_Reason(player, target);
                        }
                    }
                    else if (!string.IsNullOrEmpty(arg1) && !string.IsNullOrEmpty(arg2))
                    {
                        if (target != null)
                        {
                            if (target == player)
                            {
                                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SelfReport"]}");
                                return;
                            }
                            SendReport(player, target, arg2);
                        }
                    }
                    break;
            }
        }

        [ConsoleCommand("css_du_updatedatabase", "Update Database to the latest version")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void UpdateDatabase_CMD(CCSPlayerController player, CommandInfo info)
        {
            if (!IsDbConnected)
            {
                SendConsoleMessage("[Discord Utilities] Database is not connected!", ConsoleColor.DarkRed);
                return;
            }
            _ = UpdateDatabase();
        }

        [ConsoleCommand("css_du_serverstatus", "Perform and setup the Server Status")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void PerformFirstServerStatus_CMD(CCSPlayerController player, CommandInfo info)
        {
            if (Config.ServerStatus.UpdateTimer < 30)
            {
                SendConsoleMessage("[Discord Utilities] You do not have Server Status enabled! The minimum value of Update Time must be more than 30.", ConsoleColor.DarkYellow);
                return;
            }

            int totalMenuPlayers = 0;
            var componentsBuilder = new ComponentBuilder();
            if (playerData.Count() > 0 && Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.Enabled)
            {
                var menuBuilder = new SelectMenuBuilder()
                    .WithPlaceholder(Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.MenuName)
                    .WithCustomId("serverstatus-players")
                    .WithMinValues(1)
                    .WithMaxValues(1);

                foreach (var p in playerData!)
                {
                    if (p.Key == null || !p.Key.IsValid || p.Key.AuthorizedSteamID == null)
                        continue;

                    string replacedLabel = ReplacePlayerDataVariables(Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.PlayersFormat, p.Key.AuthorizedSteamID.SteamId64);
                    menuBuilder.AddOption(label: replacedLabel, value: p.Key.AuthorizedSteamID.SteamId64.ToString());
                    totalMenuPlayers++;
                }
                componentsBuilder.WithSelectMenu(menuBuilder);
            }
            _ = PerformFirstServerStatus(componentsBuilder, totalMenuPlayers);
        }
    }
}