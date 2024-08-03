using CounterStrikeSharp.API.Core;
using DiscordUtilitiesAPI.Builders;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;
namespace DiscordUtilitiesAPI;

public interface IDiscordUtilitiesAPI
{
    public void RegisterNewSlashCommand(Commands.Builder command);
    public void SendMessageToChannel(ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components);
    public void SendCustomMessageToChannel(string customId, ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components, bool saveMessage = false);
    public void SendRespondToMessage(ulong messageId, ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components);
    public void UpdateMessage(ulong messageId, ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components);
    public void SendRespondMessageToInteraction(int interactionId, string? content, Embeds.Builder? embed, Components.Builder? components, bool deleteOriginalInteraction = false, bool silent = true);
    public void SendRespondModalToInteraction(int interactionId, Modal.Builder Modal);
    public void SendRespondMessageToSlashCommand(int interactionId, string? content, Embeds.Builder? embed, Components.Builder? components, bool silent = true);
    public void SendRespondModalToSlashCommand(int interactionId, Modal.Builder Modal);
    public bool SendDirectMessage(ulong userId, string? content, Embeds.Builder? embed, Components.Builder? components);
    public void RemoveSavedCustomMessage(ulong messageId);
    public bool IsCustomMessageSaved(ulong messageId);
    public MessageData? GetMessageDataFromCustomMessage(ulong messageId);
    public Embeds.Builder GetEmbedBuilderFromConfig<T>(T obj, ReplaceVariables.Builder? replacedVariables = null);
    public void AddRolesToUser(ulong userId, List<string> rolesIds);
    public void RemoveRolesFromUser(ulong userId, List<string> rolesIds);
    public bool IsPlayerLinked(CCSPlayerController player);
    public bool IsPlayerDataLoaded(CCSPlayerController player);
    public UserData? GetUserDataByPlayerController(CCSPlayerController player);
    public UserData? GetUserDataByUserID(ulong userId);
    public Dictionary<ulong, ulong> GetLinkedPlayers();
    public event EventHandler<IDiscordUtilitiesEvent> DiscordUtilitiesEventHandlers;
    public void TriggerEvent(IDiscordUtilitiesEvent @event);
    public bool Debug();
    public void SendConsoleMessage(string text, MessageType type);
    public string ReplaceVariables(string text, ReplaceVariables.Builder replacedVariables);
    public void RemoveAllUsersFromRole(string roleId);
    public void CheckVersion(string moduleName, string moduleVersion);
    public bool IsBotLoaded();
    public bool IsDatabaseLoaded();
}
