using CounterStrikeSharp.API.Core;
using DiscordUtilitiesAPI.Builders;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;
namespace DiscordUtilitiesAPI;

public interface IDiscordUtilitiesAPI
{
    public void RegisterNewSlashCommand(Commands.Builder command);
    public void SendMessageToChannel(ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components);
    public void SendCustomMessageToChannel(string customId, ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components);
    public void SendRespondToMessage(ulong messageId, ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components);
    public void UpdateMessage(ulong messageId, ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components);
    public void SendRespondMessageToInteraction(int interactionId, string? content, Embeds.Builder? embed, Components.Builder? components, bool silent = true);
    public void SendRespondMessageToSlashCommand(int interactionId, string? content, Embeds.Builder? embed, Components.Builder? components, bool silent = true);
    public Embeds.Builder GetEmbedBuilderFromConfig<T>(T obj, ReplaceVariables.Builder? replacedVariables = null);
    public void AddRolesToUser(UserData user, List<string> rolesIds);
    public void RemoveRolesFromUser(UserData user, List<string> rolesIds);
    public bool IsPlayerLinked(CCSPlayerController player);
    public bool IsPlayerDataLoaded(CCSPlayerController player);
    public UserData? GetUserData(CCSPlayerController player);
    public Dictionary<ulong, ulong> GetLinkedPlayers();
    public event EventHandler<IDiscordUtilitiesEvent> DiscordUtilitiesEventHandlers;
    public void TriggerEvent(IDiscordUtilitiesEvent @event);
    public bool Debug();
    public void SendConsoleMessage(string text, MessageType type);
    public string ReplaceVariables(string text, ReplaceVariables.Builder replacedVariables);
    public bool IsValidEmoji(string emoji);
    public void RemoveAllUsersFromRole(string roleId);
    public bool IsBotLoaded();
    public bool IsDatabaseLoaded();
}
