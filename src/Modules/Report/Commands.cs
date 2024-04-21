using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace Report
{
    public partial class Report
    {
        private void CreateCustomCommands()
        {
            string[] Commands = Config.ReportCommands.Split(',');
            foreach (var cmd in Commands)
                AddCommand($"css_{cmd}", $"Report Players ({cmd})", ReportPlayers_CMD);
        }

        public void ReportPlayers_CMD(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid)
                return;

            if (DiscordUtilities == null || !DiscordUtilities.IsBotLoaded())
            {
                player.PrintToChat("Discord BOT is not connected! Contact the Administrator.");
                return;
            }

            if (reportCooldowns.ContainsKey(player))
            {
                var remainingTime = (int)Server.CurrentTime - reportCooldowns[player];
                if (remainingTime < Config.ReportCooldown)
                {
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ReportCooldown", Config.ReportCooldown]}");
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
            switch (Config.ReportMethod)
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
    }
}