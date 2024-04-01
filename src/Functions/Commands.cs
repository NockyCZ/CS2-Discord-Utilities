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

            var linkedPlayers = new Dictionary<ulong, string>();
            var task = Task.Run(async () =>
            {
                linkedPlayers = await GetLinkedPlayers();
            });
            task.Wait();

            if (linkedPlayers.ContainsKey(player.AuthorizedSteamID.SteamId64))
            {
                var discordId = linkedPlayers[player.AuthorizedSteamID.SteamId64];
                var steamId = player.AuthorizedSteamID.SteamId64.ToString();
                _ = RemovePlayerData(steamId);
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

            var linkedPlayers = new Dictionary<ulong, string>();
            var task = Task.Run(async () =>
            {
                linkedPlayers = await GetLinkedPlayers();
            });
            task.Wait();

            if (!linkedPlayers.ContainsKey(player.AuthorizedSteamID.SteamId64))
            {
                var codesList = new Dictionary<string, string>();
                task = Task.Run(async () =>
                {
                    codesList = await GetCodesList();
                });
                task.Wait();

                string code;
                if (codesList.ContainsValue(player.AuthorizedSteamID.SteamId64.ToString()))
                {
                    code = codesList.FirstOrDefault(x => x.Value == player.AuthorizedSteamID.SteamId64.ToString()).Key;
                }
                else
                {
                    code = GetRandomCode(Config.Link.CodeLength);
                    var steamId = player.AuthorizedSteamID.SteamId64.ToString();
                    task = Task.Run(async () =>
                    {
                        await InsertNewCode(steamId, code);
                    });
                    task.Wait();
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

            if (reportCooldowns.ContainsKey(player))
            {
                var remainingTime = (int)Server.CurrentTime - reportCooldowns[player];
                if (remainingTime < Config.Report.ReportCooldown)
                {
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ReportCooldown", Config.Report.ReportCooldown]}");
                    return;
                }
                reportCooldowns.Remove(player);
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

        [ConsoleCommand("css_du_serverstatus", "Perform and setup the Server Status")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void PerformFirstServerStatus_CMD(CCSPlayerController player, CommandInfo info)
        {
            if (Config.ServerStatus.UpdateTimer < 30)
            {
                SendConsoleMessage("[Discord Utilities] You do not have Server Status enabled! The minimum value of Update Time must be more than 30.", ConsoleColor.DarkYellow);
                return;
            }
            var componentsBuilder = new ComponentBuilder();
            bool addComponents = false;
            if (Config.ServerStatus.ServerStatusEmbed.JoinButton.Enabled || Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.Enabled)
            {
                addComponents = true;
                componentsBuilder = GetServerStatusComponents(componentsBuilder);
            }
            _ = PerformFirstServerStatus(componentsBuilder, addComponents);
        }
    }
}