using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid || player.AuthorizedSteamID == null || !playerData.ContainsKey(player))
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

                if ((msg.StartsWith('!') || msg.StartsWith('/')) && !Config.Chatlog.DisplayCommands)
                    return HookResult.Continue;

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
            if (player == null || !player.IsValid || player.AuthorizedSteamID == null || !playerData.ContainsKey(player))
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

            string msg = info.GetArg(1);
            if (msg.StartsWith('@') && Config.Chatlog.AdminChat.Enabled && AdminManager.PlayerHasPermissions(player, Config.Chatlog.AdminChat.AdminFlag))
            {
                msg = msg.Replace("@", string.Empty);
                if (!string.IsNullOrEmpty(msg))
                    PerformAdminChatlog(player.AuthorizedSteamID.SteamId64, msg);
                return HookResult.Handled;
            }
            if (Config.Chatlog.Enabled)
            {
                if ((msg.StartsWith('!') || msg.StartsWith('/')) && !Config.Chatlog.DisplayCommands)
                    return HookResult.Continue;

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
            if (player != null && player.IsValid && player.AuthorizedSteamID != null && !playerData.ContainsKey(player))
            {
                PlayerData newPlayer = new PlayerData
                {
                    Name = player.PlayerName,
                    NameWithoutEmoji = RemoveEmoji(player.PlayerName),
                    SteamId32 = player.AuthorizedSteamID!.SteamId32.ToString(),
                    SteamId64 = player.AuthorizedSteamID.SteamId64.ToString(),
                    IpAddress = player.IpAddress!.ToString(),
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
                    DiscordDisplayName = "",
                    DiscordPing = "",
                    DiscordID = "",
                };
                playerData.Add(player, newPlayer);

                if (IsDbConnected && Config.Link.Enabled && linkedPlayers.ContainsKey(player.AuthorizedSteamID.SteamId64))
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

            if (player != null && player.IsValid && playerData.ContainsKey(player))
            {
                if (Config.EventNotifications.Disconnect.Enabled)
                    _ = PerformDisconnectEvent(player.AuthorizedSteamID!.SteamId64);

                playerData.Remove(player);

                if (linkedPlayers.ContainsKey(player.AuthorizedSteamID!.SteamId64))
                {
                    if (Config.ConnectedPlayers.Enabled)
                    {
                        var discordId = linkedPlayers[player.AuthorizedSteamID.SteamId64];
                        if (!string.IsNullOrEmpty(discordId))
                            _ = RemoveConnectedPlayersRole(ulong.Parse(discordId));
                    }
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

            if (player != null && player.IsValid && playerData.ContainsKey(player))
                UpdatePlayerData(player, 1);
            if (attacker != null && attacker.IsValid && playerData.ContainsKey(attacker))
                UpdatePlayerData(attacker, 1);
            if (assister != null && assister.IsValid && playerData.ContainsKey(assister))
                UpdatePlayerData(assister, 1);

            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid && playerData.ContainsKey(player))
                UpdatePlayerData(player, 2, @event.Team);

            return HookResult.Continue;
        }
    }
}