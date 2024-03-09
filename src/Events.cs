using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid || player.AuthorizedSteamID == null)
                return HookResult.Continue;

            if (performReport.ContainsKey(player))
            {
                if (info.GetArg(1).Length < Config.Report.ReasonLength)
                {
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer[name: "Chat.ReportShortReason"]}");
                    return HookResult.Handled;
                }
                if (info.GetArg(1).Contains(Config.Report.CancelCommand))
                {
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ReportCancelled"]}");
                    performReport.Remove(player);
                    return HookResult.Handled;
                }
                SendReport(player, performReport[player], info.GetArg(1));
                performReport.Remove(player);
                return HookResult.Handled;
            }
            if (Config.Chatlog.Enabled)
            {
                string msg = info.GetArg(1);
                string[] blockedWords = Config.Chatlog.BlockedWords.Split(',');
                foreach (var word in blockedWords)
                {
                    if (msg.Contains(word))
                        msg = msg.Replace(word, "");
                }
                if (!string.IsNullOrEmpty(msg))
                    PerformChatlog(player.AuthorizedSteamID.SteamId64, msg, player.TeamNum, false);
            }

            return HookResult.Continue;
        }
        private HookResult OnPlayerSayTeam(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid || player.AuthorizedSteamID == null)
                return HookResult.Continue;

            if (performReport.ContainsKey(player))
            {
                if (info.GetArg(1).Length < Config.Report.ReasonLength)
                {
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer[name: "Chat.ReportShortReason"]}");
                    return HookResult.Handled;
                }
                if (info.GetArg(1).Contains(Config.Report.CancelCommand))
                {
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ReportCancelled"]}");
                    performReport.Remove(player);
                    return HookResult.Handled;
                }
                SendReport(player, performReport[player], info.GetArg(1));
                performReport.Remove(player);
                return HookResult.Handled;
            }
            if (Config.Chatlog.Enabled)
            {
                string msg = info.GetArg(1);
                string[] blockedWords = Config.Chatlog.BlockedWords.Split(',');
                foreach (var word in blockedWords)
                {
                    if (msg.Contains(word))
                        msg = msg.Replace(word, "");
                }
                if (!string.IsNullOrEmpty(msg))
                    PerformChatlog(player.AuthorizedSteamID.SteamId64, msg, player.TeamNum, true);
            }

            return HookResult.Continue;
        }
        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid && player.AuthorizedSteamID != null)
            {
                PlayerData newPlayer = new PlayerData
                {
                    Name = player.PlayerName,
                    SteamId32 = player.AuthorizedSteamID!.SteamId32.ToString(),
                    SteamId64 = player.AuthorizedSteamID.SteamId64.ToString(),
                    CommunityUrl = player.AuthorizedSteamID.ToCommunityUrl().ToString(),
                    TeamShortName = GetTeamShortName(player.TeamNum),
                    TeamLongName = GetTeamLongName(player.TeamNum),
                    TeamNumber = player.TeamNum.ToString(),
                    Kills = 0.ToString(),
                    Deaths = 0.ToString(),
                    Assists = 0.ToString(),
                    Points = 0.ToString(),
                    CountryShort = "Undefined",
                    CountryLong = "Undefined",
                    CountryEmoji = ":flag_white:",
                    DiscordGlobalname = "",
                    DiscordNickname = "",
                    DiscordPing = "",
                    DiscordID = "",
                };
                playerData.Add(newPlayer);

                if (IsDbConnected)
                    _ = LoadPlayerData(player.AuthorizedSteamID.SteamId64.ToString());

                string IpAddress = player!.IpAddress!.Split(":")[0];
                LoadPlayerCountry(IpAddress, player.AuthorizedSteamID.SteamId64);
                if (Config.EventNotifications.Connect.Enabled)
                    PerformConnectEvent(player.AuthorizedSteamID.SteamId64);
            }

            return HookResult.Continue;
        }
        [GameEventHandler(HookMode.Pre)]
        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var player = @event.Userid;

            if (player != null && player.IsValid && player.AuthorizedSteamID != null)
            {
                if (Config.EventNotifications.Disconnect.Enabled)
                    PerformDisconnectEvent(player.AuthorizedSteamID.SteamId64);

                playerData.RemoveAll(p => p.SteamId64 == player.AuthorizedSteamID.SteamId64.ToString());

                /*if (linkCodes.ContainsValue(player.AuthorizedSteamID.SteamId64))
                {
                    var code = linkCodes.FirstOrDefault(x => x.Value == player.AuthorizedSteamID!.SteamId64).Key;
                    linkCodes.Remove(code);
                }*/

                if (linkedPlayers.ContainsKey(player.AuthorizedSteamID.SteamId64))
                {
                    if (Config.ConnectedPlayers.Enabled)
                    {
                        var discordId = linkedPlayers[player.AuthorizedSteamID.SteamId64];
                        if (!string.IsNullOrEmpty(discordId))
                            _ = RemoveConnectedPlayersRole(ulong.Parse(discordId));
                    }
                    linkedPlayers.Remove(player.AuthorizedSteamID.SteamId64);
                }
            }
            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var player = @event.Userid;
            var attacker = @event.Attacker;
            var assister = @event.Assister;

            if (player != null && player.IsValid && player.AuthorizedSteamID != null)
                UpdatePlayerData(player, 1);
            if (attacker != null && attacker.IsValid && attacker.AuthorizedSteamID != null)
                UpdatePlayerData(attacker, 1);
            if (assister != null && assister.IsValid && assister.AuthorizedSteamID != null)
                UpdatePlayerData(assister, 1);

            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid && player.AuthorizedSteamID != null)
                UpdatePlayerData(player, 2, @event.Team);

            return HookResult.Continue;
        }
    }
}