using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        private void CreateCustomCommands()
        {
            if (Config.Link.Enabled)
            {
                string[] LinkCmds = Config.Link.IngameLinkCommands.Split(',');
                foreach (var cmd in LinkCmds)
                    AddCommand($"css_{cmd}", $"Discord Link Command ({cmd})", LinkProfile_CMD);

                string[] UnlinkCmds = Config.Link.IngameUnlinkCommands.Split(',');
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
                    _ = InsertNewCode(steamId, code);
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