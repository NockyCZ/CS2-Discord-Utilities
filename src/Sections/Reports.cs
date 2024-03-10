using CounterStrikeSharp.API.Core;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public Dictionary<CCSPlayerController, CCSPlayerController> performReport = new Dictionary<CCSPlayerController, CCSPlayerController>();
        public void SendReport(CCSPlayerController sender, CCSPlayerController target, string reason)
        {
            if (Config.Report.ReportMethod != 3)
            {
                if (target == null || !target.IsValid || target.AuthorizedSteamID == null)
                {
                    sender.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.TargetNotConnected"]}");
                    return;
                }
                if (target == sender)
                {
                    sender.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SelfReport"]}");
                    return;
                }
            }
            string[] data = new string[3];
            data[0] = sender.AuthorizedSteamID!.SteamId64.ToString();
            data[1] = Config.Report.ReportMethod != 3 ? target.AuthorizedSteamID!.SteamId64.ToString() : sender.AuthorizedSteamID!.SteamId64.ToString();
            data[2] = reason;

            var embedBuiler = GetEmbed(EmbedTypes.Report, data);
            var content = GetContent(ContentTypes.Report, data);

            _ = SendDiscordMessage(embedBuiler, content, ulong.Parse(Config.Report.ChannelID), "Report");
            sender.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ReportSend", target.PlayerName, reason]}");
        }

        public void CustomReasonReport(CCSPlayerController sender, CCSPlayerController target)
        {
            if (!performReport.ContainsKey(sender))
            {
                performReport.Add(sender, target);
            }
            sender.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.InserYourReason"]}");
        }
    }
}