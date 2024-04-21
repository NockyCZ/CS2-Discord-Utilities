using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Builders;
using DiscordUtilitiesAPI.Helpers;

namespace DiscordUtilities;
public partial class DiscordUtilities : IDiscordUtilitiesAPI
{
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
                Perform_SendConsoleMessage($"[Discord Utilities] Guild with id '{ServerId}' was not found!", ConsoleColor.Red);
                throw new Exception($"Guild with id '{ServerId}' was not found!");
            }
            var role = guild.GetRole(roleId);
            if (role == null)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] Role with id '{roleId}' was not found! (RemoveAllUsersFromRole)", ConsoleColor.Red);
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
                Perform_SendConsoleMessage($"[Discord Utilities] Role with id '{roleId}' has been cleared", ConsoleColor.Cyan);
        }
        catch (Exception ex)
        {
            Perform_SendConsoleMessage($"[Discord Utilities] An error occurred while removing all users from the role: {ex.Message}", ConsoleColor.Red);
        }
    }

    public void RegisterNewSlashCommand(Commands.Builder command)
    {
        var commandData = command.commandData;
        var customCommand = new SlashCommandBuilder()
            .WithName(commandData.Name.ToLower())
            .WithDescription(commandData.Description);

        var commandOptions = command.commandOptions;
        if (commandOptions.Count() == 0)
            throw new Exception("Failed to create Slash Command because command options is empty!");

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
        _ = Perform_RegisterNewSlashCommand(customCommand);
    }

    public async Task Perform_RegisterNewSlashCommand(SlashCommandBuilder command)
    {
        try
        {
            if (BotClient != null)
            {
                await BotClient.CreateGlobalApplicationCommandAsync(command.Build());
            }
            else
                throw new Exception("Failed to create Slash Command because BOT is not connected!");
        }
        catch (HttpException ex)
        {
            Perform_SendConsoleMessage($"[Discord Utilities] An error occurred while updating API Slash Commands: {ex.Message}", ConsoleColor.Red);
            throw new Exception($"An error occurred while updating API Slash Commands: {ex.Message}");
        }
    }

    public void SendMessageToChannel(ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components)
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
        if (BotClient?.GetChannel(channelId) is not IMessageChannel channel)
        {
            Perform_SendConsoleMessage($"[Discord Utilities] Invalid Channel ID '{channelId}'.", ConsoleColor.Red);
            throw new Exception($"Invalid Channel ID '{channelId}'");
        }
        _ = channel.SendMessageAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embedToBuild) ? embedToBuild.Build() : null, components: componentsToBuild != null ? componentsToBuild.Build() : null);
    }

    public async Task SendCustomMessageToChannelAsync(string customId, ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components)
    {
        try
        {
            if (BotClient != null)
            {
                if (BotClient.GetChannel(channelId) is not IMessageChannel channel)
                {
                    Perform_SendConsoleMessage($"[Discord Utilities] Invalid Channel ID '{channelId}'.", ConsoleColor.Red);
                    throw new Exception($"Invalid Channel ID '{channelId}'");
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
                var message = await channel.SendMessageAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embedToBuild) ? embedToBuild.Build() : null, components: componentsToBuild != null ? componentsToBuild.Build() : null);
                if (message != null)
                    Event_CustomMessageReceived(customId, message);
            }
            else
                throw new Exception("Failed to get message builders because BOT is not connected!");
        }
        catch (HttpException ex)
        {
            Perform_SendConsoleMessage($"[Discord Utilities] An error occurred while retrieving the builders: {ex.Message}", ConsoleColor.Red);
            throw new Exception($"An error occurred while retrieving the builders: {ex.Message}");
        }
    }

    public void SendCustomMessageToChannel(string customId, ulong channelId, string? content, Embeds.Builder? embed, Components.Builder? components)
    {
        _ = SendCustomMessageToChannelAsync(customId, channelId, content, embed, components);

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

        _ = Perform_SendRespondToMessage(messageId, channelId, content != null ? content : "", embedToBuild, componentsToBuild);
    }

    public async Task Perform_SendRespondToMessage(ulong messageId, ulong channelId, string content, EmbedBuilder embed, ComponentBuilder components)
    {
        try
        {
            if (BotClient != null)
            {
                if (BotClient.GetChannel(channelId) is not IMessageChannel channel)
                {
                    Perform_SendConsoleMessage($"[Discord Utilities] Invalid Channel ID '{channelId}'.", ConsoleColor.Red);
                    throw new Exception($"Invalid Channel ID '{channelId}'");
                }
                var message = await channel.GetMessageAsync(messageId) as IUserMessage;
                if (message != null)
                {
                    await message.ReplyAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: components.Build().Components.Count() > 0 ? components.Build() : null);
                }
                else
                {
                    Perform_SendConsoleMessage($"[Discord Utilities] Message with ID '{messageId}' was not found!", ConsoleColor.Red);
                }
            }
            else
                throw new Exception("Failed to send respond to message because BOT is not connected!");
        }
        catch (HttpException ex)
        {
            Perform_SendConsoleMessage($"[Discord Utilities] SendRespondToMessage ERROR: {ex.Message}", ConsoleColor.Red);
            throw new Exception($"SendRespondToMessage ERROR: {ex.Message}");
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
        _ = Perform_UpdateMessage(messageId, channelId, content != null ? content : "", embedToBuild, componentsToBuild);
    }

    public async Task Perform_UpdateMessage(ulong messageId, ulong channelId, string content, EmbedBuilder embed, ComponentBuilder components)
    {
        try
        {
            if (BotClient != null)
            {
                if (BotClient.GetChannel(channelId) is not IMessageChannel channel)
                {
                    Perform_SendConsoleMessage($"[Discord Utilities] Invalid Channel ID '{channelId}'.", ConsoleColor.Red);
                    throw new Exception($"Invalid Channel ID '{channelId}'");
                }

                var message = await channel.GetMessageAsync(messageId) as IUserMessage;
                if (message != null)
                {
                    await message.ModifyAsync(msg =>
                    {
                        msg.Content = string.IsNullOrEmpty(content) ? "" : content;
                        msg.Embed = IsEmbedValid(embed) ? embed.Build() : null;
                        msg.Components = components.Build().Components.Count() > 0 ? components.Build() : null;
                    });
                }
                else
                {
                    Perform_SendConsoleMessage($"[Discord Utilities] Message with ID '{messageId}' was not found!", ConsoleColor.Red);
                }
            }
            else
                throw new Exception("Failed to update message because BOT is not connected!");
        }
        catch (HttpException ex)
        {
            Perform_SendConsoleMessage($"[Discord Utilities] UpdateMessage ERROR: {ex.Message}", ConsoleColor.Red);
            throw new Exception($"UpdateMessage ERROR: {ex.Message}");
        }
    }

    public void SendRespondMessageToInteraction(int interactionId, string? content, Embeds.Builder? embed, Components.Builder? components, bool silent = true)
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
            var interaction = savedInteractions[interactionId];
            _ = interaction.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embedToBuild) ? embedToBuild.Build() : null, components: componentsToBuild != null ? componentsToBuild.Build() : null, ephemeral: silent);
            savedInteractions.Remove(interactionId);
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
        if (savedCommandInteractions.ContainsKey(interactionId))
        {
            var interaction = savedCommandInteractions[interactionId];
            _ = interaction.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embedToBuild) ? embedToBuild.Build() : null, components: componentsToBuild != null ? componentsToBuild.Build() : null, ephemeral: silent);
            savedCommandInteractions.Remove(interactionId);
        }
    }

    public void SendConsoleMessage(string text, DiscordUtilitiesAPI.Helpers.MessageType type)
    {
        Perform_SendConsoleMessage(text, (ConsoleColor)type);
    }
}