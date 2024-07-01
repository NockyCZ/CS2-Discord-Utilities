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
            if (player != null && player.IsValid && player.AuthorizedSteamID != null && !playerData.ContainsKey(player.Slot))
            {
                PlayerData newPlayer = new PlayerData
                {
                    Name = player.PlayerName,
                    NameWithoutEmoji = RemoveEmoji(player.PlayerName),
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
                    _ = UpdateOrLoadPlayerData(player, player.AuthorizedSteamID.SteamId64.ToString(), 0);
            }
            return HookResult.Continue;
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

            return HookResult.Continue;
        }
    }
}