using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null)
                InitializePlayerData(player);

            serverData.OnlinePlayers = GetPlayersCount().ToString();
            serverData.OnlinePlayersAndBots = GetPlayersCountWithBots().ToString();
            serverData.OnlineBots = GetBotsCounts().ToString();
            return HookResult.Continue;
        }

        // Must be called on the game thread
        private void InitializePlayerData(CCSPlayerController player)
        {
            if (player.IsValid && player.AuthorizedSteamID != null && !playerData.ContainsKey(player.Slot))
            {
                PlayerData newPlayer = new PlayerData
                {
                    Name = player.PlayerName,
                    UserId = player.UserId.ToString()!,
                    SteamId32 = player.AuthorizedSteamID.SteamId32.ToString(),
                    SteamId64 = player.AuthorizedSteamID.SteamId64.ToString(),
                    IpAddress = player.IpAddress != null ? player.IpAddress.ToString() : "Invalid",
                    CommunityUrl = player.AuthorizedSteamID.ToCommunityUrl().ToString(),
                    PlayedTime = 0,
                    FirstJoin = DateTime.Now,
                    LastSeen = DateTime.Now,
                    CountryShort = "Undefined",
                    CountryLong = "Undefined",
                    CountryEmoji = ":flag_white:",
                    DiscordGlobalname = "",
                    DiscordDisplayName = "",
                    DiscordPing = "",
                    DiscordAvatar = "",
                    DiscordID = "",
                    IsLinked = false,
                };
                playerData.Add(player.Slot, newPlayer);
                if (IsDbConnected)
                {
                    // Primitive data is captured here, the database and GeoIP work runs on the thread pool
                    var steamId = player.AuthorizedSteamID.SteamId64.ToString();
                    var ipAddress = (player.IpAddress ?? "").Split(':')[0];
                    _ = Task.Run(() => UpdateOrLoadPlayerData(player, steamId, 0, true, ipAddress));
                }
            }
        }

        // Players that are already on the server do not fire 'player_connect_full' again (plugin reload, late database connection).
        // Must be called on the game thread
        private void LoadConnectedPlayers()
        {
            try
            {
                foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.Connected == PlayerConnectedState.Connected && p.AuthorizedSteamID != null))
                    InitializePlayerData(player);
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while loading data of the connected players: '{ex.Message}'", ConsoleColor.Red);
            }
        }

        // Linked players that joined before the Discord BOT was connected get their Discord data and Link role now.
        // Must be called on the game thread
        private void LoadConnectedLinkedPlayers()
        {
            try
            {
                if (!Config.Link.Enabled)
                    return;

                foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.AuthorizedSteamID != null && playerData.ContainsKey(p.Slot)))
                {
                    var steamId = player.AuthorizedSteamID!.SteamId64;
                    if (!linkedPlayers.TryGetValue(steamId, out var linkedDiscordId))
                        continue;

                    playerData[player.Slot].IsLinked = true;
                    _ = LoadPlayerData(steamId.ToString(), linkedDiscordId);
                }
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while loading data of the linked players: '{ex.Message}'", ConsoleColor.Red);
            }
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid && playerData.ContainsKey(player.Slot) && player.AuthorizedSteamID != null)
            {
                if (IsDbConnected)
                    _ = UpdateOrLoadPlayerData(player, player.AuthorizedSteamID.SteamId64.ToString(), playerData[player.Slot].PlayedTime, false);
                playerData.Remove(player.Slot);
            }
            serverData.OnlinePlayers = GetPlayersCount().ToString();
            serverData.OnlinePlayersAndBots = GetPlayersCountWithBots().ToString();
            serverData.OnlineBots = GetBotsCounts().ToString();

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            serverData.TeamScoreCT = GetTeamScore(3).ToString();
            serverData.TeamScoreT = GetTeamScore(2).ToString();
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnMatchEnd(EventCsWinPanelMatch @event, GameEventInfo info)
        {
            serverData.TeamScoreCT = GetTeamScore(3).ToString();
            serverData.TeamScoreT = GetTeamScore(2).ToString();
            return HookResult.Continue;
        }
    }
}