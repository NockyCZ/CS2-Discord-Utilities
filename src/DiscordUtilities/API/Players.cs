using DiscordUtilitiesAPI;
using CounterStrikeSharp.API.Core;
using DiscordUtilitiesAPI.Helpers;
using Discord.WebSocket;
using System.Text;

namespace DiscordUtilities;
public partial class DiscordUtilities : IDiscordUtilitiesAPI
{
    public UserData? GetUserDataByPlayerController(CCSPlayerController player)
    {
        if (!playerData.ContainsKey(player.Slot))
        {
            Perform_SendConsoleMessage($"'GetUserData' - Selected Player was not found on the server!", ConsoleColor.Cyan);
            return null;
        }

        if (!playerData[player.Slot].IsLinked)
        {
            Perform_SendConsoleMessage($"'GetUserData' - Selected Player is not linked!", ConsoleColor.Cyan);
            return null;
        }

        if (!TryGetGuild(out var guild))
            return null;
        var user = guild.GetUser(ulong.Parse(playerData[player.Slot].DiscordID));
        if (user == null)
        {
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

    public UserData? GetUserDataByUserID(ulong userId)
    {
        if (!TryGetGuild(out var guild))
            return null;
        var user = guild.GetUser(userId);
        if (user == null)
        {
            Perform_SendConsoleMessage($"'GetUserData' - User with ID '{userId}' was not found on the Discord server!", ConsoleColor.Cyan);
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

    public void AddRolesToUser(ulong userId, List<string> rolesIds)
    {
        if (!TryGetGuild(out var guild))
            return;
        var socketUser = guild.GetUser(userId);
        if (socketUser == null)
        {
            Perform_SendConsoleMessage($"AddRolesToUser - User with ID '{userId}' was not found on the Discord server!", ConsoleColor.Cyan);
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
        }
    }

    private static readonly HashSet<string> linkRoleWarnings = new();

    // The link role is owned by the Link system (added on link/join, removed on unlink).
    // Modules and timed roles must not be able to strip it from linked users.
    private static bool IsProtectedLinkRole(string roleId, string source)
    {
        if (string.IsNullOrEmpty(LinkRoleId) || roleId == null || roleId.Trim() != LinkRoleId)
            return false;

        bool firstWarning;
        lock (linkRoleWarnings)
        {
            firstWarning = linkRoleWarnings.Add(source);
        }

        if (firstWarning)
            Perform_SendConsoleMessage($"'{source}' - Role '{roleId}' is used as 'Link Role ID' and will not be removed! Use a different role in your modules (e.g. 'Connected Players Role') or timed roles.", ConsoleColor.DarkYellow);
        return true;
    }

    public void RemoveRolesFromUser(ulong userId, List<string> rolesIds)
    {
        if (rolesIds.Any(id => IsProtectedLinkRole(id, "RemoveRolesFromUser")))
        {
            rolesIds = rolesIds.Where(id => !IsProtectedLinkRole(id, "RemoveRolesFromUser")).ToList();
            if (rolesIds.Count == 0)
                return;
        }

        if (!TryGetGuild(out var guild))
            return;
        var socketUser = guild.GetUser(userId);
        if (socketUser == null)
        {
            Perform_SendConsoleMessage($"'RemoveRolesFromUser' - User with ID '{userId}' was not found on the Discord server!", ConsoleColor.Cyan);
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
        return new Dictionary<ulong, ulong>(linkedPlayers);
    }
}