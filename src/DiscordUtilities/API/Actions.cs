using CounterStrikeSharp.API.Core;
using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Builders;

namespace DiscordUtilities;
public partial class DiscordUtilities : IDiscordUtilitiesAPI
{
    public void RemoveSavedCustomMessage(ulong messageId)
    {
        savedMessages.TryRemove(messageId, out _);
    }

    private const int MaxSavedMessages = 500;

    // Saved messages are only removed by the module that saved them, so cap the cache and drop the oldest ones
    private static void SaveCustomMessage(IUserMessage message)
    {
        savedMessages.TryAdd(message.Id, message);

        int overflow = savedMessages.Count - MaxSavedMessages;
        if (overflow <= 0)
            return;

        // Message IDs are snowflakes, the lowest ID is the oldest message
        foreach (var messageId in savedMessages.Keys.OrderBy(id => id).Take(overflow).ToList())
            savedMessages.TryRemove(messageId, out _);
    }

    public void RemoveAllUsersFromRole(string roleId)
    {
        if (!string.IsNullOrEmpty(roleId))
        {
            if (IsProtectedLinkRole(roleId, "RemoveAllUsersFromRole"))
                return;

            _ = RemoveAllUsersFromRoleAsync(ulong.Parse(roleId));
        }
    }
    public async Task RemoveAllUsersFromRoleAsync(ulong roleId)
    {
        try
        {
            if (!TryGetGuild(out var guild))
                return;
            var role = guild.GetRole(roleId);
            if (role == null)
            {
                Perform_SendConsoleMessage($"Role with id '{roleId}' was not found! (RemoveAllUsersFromRole)", ConsoleColor.Red);
                return;
            }

            var users = role.Members;
            if (users != null)
            {
                foreach (var user in users)
                {
                    await user.RemoveRoleAsync(role);
                }
            }
            if (IsDebug)
                Perform_SendConsoleMessage($"Role with id '{roleId}' has been cleared", ConsoleColor.Cyan);
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while removing all users from the role: '{ex.Message}'", ConsoleColor.Red);
        }
    }

    public void RegisterNewSlashCommand(Commands.Builder command)
    {
        var commandData = command.commandData;
        var customCommand = new SlashCommandBuilder()
            .WithName(commandData.Name.ToLower())
            .WithDescription(commandData.Description);

        var commandOptions = command.commandOptions;
        if (commandOptions != null && commandOptions.Count() > 0)
        {
            foreach (var cmd in commandOptions)
            {
                var customCommandOptions = new SlashCommandOptionBuilder();
                customCommandOptions.WithName(cmd.Name.ToLower());
                customCommandOptions.WithDescription(cmd.Description);
                customCommandOptions.WithType((ApplicationCommandOptionType)cmd.Type);
                customCommandOptions.WithRequired(cmd.Required);
                if (cmd.Choices != null)
                {
                    foreach (var choice in cmd.Choices)
                    {
                        customCommandOptions.AddChoice(choice.Name, choice.Value);
                    }
                }
                customCommand.AddOption(customCommandOptions);
            }
        }
        _ = RegisterNewSlashCommandAsync(customCommand);
    }

    public async Task RegisterNewSlashCommandAsync(SlashCommandBuilder command)
    {
        try
        {
            if (BotClient != null)
            {
                await BotClient.CreateGlobalApplicationCommandAsync(command.Build());
                if (IsDebug)
                    Perform_SendConsoleMessage($"Slash Command '{command.Name}' has been successfully updated/created", ConsoleColor.Cyan);
            }
            else
                throw new Exception("Failed to create Slash Command because BOT is not connected!");
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while updating API Slash Commands: '{ex.Message}'", ConsoleColor.Red);
        }
    }

    public void SendMessageToChannel(ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components)
    {
        EmbedBuilder? embedToBuild = new();
        ComponentBuilder? componentsToBuild = new();

        if (embed != null)
        {
            embedToBuild = GetEmbedBuilder(embed);
        }
        if (components != null)
        {
            componentsToBuild = GetComponentsBuilder(components);
        }
        _ = SendMessageToChannelAsync(channelId, content, embedToBuild, componentsToBuild);
    }

