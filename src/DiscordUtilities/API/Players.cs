using DiscordUtilitiesAPI;
using CounterStrikeSharp.API.Core;
using DiscordUtilitiesAPI.Helpers;

namespace DiscordUtilities;
public partial class DiscordUtilities : IDiscordUtilitiesAPI
{
    public UserData? GetUserData(CCSPlayerController player)
    {
        if (!playerData.ContainsKey(player))
        {
            if (IsDebug)
                Perform_SendConsoleMessage($"[Discord Utilities] DEBUG: GetUserData - Selected Player was not found on the server!", ConsoleColor.Cyan);
            return null;
        }

        if (!playerData[player].IsLinked)
        {
            if (IsDebug)
                Perform_SendConsoleMessage($"[Discord Utilities] DEBUG: GetUserData - Selected Player is not linked!", ConsoleColor.Cyan);
            return null;
        }

        var guild = BotClient!.GetGuild(ulong.Parse(ServerId));
        if (guild == null)
        {
            Perform_SendConsoleMessage($"[Discord Utilities] Guild with id '{ServerId}' was not found!", ConsoleColor.Red);
            throw new Exception($"Guild with id '{ServerId}' was not found!");
        }
        var user = guild.GetUser(ulong.Parse(playerData[player].DiscordID));
        if (user == null)
        {
            if (IsDebug)
                Perform_SendConsoleMessage($"[Discord Utilities] DEBUG: GetUserData - User with ID '{playerData[player].DiscordID}' was not found on the Discord server!", ConsoleColor.Cyan);
            return null;
        }

        var userRoles = user.Roles.Select(role => role.Id).ToList() ?? new List<ulong>();
        var userData = new UserData
        {
            GlobalName = user.GlobalName,
            DisplayName = user.Username,
            ID = user.Id,
            RolesIds = userRoles
        };
        return userData;
    }

    public void AddRolesToUser(UserData user, List<string> rolesIds)
    {
        var guild = BotClient!.GetGuild(ulong.Parse(ServerId));
        if (guild == null)
        {
            Perform_SendConsoleMessage($"[Discord Utilities] Guild with id '{ServerId}' was not found!", ConsoleColor.Red);
            throw new Exception($"Guild with id '{ServerId}' was not found!");
        }
        var socketUser = guild.GetUser(user.ID);
        if (socketUser == null)
        {
            if (IsDebug)
                Perform_SendConsoleMessage($"[Discord Utilities] DEBUG: GetUserData - User with ID '{user!.ID}' was not found on the Discord server!", ConsoleColor.Cyan);
            return;
        }

        var socketRoles = rolesIds.Select(id => socketUser.Guild.GetRole(ulong.Parse(id)));
        if (!socketUser.Roles.Any(userRole => socketRoles.Any(role => role.Id == userRole.Id)))
        {
            _ = socketUser.AddRolesAsync(socketRoles);
        }
    }

    public void RemoveRolesFromUser(UserData user, List<string> rolesIds)
    {
        var guild = BotClient!.GetGuild(ulong.Parse(ServerId));
        if (guild == null)
        {
            Perform_SendConsoleMessage($"[Discord Utilities] Guild with id '{ServerId}' was not found!", ConsoleColor.Red);
            throw new Exception($"Guild with id '{ServerId}' was not found!");
        }
        var socketUser = guild.GetUser(user.ID);
        if (socketUser == null)
        {
            if (IsDebug)
                Perform_SendConsoleMessage($"[Discord Utilities] DEBUG: GetUserData - User with ID '{user!.ID}' was not found on the Discord server!", ConsoleColor.Cyan);
            return;
        }

        var socketRoles = rolesIds.Select(id => socketUser.Guild.GetRole(ulong.Parse(id)));
        if (socketUser.Roles.Any(userRole => socketRoles.Any(role => role.Id == userRole.Id)))
        {
            _ = socketUser.RemoveRolesAsync(socketRoles);
        }
    }

    public bool IsPlayerLinked(CCSPlayerController? player)
    {
        return player != null && player.IsValid && !player.IsBot && !player.IsHLTV && playerData.ContainsKey(player) && playerData[player].IsLinked;
    }

    public bool IsPlayerDataLoaded(CCSPlayerController? player)
    {
        return player != null && player.IsValid && !player.IsBot && !player.IsHLTV && playerData.ContainsKey(player);
    }

    public Dictionary<ulong, ulong> GetLinkedPlayers()
    {
        return linkedPlayers;
    }
}