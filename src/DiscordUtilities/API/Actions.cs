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
        if (savedMessages.ContainsKey(messageId))
            savedMessages.Remove(messageId);
    }

    public void RemoveAllUsersFromRole(string roleId)
    {
        if (!string.IsNullOrEmpty(roleId))
        {
            _ = RemoveAllUsersFromRoleAsync(ulong.Parse(roleId));
        }
    }
    public async Task RemoveAllUsersFromRoleAsync(ulong roleId)
    {
        try
        {
            var guild = BotClient!.GetGuild(ulong.Parse(ServerId));
            if (guild == null)
            {
                Perform_SendConsoleMessage($"Guild with id '{ServerId}' was not found!", ConsoleColor.Red);
                throw new Exception($"Guild with id '{ServerId}' was not found!");
            }
            var role = guild.GetRole(roleId);
            if (role == null)
            {
                Perform_SendConsoleMessage($"Role with id '{roleId}' was not found! (RemoveAllUsersFromRole)", ConsoleColor.Red);
                throw new Exception($"Role with id '{roleId}' was not found!");
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
            throw new Exception($"An error occurred while updating API Slash Commands: {ex.Message}");
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
                    throw new Exception($"Invalid Channel ID '{channelId}'");
                }
                await channel.SendMessageAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: components != null ? components.Build() : null);
                if (IsDebug)
                    Perform_SendConsoleMessage($"The message was successfully sent to the channel with ID '{channelId}'", ConsoleColor.Cyan);
            }
            else
            {
                Perform_SendConsoleMessage("Failed to send a message to channel because BOT is not connected!", ConsoleColor.Red);
                throw new Exception("Failed to send a message to channel because BOT is not connected!");
            }
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while sending a message: '{ex.Message}'", ConsoleColor.Red);
            throw new Exception($"An error occurred while sending a message: {ex.Message}");
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
                    throw new Exception($"Invalid Channel ID '{channelId}'");
                }

                var message = await channel.SendMessageAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: components != null ? components.Build() : null);
                if (message != null)
                {
                    Event_CustomMessageReceived(customId, message);
                    if (saveMessage)
                    {
                        if (!savedMessages.ContainsKey(message.Id))
                            savedMessages.Add(message.Id, message);
                    }
                }
            }
            else
                throw new Exception("Failed to send a custom message to channel because BOT is not connected!");
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while sending a custom message: '{ex.Message}'", ConsoleColor.Red);
            throw new Exception($"An error occurred while sending a custom message: {ex.Message}");
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
                    throw new Exception($"Invalid Channel ID '{channelId}'");
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
            throw new Exception($"An error occurred while sending a response message: {ex.Message}");
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
                    throw new Exception($"Invalid Channel ID '{channelId}'");
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
            throw new Exception($"An error occurred while updating message: {ex.Message}");
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
            var interaction = savedInteractions[interactionId];

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

            savedInteractions.Remove(interactionId);

            if (IsDebug)
                Perform_SendConsoleMessage($"Respond message to Interaction was sent to interaction with ID '{interactionId}'", ConsoleColor.Cyan);
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while sending a Response message to Interaction: '{ex.Message}'", ConsoleColor.Red);
            throw new Exception($"An error occurred while sending a Response message to Interaction: {ex.Message}");
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
            var interaction = savedInteractions[interactionId];
            await interaction.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: components != null ? components.Build() : null, ephemeral: silent);
            savedInteractions.Remove(interactionId);
            if (IsDebug)
                Perform_SendConsoleMessage($"Respond message to Slash Command was sent to interaction with ID '{interactionId}'", ConsoleColor.Cyan);
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while sending a Response message to Slash Command: '{ex.Message}'", ConsoleColor.Red);
            throw new Exception($"An error occurred while sending a Response message to Slash Command: {ex.Message}");
        }
    }

    public void SendRespondModalToInteraction(int interactionId, DiscordUtilitiesAPI.Builders.Modal.Builder Modal)
    {
        var modalToBuild = GetModalBuilder(Modal);
        try
        {
            var interaction = savedInteractions[interactionId];
            _ = interaction.RespondWithModalAsync(modalToBuild.Build());
            savedInteractions.Remove(interactionId);
            if (IsDebug)
                Perform_SendConsoleMessage($"Respond modal to Interaction was sent to interaction with ID '{interactionId}'", ConsoleColor.Cyan);
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while sending a Response modal to Interaction: '{ex.Message}'", ConsoleColor.Red);
            throw new Exception($"An error occurred while sending a Response modal to Interaction: {ex.Message}");
        }
    }

    public void SendRespondModalToSlashCommand(int interactionId, DiscordUtilitiesAPI.Builders.Modal.Builder Modal)
    {
        var modalToBuild = GetModalBuilder(Modal);
        try
        {
            var interaction = savedInteractions[interactionId];
            _ = interaction.RespondWithModalAsync(modalToBuild.Build());
            savedInteractions.Remove(interactionId);
            if (IsDebug)
                Perform_SendConsoleMessage($"Respond modal to Slash Command was sent to interaction with ID '{interactionId}'", ConsoleColor.Cyan);
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"An error occurred while sending a Response modal to Slash Command: '{ex.Message}'", ConsoleColor.Red);
            throw new Exception($"An error occurred while sending a Response modal to Slash Command: {ex.Message}");
        }
    }

    public bool SendDirectMessage(ulong userId, string? content, Embeds.Builder? embed, Components.Builder? components)
    {
        try
        {
            var user = BotClient!.GetUser(userId);
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
            throw new Exception($"An error occurred while sending a Direct message: {ex.Message}");
        }
    }

    public void SendConsoleMessage(string text, DiscordUtilitiesAPI.Helpers.MessageType type)
    {
        Perform_SendConsoleMessage(text, (ConsoleColor)type);
    }
}