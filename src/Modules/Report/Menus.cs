using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace Report
{
    public partial class Report
    {
        public void OpenReportsList_Menu(CCSPlayerController player)
        {
            if (reportsList.Count == 0)
            {
                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.NoReportsFound"]}");
                return;
            }
            CenterHtmlMenu Menu = new CenterHtmlMenu(Localizer["Menu.ReportsList", reportsList.Count], this);

            foreach (var item in reportsList)
            {
                var data = item.Value;
                if (Config.ReportMethod != 3)
                    Menu.AddMenuOption(Localizer["Menu.ReportInfo", data.targetName, data.reason], (player, option) => OpenReportData_Menu(player, item.Key));
                else
                    Menu.AddMenuOption(Localizer["Menu.ReportInfo", data.senderName, data.reason], (player, option) => OpenReportData_Menu(player, item.Key));
            }

            Menu.Open(player);
        }

        public void OpenReportData_Menu(CCSPlayerController player, string reportId)
        {
            var data = reportsList[reportId];
            CenterHtmlMenu Menu = new CenterHtmlMenu(Localizer["Menu.ReportDetails"], this);
            Menu.PostSelectAction = PostSelectAction.Close;

            Menu.AddMenuOption(Localizer["Menu.MarkAsSolved"], (player, option) => ReportSolved(player, reportId));
            Menu.AddMenuOption(Localizer["Menu.ReportInfo.Sender", data.senderName], null!, true);
            Menu.AddMenuOption(Localizer["Menu.ReportInfo.Reason", data.reason], null!, true);

            if (Config.ReportMethod != 3)
                Menu.AddMenuOption(Localizer["Menu.ReportInfo.Target", data.targetName], null!, true);

            Menu.AddMenuOption(Localizer["Menu.ReportInfo.Time", data.time.ToString(Config.DateFormat)], null!, true);

            Menu.Open(player);
        }

        public void ReportSolved(CCSPlayerController player, string reportId)
        {
            player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ReportSolved"]}");
            if (reportsList.ContainsKey(reportId))
            {
                PerformReportSolved(reportId, 0, player);
            }
        }

        public void OpenReportMenu_Players(CCSPlayerController player)
        {
            if (!Config.SelfReport)
            {
                if (GetTargetsForReportCount(player) == 0)
                {
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ReportNoTargetsFound"]}");
                    return;
                }
            }

            CenterHtmlMenu Menu = new CenterHtmlMenu(Localizer["Menu.ReportSelectPlayer"], this);

            if (Config.SelfReport)
            {
                foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && p.SteamID.ToString().Length == 17))
                    Menu.AddMenuOption(p.PlayerName, (player, target) => OnSelectPlayer_ReportMenu(player, p));
            }
            else
            {
                foreach (var p in Utilities.GetPlayers().Where(p => p != null && p.IsValid && p != player && DiscordUtilities!.IsPlayerDataLoaded(p) && p.Connected == PlayerConnectedState.PlayerConnected && p.SteamID.ToString().Length == 17 && !AdminManager.PlayerHasPermissions(p, Config.UnreportableFlag)))
                    Menu.AddMenuOption(p.PlayerName, (player, target) => OnSelectPlayer_ReportMenu(player, p));
            }

            Menu.Open(player);
        }

        public void OpenReportMenu_Reason(CCSPlayerController player, CCSPlayerController target)
        {
            var selectedTarget = target;
            string[] Reasons = Config.ReportReasons.Split(',');
            var Menu = new CenterHtmlMenu(Localizer["Menu.ReportSelectReason"], this);
            foreach (var reason in Reasons)
            {
                if (reason.Contains("#CUSTOMREASON"))
                    Menu.AddMenuOption(Localizer["Menu.ReportCustomReason"], (player, target) => CustomReasonReport(player, selectedTarget));
                else
                    Menu.AddMenuOption(reason, (player, target) => SendReport(player, selectedTarget, reason));
            }
            Menu.PostSelectAction = PostSelectAction.Close;
            Menu.Open(player);
        }

        private void OnSelectPlayer_ReportMenu(CCSPlayerController player, CCSPlayerController target)
        {
            if (Config.AntiSpamReport && solvedPlayers.Contains(target.SteamID))
            {
                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ThisPlayerCannotBeReported", target.PlayerName]}");
                return;
            }
            OpenReportMenu_Reason(player, target);
        }
    }
}