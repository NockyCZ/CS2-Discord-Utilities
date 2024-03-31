using CounterStrikeSharp.API.Modules.Admin;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
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
        public async Task RemoveLinkRole(string discordid)
        {
            try
            {
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
                if (user.Roles.Any(id => id == role))
                {
                    await user.RemoveRoleAsync(role);
                }
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while removing Link role: {ex.Message}", ConsoleColor.Red);
            }
        }
        public async Task PerformLinkRole(string discordid)
        {
            try
            {
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