    public async Task SendMessageToChannelAsync(ulong channelId, string? content, EmbedBuilder embed, ComponentBuilder? components)
    {
        try
        {
            if (BotClient != null)
            {
                if (BotClient.GetChannel(channelId) is not IMessageChannel channel)
                {
                    Perform_SendConsoleMessage($"Invalid Channel ID '{channelId}'.", ConsoleColor.Red);
                    return;
                }
                await channel.SendMessageAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: components != null ? components.Build() : null);
                if (IsDebug)
                    Perform_SendConsoleMessage($"The message was successfully sent to the channel with ID '{channelId}'", ConsoleColor.Cyan);
            }
            else
            {
                Perform_SendConsoleMessage("Failed to send a message to channel because BOT is not connected!", ConsoleColor.Red);
                return;
            }
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while sending a message: '{ex.Message}'", ConsoleColor.Red);
        }
    }

    public void SendCustomMessageToChannel(string customId, ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components, bool saveMessage = false)
    {
        EmbedBuilder? embedToBuild = new();
        ComponentBuilder? componentsToBuild = new();

        if (embed != null)
        {
            embedToBuild = GetEmbedBuilder(embed);
        }
        if (components != null)
        {
            componentsToBuild = GetComponentsBuilder(components);
        }
        _ = SendCustomMessageToChannelAsync(customId, channelId, content, embedToBuild, componentsToBuild, saveMessage);
    }

    public async Task SendCustomMessageToChannelAsync(string customId, ulong channelId, string? content, EmbedBuilder embed, ComponentBuilder? components, bool saveMessage = false)
    {
        try
        {
            if (BotClient != null)
            {
                if (BotClient.GetChannel(channelId) is not IMessageChannel channel)
                {
                    Perform_SendConsoleMessage($"Invalid Channel ID '{channelId}'.", ConsoleColor.Red);
                    return;
                }

                var message = await channel.SendMessageAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: components != null ? components.Build() : null);
                if (message != null)
                {
                    if (saveMessage)
                        SaveCustomMessage(message);
                    Event_CustomMessageReceived(customId, message);
                }
            }
            else
                throw new Exception("Failed to send a custom message to channel because BOT is not connected!");
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while sending a custom message: '{ex.Message}'", ConsoleColor.Red);
        }
    }

    public void SendRespondToMessage(ulong messageId, ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components)
    {
        EmbedBuilder? embedToBuild = new();
        ComponentBuilder componentsToBuild = new();

        if (embed != null)
        {
            embedToBuild = GetEmbedBuilder(embed);
        }
        if (components != null)
        {
            componentsToBuild = GetComponentsBuilder(components);
        }

        _ = SendRespondToMessageAsync(messageId, channelId, content, embedToBuild, componentsToBuild);
    }

    public async Task SendRespondToMessageAsync(ulong messageId, ulong channelId, string? content, EmbedBuilder embed, ComponentBuilder? components)
    {
        try
        {
            if (BotClient != null)
            {
                if (BotClient.GetChannel(channelId) is not IMessageChannel channel)
                {
                    Perform_SendConsoleMessage($"Invalid Channel ID '{channelId}'.", ConsoleColor.Red);
                    return;
                }
                var message = await channel.GetMessageAsync(messageId) as IUserMessage;
                if (message != null)
                {
                    await message.ReplyAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: components != null ? components.Build() : null);
                    if (IsDebug)
                        Perform_SendConsoleMessage($"Respond message was sent to channel ID '{channelId}' and responded to message ID '{messageId}'", ConsoleColor.Cyan);
                }
                else
                {
                    Perform_SendConsoleMessage($"Message with ID '{messageId}' was not found! (SendRespondToMessage)", ConsoleColor.Red);
                }
            }
            else
                throw new Exception("Failed to send respond to message because BOT is not connected!");
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while sending a response message: '{ex.Message}'", ConsoleColor.Red);
        }
    }

    public void UpdateMessage(ulong messageId, ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components)
    {
        EmbedBuilder? embedToBuild = new();
        ComponentBuilder componentsToBuild = new();

        if (embed != null)
        {
            embedToBuild = GetEmbedBuilder(embed);
        }
        if (components != null)
        {
            componentsToBuild = GetComponentsBuilder(components);
        }
        _ = UpdateMessageAsync(messageId, channelId, content, embedToBuild, componentsToBuild);
    }

    public async Task UpdateMessageAsync(ulong messageId, ulong channelId, string? content, EmbedBuilder embed, ComponentBuilder? components)
    {
        try
        {
            if (BotClient != null)
            {
                if (BotClient.GetChannel(channelId) is not IMessageChannel channel)
                {
                    Perform_SendConsoleMessage($"Invalid Channel ID '{channelId}'.", ConsoleColor.Red);
                    return;
                }

                var message = await channel.GetMessageAsync(messageId) as IUserMessage;
                if (message != null)
                {
                    await message.ModifyAsync(msg =>
                    {
                        msg.Content = string.IsNullOrEmpty(content) ? "" : content;
                        msg.Embed = IsEmbedValid(embed) ? embed.Build() : null;
                        msg.Components = components != null ? components.Build() : null;
                    });
                    if (IsDebug)
                        Perform_SendConsoleMessage($"Message with ID '{messageId}' has been successfully updated", ConsoleColor.Cyan);
                }
                else
                {
                    Perform_SendConsoleMessage($"Message with ID '{messageId}' was not found! (UpdateMessage)", ConsoleColor.Red);
                }
            }
            else
                throw new Exception("Failed to update message because BOT is not connected!");
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while updating message: '{ex.Message}'", ConsoleColor.Red);
        }
    }

    public void SendRespondMessageToInteraction(int interactionId, string? content, Embeds.Builder? embed, Components.Builder? components, bool deleteOriginalInteraction = false, bool silent = true)
    {
        EmbedBuilder? embedToBuild = new();
        ComponentBuilder componentsToBuild = new();

        if (embed != null)
        {
            embedToBuild = GetEmbedBuilder(embed);
        }
        if (components != null)
        {
            componentsToBuild = GetComponentsBuilder(components);
        }
        if (savedInteractions.ContainsKey(interactionId))
        {
            _ = SendRespondMessageToInteractionAsync(interactionId, content, embedToBuild, componentsToBuild, deleteOriginalInteraction, silent);
        }
    }
    public async Task SendRespondMessageToInteractionAsync(int interactionId, string? content, EmbedBuilder embed, ComponentBuilder? components, bool deleteOriginalInteraction, bool silent)
    {
        try
        {
            if (!savedInteractions.TryGetValue(interactionId, out var interaction))
            {
                Perform_SendConsoleMessage($"Interaction with ID '{interactionId}' was not found or has expired!", ConsoleColor.Red);
                return;
            }

            if (deleteOriginalInteraction)
            {
                await interaction.DeferAsync();
                await interaction.DeleteOriginalResponseAsync();
                await interaction.FollowupAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: components != null ? components.Build() : null, ephemeral: silent);
            }
            else
            {
                await interaction.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: components != null ? components.Build() : null, ephemeral: silent);
            }

            savedInteractions.TryRemove(interactionId, out _);

            if (IsDebug)
                Perform_SendConsoleMessage($"Respond message to Interaction was sent to interaction with ID '{interactionId}'", ConsoleColor.Cyan);
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while sending a Response message to Interaction: '{ex.Message}'", ConsoleColor.Red);
        }
    }

    public void SendRespondMessageToSlashCommand(int interactionId, string? content, Embeds.Builder? embed, Components.Builder? components, bool silent = true)
    {
        EmbedBuilder? embedToBuild = new();
        ComponentBuilder componentsToBuild = new();

        if (embed != null)
        {
            embedToBuild = GetEmbedBuilder(embed);
        }
        if (components != null)
        {
            componentsToBuild = GetComponentsBuilder(components);
        }
        if (savedInteractions.ContainsKey(interactionId))
        {
            _ = SendRespondMessageToSlashCommandAsync(interactionId, content, embedToBuild, componentsToBuild, silent);
        }
    }
    public async Task SendRespondMessageToSlashCommandAsync(int interactionId, string? content, EmbedBuilder embed, ComponentBuilder? components, bool silent)
    {
        try
        {
            if (!savedInteractions.TryGetValue(interactionId, out var interaction))
            {
                Perform_SendConsoleMessage($"Interaction with ID '{interactionId}' was not found or has expired!", ConsoleColor.Red);
                return;
            }
            await interaction.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: components != null ? components.Build() : null, ephemeral: silent);
            savedInteractions.TryRemove(interactionId, out _);
            if (IsDebug)
                Perform_SendConsoleMessage($"Respond message to Slash Command was sent to interaction with ID '{interactionId}'", ConsoleColor.Cyan);
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while sending a Response message to Slash Command: '{ex.Message}'", ConsoleColor.Red);
        }
    }

    public void SendRespondModalToInteraction(int interactionId, DiscordUtilitiesAPI.Builders.Modal.Builder Modal)
    {
        var modalToBuild = GetModalBuilder(Modal);
        try
        {
            if (!savedInteractions.TryGetValue(interactionId, out var interaction))
            {
                Perform_SendConsoleMessage($"Interaction with ID '{interactionId}' was not found or has expired!", ConsoleColor.Red);
                return;
            }
            _ = interaction.RespondWithModalAsync(modalToBuild.Build());
            savedInteractions.TryRemove(interactionId, out _);
            if (IsDebug)
                Perform_SendConsoleMessage($"Respond modal to Interaction was sent to interaction with ID '{interactionId}'", ConsoleColor.Cyan);
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while sending a Response modal to Interaction: '{ex.Message}'", ConsoleColor.Red);
        }
    }

    public void SendRespondModalToSlashCommand(int interactionId, DiscordUtilitiesAPI.Builders.Modal.Builder Modal)
    {
        var modalToBuild = GetModalBuilder(Modal);
        try
        {
            if (!savedInteractions.TryGetValue(interactionId, out var interaction))
            {
                Perform_SendConsoleMessage($"Interaction with ID '{interactionId}' was not found or has expired!", ConsoleColor.Red);
                return;
            }
            _ = interaction.RespondWithModalAsync(modalToBuild.Build());
            savedInteractions.TryRemove(interactionId, out _);
            if (IsDebug)
                Perform_SendConsoleMessage($"Respond modal to Slash Command was sent to interaction with ID '{interactionId}'", ConsoleColor.Cyan);
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while sending a Response modal to Slash Command: '{ex.Message}'", ConsoleColor.Red);
        }
    }

    public bool SendDirectMessage(ulong userId, string? content, Embeds.Builder? embed, Components.Builder? components)
    {
        try
        {
            var user = BotClient?.GetUser(userId);
            if (user == null)
            {
                Perform_SendConsoleMessage($"'SendDirectMessage' - User with ID '{userId}' was not found on the Discord server!", ConsoleColor.DarkYellow);
                return false;
            }

            EmbedBuilder? embedToBuild = new();
            ComponentBuilder componentsToBuild = new();

            if (embed != null)
            {
                embedToBuild = GetEmbedBuilder(embed);
            }
            if (components != null)
            {
                componentsToBuild = GetComponentsBuilder(components);
            }

            _ = user.SendMessageAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embedToBuild) ? embedToBuild.Build() : null, components: components != null ? componentsToBuild.Build() : null);
            return true;
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            Perform_SendConsoleMessage($"User with ID '{userId}' has disabled or blocked direct messages.", ConsoleColor.DarkYellow);
            return false;
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while sending a Direct message: '{ex.Message}'", ConsoleColor.Red);
            return false;
        }
    }

    public void SendConsoleMessage(string text, DiscordUtilitiesAPI.Helpers.MessageType type)
    {
        Perform_SendConsoleMessage(text, (ConsoleColor)type);
    }
}