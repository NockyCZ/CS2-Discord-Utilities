using CounterStrikeSharp.API.Modules.Admin;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public Dictionary<string, ulong> linkCodes = new Dictionary<string, ulong>();
        public void PerformLinkAccount(string code, string discordName, string discordId)
        {
            var player = GetTargetBySteamID64(linkCodes[code]);
            linkCodes.Remove(code);
            if (player != null && player.IsValid && player.AuthorizedSteamID != null)
            {
                if (!linkedPlayers.ContainsKey(player.AuthorizedSteamID.SteamId64))
                    linkedPlayers.Add(player.AuthorizedSteamID.SteamId64, discordId);
                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AccountLinked", discordName]}");
            }
        }

        public void PerformLinkPermission(ulong steamid)
        {
            var Permission = Config.Link.LinkPermissions;
            if (Permission.StartsWith('@') || Permission.StartsWith('#'))
            {

                var player = GetTargetBySteamID64(steamid);
                if (player == null || !player.IsValid)
                    return;

                if (!string.IsNullOrEmpty(Permission))
                {
                    if (Permission.StartsWith('@'))
                        AdminManager.AddPlayerPermissions(player, Permission);
                    else
                        AdminManager.AddPlayerToGroup(player, Permission);
                }
            }
            else
            {
                SendConsoleMessage($"[Discord Utilities] Invalid permission '{Permission}'!", ConsoleColor.Red);
                return;
            }
        }
        public async Task PerformLinkRole(string discordid)
        {
            try
            {
                /*var guilds = BotClient!.Guilds;
                IGuild guild = null!;
                foreach (var g in guilds)
                {
                    var currentUser = await BotClient.GetUserAsync(ulong.Parse(discordid));
                    if (currentUser != null)
                    {
                        guild = g;
                        break;
                    }
                }

                var user = await guild.GetUserAsync(ulong.Parse(discordid));
                if (user == null)
                    return;
                */
                var guild = BotClient!.GetGuild(ulong.Parse(Config.ServerID));
                if (guild == null)
                {
                    SendConsoleMessage($"[Discord Utilities] Guild with id '{Config.ServerID}' was not found!", ConsoleColor.Red);
                    return;
                }
                var user = guild.GetUser(ulong.Parse(discordid));
                if (user == null)
                    return;


                var role = guild.GetRole(ulong.Parse(Config.Link.LinkRole));
                if (role == null)
                {
                    SendConsoleMessage($"[Discord Utilities] Role with id '{Config.Link.LinkRole}' was not found (Link Section)!", ConsoleColor.Red);
                    return;
                }
                if (!user.Roles.Any(id => id == role))
                {
                    await user.AddRoleAsync(role);
                }
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while adding Link role: {ex.Message}", ConsoleColor.Red);
            }
        }
    }
}