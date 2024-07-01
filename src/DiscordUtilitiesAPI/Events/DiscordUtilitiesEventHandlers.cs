using CounterStrikeSharp.API.Core;
using DiscordUtilitiesAPI.Helpers;

namespace DiscordUtilitiesAPI.Events;
public record MessageReceived(MessageData Message, UserData User) : IDiscordUtilitiesEvent;
public record CustomMessageReceived(string CustomID, MessageData Message, UserData User, bool isStored) : IDiscordUtilitiesEvent;
public record SlashCommandExecuted(CommandData Command, UserData User) : IDiscordUtilitiesEvent;
public record InteractionCreated(InteractionData Interaction, UserData User) : IDiscordUtilitiesEvent;
public record ModalSubmited(ModalData ModalData, UserData User) : IDiscordUtilitiesEvent;
public record LinkedUserDataLoaded(UserData User, CCSPlayerController player) : IDiscordUtilitiesEvent;
public record PlayerDataLoaded(CCSPlayerController player) : IDiscordUtilitiesEvent;
public record BotLoaded() : IDiscordUtilitiesEvent;
public record ServerDataLoaded() : IDiscordUtilitiesEvent;