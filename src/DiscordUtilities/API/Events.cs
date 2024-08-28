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

    public void ExecuteCustomCommands(string server, List<string> Commands)
    {
        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new ExecuteCustomCommands(server, Commands));
            if (IsDebug)
                Perform_SendConsoleMessage("New Event Triggered: 'ExecuteCustomCommands'", ConsoleColor.Cyan);
        });
    }

    public void PlayerDataLoaded(CCSPlayerController player)
    {
        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new PlayerDataLoaded(player));
            if (IsDebug)
                Perform_SendConsoleMessage("New Event Triggered: 'PlayerDataLoaded'", ConsoleColor.Cyan);
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
                Perform_SendConsoleMessage("New Event Triggered: 'LinkedUserDataLoaded'", ConsoleColor.Cyan);
        });
    }
    public void ServerDataLoaded()
    {
        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new ServerDataLoaded());
            if (IsDebug)
                Perform_SendConsoleMessage("New Event Triggered: 'ServerDataLoaded'", ConsoleColor.Cyan);
        });
    }
    public void BotLoaded()
    {
        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new BotLoaded());
            if (IsDebug)
                Perform_SendConsoleMessage("New Event Triggered: 'BotLoaded'", ConsoleColor.Cyan);
        });
    }

    public void Event_SlashCommand(SocketSlashCommand command)
    {
        var interactionId = int.Parse(GetRandomCode(6, true));

        List<CommandOptionsData> optionsData = new();
        foreach (var option in command.Data.Options)
        {
            string? value = "";
            if (option.Type == ApplicationCommandOptionType.User)
            {
                var targetUser = (SocketUser)option.Value;
                if (targetUser != null)
                    value = targetUser.Id.ToString();
            }
            else if (option.Type == ApplicationCommandOptionType.Role)
            {
                var targetRole = (SocketRole)option.Value;
                if (targetRole != null)
                    value = targetRole.Id.ToString();
            }
            else
            {
                value = option.Value.ToString();
            }

            optionsData.Add(new CommandOptionsData
            {
                Name = option.Name.ToLower(),
                Value = value != null ? value : "",
                Type = (SlashCommandOptionsType)option.Type
            });
        }

        ulong? guildId = command.GuildId != null ? command.GuildId.Value : null;

        var commandData = new CommandData
        {
            GuildId = guildId,
            InteractionId = interactionId,
            CommandName = command.CommandName,
            OptionsData = optionsData
        };

        var user = command.User as SocketGuildUser;
        var userData = new UserData
        {
            GlobalName = user != null ? user.GlobalName : command.User.GlobalName,
            DisplayName = user != null ? user.DisplayName : command.User.GlobalName,
            ID = user != null ? user.Id : command.User.Id,
            RolesIds = user != null ? user.Roles.Select(role => role.Id).ToList() : new(),
        };

        if (!savedInteractions.ContainsKey(interactionId))
            savedInteractions.Add(interactionId, command);

        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new SlashCommandExecuted(commandData, userData));
            if (IsDebug)
                Perform_SendConsoleMessage("New Event Triggered: 'SlashCommandExecuted'", ConsoleColor.Cyan);
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

        var user = message.Author as SocketGuildUser;
        var userData = new UserData
        {
            GlobalName = user != null ? user.GlobalName : message.Author.GlobalName,
            DisplayName = user != null ? user.DisplayName : message.Author.GlobalName,
            ID = user != null ? user.Id : message.Author.Id,
            RolesIds = user != null ? user.Roles.Select(role => role.Id).ToList() : new(),
        };

        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new CustomMessageReceived(customId, messageData, userData, savedMessages.ContainsKey(message.Id)));
            if (IsDebug)
                Perform_SendConsoleMessage("New Event Triggered: 'CustomMessageReceived'", ConsoleColor.Cyan);
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
        var userData = new UserData
        {
            GlobalName = user != null ? user.GlobalName : message.Author.GlobalName,
            DisplayName = user != null ? user.DisplayName : message.Author.GlobalName,
            ID = user != null ? user.Id : message.Author.Id,
            RolesIds = user != null ? user.Roles.Select(role => role.Id).ToList() : new(),
        };

        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new MessageReceived(messageData, userData));
            if (IsDebug)
                Perform_SendConsoleMessage("New Event Triggered: 'MessageReceived'", ConsoleColor.Cyan);
        });
    }

    public void Event_ModalSubmited(SocketInteraction interaction)
    {
        var modalInteraction = (SocketModal)interaction;
        var interactionId = int.Parse(GetRandomCode(6, true));

        ulong? guildId = interaction.GuildId != null ? interaction.GuildId.Value : null;

        var InputValues = new Dictionary<string, string>();
        var modalInputs = modalInteraction.Data.Components;
        foreach (var input in modalInputs)
        {
            InputValues.Add(input.CustomId, input.Value);
        }

        var modalData = new ModalData
        {
            ChannelName = interaction.Channel.Name,
            ChannelID = interaction.Channel.Id,
            GuildId = guildId,
            CustomId = modalInteraction.Data.CustomId,
            InputValues = InputValues,
            InteractionId = interactionId,
        };

        var user = interaction.User as SocketGuildUser;
        var userData = new UserData
        {
            GlobalName = user != null ? user.GlobalName : interaction.User.GlobalName,
            DisplayName = user != null ? user.DisplayName : interaction.User.GlobalName,
            ID = user != null ? user.Id : interaction.User.Id,
            RolesIds = user != null ? user.Roles.Select(role => role.Id).ToList() : new(),
        };

        if (!savedInteractions.ContainsKey(interactionId))
            savedInteractions.Add(interactionId, modalInteraction);

        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new ModalSubmited(modalData, userData));
            if (IsDebug)
                Perform_SendConsoleMessage("New Event Triggered: 'ModalSubmited'", ConsoleColor.Cyan);
        });
    }

    public void Event_InteractionCreated(SocketInteraction interaction, SocketMessageComponent component)
    {
        var interactionId = int.Parse(GetRandomCode(6, true));
        var message = component.Message;
        ulong? guildId = interaction.GuildId != null ? interaction.GuildId.Value : null;
        ulong? messageId = message != null ? message.Id : null;

        var interactionData = new InteractionData
        {
            ChannelName = interaction.Channel.Name,
            ChannelID = interaction.Channel.Id,
            MessageId = messageId,
            GuildId = guildId,
            CustomId = component.Data.CustomId,
            SelectedValues = component.Data.Values,
            InteractionId = interactionId,
            Builders = message == null ? null : GetMessageBuilders(message)
        };

        var user = interaction.User as SocketGuildUser;
        var userData = new UserData
        {
            GlobalName = user != null ? user.GlobalName : interaction.User.GlobalName,
            DisplayName = user != null ? user.DisplayName : interaction.User.GlobalName,
            ID = user != null ? user.Id : interaction.User.Id,
            RolesIds = user != null ? user.Roles.Select(role => role.Id).ToList() : new(),
        };

        if (!savedInteractions.ContainsKey(interactionId))
            savedInteractions.Add(interactionId, interaction);

        Server.NextFrame(() =>
        {
            DiscordUtilitiesAPI.Get()?.TriggerEvent(new InteractionCreated(interactionData, userData));
            if (IsDebug)
                Perform_SendConsoleMessage("New Event Triggered: 'InteractionCreated'", ConsoleColor.Cyan);
        });
    }
}

