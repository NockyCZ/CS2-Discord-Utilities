namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {

        public async Task ClearConnectedPlayersRole()
        {
            try
            {
                var guild = BotClient!.GetGuild(ulong.Parse(Config.ServerID));
                if (guild == null)
                {
                    SendConsoleMessage($"[Discord Utilities] Guild with id '{Config.ServerID}' was not found!", ConsoleColor.Red);
                    return;
                }
                var role = guild.GetRole(ulong.Parse(Config.ConnectedPlayers.Role));
                if (role == null)
                {
                    SendConsoleMessage($"[Discord Utilities] Role with id '{Config.ConnectedPlayers.Role}' was not found (Connected Players Role Section)!", ConsoleColor.Red);
                    return;
                }

                var users = role!.Members;
                if (users != null)
                {
                    foreach (var user in users)
                    {
                        await user.RemoveRoleAsync(role);
                    }
                }
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while removing connected players role: {ex.Message}", ConsoleColor.Red);
            }
        }

        public async Task AddConnectedPlayersRole(ulong discordid)
        {
            try
            {
                var guild = BotClient!.GetGuild(ulong.Parse(Config.ServerID));
                if (guild == null)
                {
                    SendConsoleMessage($"[Discord Utilities] Guild with id '{Config.ServerID}' was not found!", ConsoleColor.Red);
                    return;
                }

                var user = guild.GetUser(discordid);
                if (user == null)
                    return;

                var role = guild.GetRole(ulong.Parse(Config.ConnectedPlayers.Role));
                if (role == null)
                {
                    SendConsoleMessage($"[Discord Utilities] Role with id '{Config.ConnectedPlayers.Role}' was not found (Connected Players Role Section)!", ConsoleColor.Red);
                    return;
                }
                if (!user.Roles.Any(id => id == role))
                    await user.AddRoleAsync(role);
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while adding connected players role: {ex.Message}", ConsoleColor.Red);
            }
        }

        public async Task RemoveConnectedPlayersRole(ulong discordid)
        {
            try
            {
                var guild = BotClient!.GetGuild(ulong.Parse(Config.ServerID));
                if (guild == null)
                {
                    SendConsoleMessage($"[Discord Utilities] Guild with id '{Config.ServerID}' was not found!", ConsoleColor.Red);
                    return;
                }
                var user = guild.GetUser(discordid);
                if (user == null)
                    return;

                var role = guild.GetRole(ulong.Parse(Config.ConnectedPlayers.Role));
                if (role == null)
                {
                    SendConsoleMessage($"[Discord Utilities] Role with id '{Config.ConnectedPlayers.Role}' was not found (Connected Players Role Section)!", ConsoleColor.Red);
                    return;
                }

                if (user.Roles.Any(id => id == role))
                    await user.RemoveRoleAsync(role);
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while removing connected players role: {ex.Message}", ConsoleColor.Red);
            }
        }
    }
}