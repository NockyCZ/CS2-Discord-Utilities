using DiscordUtilitiesAPI;
using CounterStrikeSharp.API.Core;
using DiscordUtilitiesAPI.Helpers;
using Discord.WebSocket;
using System.Text;

namespace DiscordUtilities;
public partial class DiscordUtilities : IDiscordUtilitiesAPI
{
    public UserData? GetUserData(CCSPlayerController player)
    {
        if (!playerData.ContainsKey(player.Slot))
        {
            if (IsDebug)
                Perform_SendConsoleMessage($"'GetUserData' - Selected Player was not found on the server!", ConsoleColor.Cyan);
            return null;
        }

        if (!playerData[player.Slot].IsLinked)
        {
            if (IsDebug)
                Perform_SendConsoleMessage($"'GetUserData' - Selected Player is not linked!", ConsoleColor.Cyan);
            return null;
        }

        var guild = BotClient!.GetGuild(ulong.Parse(ServerId));
        if (guild == null)
        {
            Perform_SendConsoleMessage($"Guild with id '{ServerId}' was not found!", ConsoleColor.Red);
            throw new Exception($"Guild with id '{ServerId}' was not found!");
        }
        var user = guild.GetUser(ulong.Parse(playerData[player.Slot].DiscordID));
        if (user == null)
        {
            if (IsDebug)
                Perform_SendConsoleMessage($"'GetUserData' - User with ID '{playerData[player.Slot].DiscordID}' was not found on the Discord server!", ConsoleColor.Cyan);
            return null;
        }

        var userRoles = user.Roles.Select(role => role.Id).ToList() ?? new List<ulong>();
        var userData = new UserData
        {
            GlobalName = user.GlobalName,
            DisplayName = user.DisplayName,
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
            Perform_SendConsoleMessage($"Guild with id '{ServerId}' was not found!", ConsoleColor.Red);
            throw new Exception($"Guild with id '{ServerId}' was not found!");
        }
        var socketUser = guild.GetUser(user.ID);
        if (socketUser == null)
        {
            if (IsDebug)
                Perform_SendConsoleMessage($"AddRolesToUser - User with ID '{user!.ID}' was not found on the Discord server!", ConsoleColor.Cyan);
            return;
        }

        _ = AddRolesToUserAsync(socketUser, rolesIds);
    }
    public async Task AddRolesToUserAsync(SocketGuildUser user, List<string> rolesIds)
    {
        try
        {
            var socketRoles = rolesIds.Select(id => user.Guild.GetRole(ulong.Parse(id)));
            if (!user.Roles.Any(userRole => socketRoles.Any(role => role.Id == userRole.Id)))
            {
                await user.AddRolesAsync(socketRoles);
                if (IsDebug)
                {
                    var sb = new StringBuilder();
                    foreach (var roleId in rolesIds)
                    {
                        var role = user.Guild.GetRole(ulong.Parse(roleId));
                        sb.Append(role.Name);
                        sb.Append(", ");
                    }
                    sb.Length -= 2;
                    Perform_SendConsoleMessage($"Roles have been added to user '{user.DisplayName}': {sb.ToString()}", ConsoleColor.Cyan);
                }
            }
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while adding roles: '{ex.Message}'", ConsoleColor.Red);
            throw new Exception($"An error occurred when adding roles: {ex.Message}");
        }
    }

    public void RemoveRolesFromUser(UserData user, List<string> rolesIds)
    {
        var guild = BotClient!.GetGuild(ulong.Parse(ServerId));
        if (guild == null)
        {
            Perform_SendConsoleMessage($"Guild with id '{ServerId}' was not found!", ConsoleColor.Red);
            throw new Exception($"Guild with id '{ServerId}' was not found!");
        }
        var socketUser = guild.GetUser(user.ID);
        if (socketUser == null)
        {
            if (IsDebug)
                Perform_SendConsoleMessage($"'RemoveRolesFromUser' - User with ID '{user.ID}' was not found on the Discord server!", ConsoleColor.Cyan);
            return;
        }

        _ = RemoveRolesFromUserAsync(socketUser, rolesIds);
    }

    public async Task RemoveRolesFromUserAsync(SocketGuildUser user, List<string> rolesIds)
    {
        try
        {
            var socketRoles = rolesIds.Select(id => user.Guild.GetRole(ulong.Parse(id)));
            if (user.Roles.Any(userRole => socketRoles.Any(role => role.Id == userRole.Id)))
            {
                await user.RemoveRolesAsync(socketRoles);
                if (IsDebug)
                {
                    var sb = new StringBuilder();
                    foreach (var roleId in rolesIds)
                    {
                        var role = user.Guild.GetRole(ulong.Parse(roleId));
                        sb.Append(role.Name);
                        sb.Append(", ");
                    }
                    sb.Length -= 2;
                    Perform_SendConsoleMessage($"Roles have been removed from user '{user.DisplayName}': {sb.ToString()}", ConsoleColor.Cyan);
                }
            }
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while removing roles: '{ex.Message}'", ConsoleColor.Red);
            throw new Exception($"An error occurred when removing roles: {ex.Message}");
        }
    }

    public bool IsPlayerLinked(CCSPlayerController? player)
    {
        return player != null && player.IsValid && !player.IsBot && !player.IsHLTV && playerData.ContainsKey(player.Slot) && playerData[player.Slot].IsLinked;
    }

    public bool IsPlayerDataLoaded(CCSPlayerController? player)
    {
        return player != null && player.IsValid && !player.IsBot && !player.IsHLTV && playerData.ContainsKey(player.Slot);
    }

    public Dictionary<ulong, ulong> GetLinkedPlayers()
    {
        return linkedPlayers;
    }
}