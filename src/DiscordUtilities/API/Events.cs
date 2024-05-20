using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Discord;
using Discord.WebSocket;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;

namespace DiscordUtilities;

public partial class DiscordUtilities : IDiscordUtilitiesAPI
{
    public event EventHandler<IDiscordUtilitiesEvent>? DiscordUtilitiesEventHandlers;
    public void TriggerEvent(IDiscordUtilitiesEvent @event)
    {
        DiscordUtilitiesEventHandlers?.Invoke(this, @event);
    }
    public void PlayerDataLoaded(CCSPlayerController player)
    {
        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new PlayerDataLoaded(player));
            if (IsDebug)
                Perform_SendConsoleMessage("[Discord Utilities] New Event Triggered: PlayerDataLoaded", ConsoleColor.Cyan);
        });
    }
    public void LinkedUserLoaded(SocketGuildUser user, CCSPlayerController player)
    {
        var userRoles = user.Roles.Select(role => role.Id).ToList() ?? new List<ulong>();
        var userData = new UserData
        {
            GlobalName = user.GlobalName,
            DisplayName = user.DisplayName,
            ID = user.Id,
            RolesIds = userRoles,
        };
        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new LinkedUserDataLoaded(userData, player));
            if (IsDebug)
                Perform_SendConsoleMessage("[Discord Utilities] New Event Triggered: LinkedUserDataLoaded", ConsoleColor.Cyan);
        });
    }
    public void ServerDataLoaded()
    {
        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new ServerDataLoaded());
            if (IsDebug)
                Perform_SendConsoleMessage("[Discord Utilities] New Event Triggered: ServerDataLoaded", ConsoleColor.Cyan);
        });
    }
    public void BotLoaded()
    {
        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new BotLoaded());
            if (IsDebug)
                Perform_SendConsoleMessage("[Discord Utilities] New Event Triggered: BotLoaded", ConsoleColor.Cyan);
        });
    }

    public void Event_SlashCommand(SocketSlashCommand command)
    {
        var interactionId = savedCommandInteractions.Count() + 1;
        var optionsData = command.Data.Options.Select(option => new CommandOptionsData
        {
            Name = option.Name,
            Value = option.Value?.ToString() ?? "",
            Type = (SlashCommandOptionsType)option.Type
        }).ToList();

        ulong? guildId = command.GuildId != null ? command.GuildId.Value : null;

        var commandData = new CommandData
        {
            GuildId = guildId,
            InteractionId = interactionId,
            CommandName = command.CommandName,
            OptionsData = optionsData
        };

        var user = command.User as SocketGuildUser;
        if (user == null)
        {
            if (IsDebug)
                Perform_SendConsoleMessage($"[Discord Utilities] DEBUG: Event_SlashCommand - User was not found!", ConsoleColor.Cyan);
            return;
        }

        var userRoles = user.Roles.Select(role => role.Id).ToList() ?? new List<ulong>();
        var userData = new UserData
        {
            GlobalName = user.GlobalName,
            DisplayName = user.DisplayName,
            ID = user.Id,
            RolesIds = userRoles,
        };

        if (!savedCommandInteractions.ContainsKey(interactionId))
            savedCommandInteractions.Add(interactionId, command);

        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new SlashCommandExecuted(commandData, userData));
            if (IsDebug)
                Perform_SendConsoleMessage("[Discord Utilities] New Event Triggered: SlashCommandExecuted", ConsoleColor.Cyan);
        });
    }

    public void Event_CustomMessageReceived(string customId, IUserMessage message)
    {
        var guildChannel = message.Channel as SocketGuildChannel;
        ulong? guildId = guildChannel != null ? guildChannel.Guild.Id : null;

        var messageData = new MessageData
        {
            ChannelName = message.Channel.Name,
            ChannelID = message.Channel.Id,
            MessageID = message.Id,
            Text = message.Content,
            GuildId = guildId,
            Builders = GetMessageBuilders(message)
        };

        var user = message.Author;
        var userRoles = (user as SocketGuildUser)?.Roles.Select(role => role.Id).ToList() ?? new List<ulong>();
        var userData = new UserData
        {
            GlobalName = user.GlobalName,
            DisplayName = user.Username,
            ID = user.Id,
            RolesIds = userRoles
        };

        /*var user = message.Author;
        if (user == null)
        {
            if (IsDebug)
                Perform_SendConsoleMessage($"[Discord Utilities] DEBUG: Event_CustomMessageReceived - User was not found!", ConsoleColor.Cyan);
            return;
        }

        var userRoles = user.Roles.Select(role => role.Id).ToList() ?? new List<ulong>();
        var userData = new UserData
        {
            GlobalName = user.GlobalName,
            DisplayName = user.DisplayName,
            ID = user.Id,
            RolesIds = userRoles
        };*/

        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new CustomMessageReceived(customId, messageData, userData, savedMessages.ContainsKey(message.Id)));
            if (IsDebug)
                Perform_SendConsoleMessage("[Discord Utilities] New Event Triggered: CustomMessageReceived", ConsoleColor.Cyan);
        });
    }

    public void Event_MessageReceived(SocketMessage message)
    {
        var guildChannel = message.Channel as SocketGuildChannel;
        ulong? guildId = guildChannel != null ? guildChannel.Guild.Id : null;

        var messageData = new MessageData
        {
            ChannelName = message.Channel.Name,
            ChannelID = message.Channel.Id,
            MessageID = message.Id,
            Text = message.Content,
            GuildId = guildId,
            Builders = GetMessageBuilders(message as IUserMessage)
        };

        var user = message.Author as SocketGuildUser;
        if (user == null)
        {
            if (IsDebug)
                Perform_SendConsoleMessage($"[Discord Utilities] DEBUG: Event_MessageReceived - User was not found!", ConsoleColor.Cyan);
            return;
        }

        var userRoles = user.Roles.Select(role => role.Id).ToList() ?? new List<ulong>();
        var userData = new UserData
        {
            GlobalName = user.GlobalName,
            DisplayName = user.DisplayName,
            ID = user.Id,
            RolesIds = userRoles
        };
        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new MessageReceived(messageData, userData));
            if (IsDebug)
                Perform_SendConsoleMessage("[Discord Utilities] New Event Triggered: MessageReceived", ConsoleColor.Cyan);
        });
    }

    public void Event_InteractionCreated(SocketInteraction interaction, IComponentInteractionData componentData)
    {
        var interactionId = savedInteractions.Count() + 1;
        IUserMessage? message = null;
        if (interaction is SocketMessageComponent messageComponent)
        {
            message = messageComponent.Message;
        }

        ulong? guildId = interaction.GuildId != null ? interaction.GuildId.Value : null;
        ulong? messageId = message != null ? message.Id : null;

        var interactionData = new InteractionData
        {
            ChannelName = interaction.Channel.Name,
            ChannelID = interaction.Channel.Id,
            MessageId = messageId,
            GuildId = guildId,
            CustomId = componentData.CustomId,
            SelectedValues = componentData.Values,
            InteractionId = interactionId,
            Builders = message == null ? null : GetMessageBuilders(message)
        };

        var user = interaction.User as SocketGuildUser;
        if (user == null)
        {
            if (IsDebug)
                Perform_SendConsoleMessage($"[Discord Utilities] DEBUG: interactionData - User was not found!", ConsoleColor.Cyan);
            return;
        }

        var userRoles = user.Roles.Select(role => role.Id).ToList() ?? new List<ulong>();
        var userData = new UserData
        {
            GlobalName = user.GlobalName,
            DisplayName = user.DisplayName,
            ID = user.Id,
            RolesIds = userRoles
        };

        if (!savedInteractions.ContainsKey(interactionId))
            savedInteractions.Add(interactionId, interaction);

        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new InteractionCreated(interactionData, userData));
            if (IsDebug)
                Perform_SendConsoleMessage("[Discord Utilities] New Event Triggered: InteractionCreated", ConsoleColor.Cyan);
        });
    }
}

