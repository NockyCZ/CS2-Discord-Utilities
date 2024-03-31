using CounterStrikeSharp.API.Core;
using Discord;
using Discord.WebSocket;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public ComponentBuilder GetServerStatusComponents(ComponentBuilder componentsBuilder)
        {
            if (Config.ServerStatus.ServerStatusEmbed.FirstComponent == 1)
            {
                if (Config.ServerStatus.ServerStatusEmbed.JoinButton.Enabled)
                {
                    var replacedText = ReplaceServerDataVariables(Config.ServerStatus.ServerStatusEmbed.JoinButton.Text);
                    var replacedURL = ReplaceServerDataVariables(Config.ServerStatus.ServerStatusEmbed.JoinButton.URL);
                    var button = new ButtonBuilder()
                        .WithLabel(replacedText)
                        .WithStyle(ButtonStyle.Link)
                        .WithUrl(replacedURL);

                    if (!string.IsNullOrEmpty(Config.ServerStatus.ServerStatusEmbed.JoinButton.Emoji))
                    {
                        IEmote emote = Emote.Parse(Config.ServerStatus.ServerStatusEmbed.JoinButton.Emoji);
                        button.WithEmote(emote);
                    }
                    componentsBuilder.WithButton(button);
                }
                if (playerData.Count() > 0 && Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.Enabled)
                {
                    var menuBuilder = new SelectMenuBuilder()
                        .WithPlaceholder(Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.MenuName)
                        .WithCustomId("serverstatus-players")
                        .WithMinValues(1)
                        .WithMaxValues(1);

                    foreach (var p in playerData!)
                    {
                        if (p.Key == null || !p.Key.IsValid || p.Key.AuthorizedSteamID == null)
                            continue;

                        string replacedLabel = ReplacePlayerDataVariables(Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.PlayersFormat, p.Key.AuthorizedSteamID.SteamId64);
                        menuBuilder.AddOption(label: replacedLabel, value: p.Key.AuthorizedSteamID.SteamId64.ToString());
                    }
                    componentsBuilder.WithSelectMenu(menuBuilder);
                }
            }
            else
            {
                if (playerData.Count() > 0 && Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.Enabled)
                {
                    var menuBuilder = new SelectMenuBuilder()
                        .WithPlaceholder(Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.MenuName)
                        .WithCustomId("serverstatus-players")
                        .WithMinValues(1)
                        .WithMaxValues(1);

                    foreach (var p in playerData!)
                    {
                        if (p.Key == null || !p.Key.IsValid || p.Key.AuthorizedSteamID == null)
                            continue;

                        string replacedLabel = ReplacePlayerDataVariables(Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.PlayersFormat, p.Key.AuthorizedSteamID.SteamId64);
                        menuBuilder.AddOption(label: replacedLabel, value: p.Key.AuthorizedSteamID.SteamId64.ToString());
                    }
                    componentsBuilder.WithSelectMenu(menuBuilder);
                }
                if (Config.ServerStatus.ServerStatusEmbed.JoinButton.Enabled)
                {
                    var replacedText = ReplaceServerDataVariables(Config.ServerStatus.ServerStatusEmbed.JoinButton.Text);
                    var replacedURL = ReplaceServerDataVariables(Config.ServerStatus.ServerStatusEmbed.JoinButton.URL);
                    var button = new ButtonBuilder()
                        .WithLabel(replacedText)
                        .WithStyle(ButtonStyle.Link)
                        .WithUrl(replacedURL);

                    if (!string.IsNullOrEmpty(Config.ServerStatus.ServerStatusEmbed.JoinButton.Emoji))
                    {
                        IEmote emote = Emote.Parse(Config.ServerStatus.ServerStatusEmbed.JoinButton.Emoji);
                        button.WithEmote(emote);
                    }
                    componentsBuilder.WithButton(button);
                }
            }
            return componentsBuilder;
        }
        public void SelectMenuResponse(SocketMessageComponent component)
        {
            IDiscordInteractionData data = component.Data;
            if (data is IComponentInteractionData componentData)
            {
                var selectedValues = componentData.Values;
                string[] selectedPlayer = new string[1];
                selectedPlayer[0] = selectedValues.FirstOrDefault()!;
                var content = GetContent(ContentTypes.ServerStatus_Player, selectedPlayer);
                var embed = GetEmbed(EmbedTypes.ServerStatus_Player, selectedPlayer);
                _ = component.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
            }
        }
        public async Task PerformFirstServerStatus(ComponentBuilder components, bool addComponents)
        {
            if (BotClient?.GetChannel(ulong.Parse(Config.ServerStatus.ChannelID)) is not IMessageChannel channel)
            {
                SendConsoleMessage($"[Discord Utilities] Invalid Channel ID '{Config.ServerStatus.ChannelID}' in the Server Status Section!", ConsoleColor.Red);
                return;
            }

            string[] data = new string[1];
            var embed = GetEmbed(EmbedTypes.ServerStatus, data);
            var content = GetContent(ContentTypes.ServerStatus, data);

            var sentMessage = await channel.SendMessageAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: addComponents ? components.Build() : null);
            UpdateDiscordChannelID(sentMessage.Id.ToString());
        }

        public async Task UpdateServerStatus(ComponentBuilder components, bool addComponents)
        {
            if (BotClient?.GetChannel(ulong.Parse(Config.ServerStatus.ChannelID)) is not IMessageChannel channel)
            {
                SendConsoleMessage($"[Discord Utilities] Invalid Channel ID '{Config.ServerStatus.ChannelID}' in Server Status.", ConsoleColor.Red);
                return;
            }
            var status = await channel.GetMessageAsync(ulong.Parse(Config.ServerStatus.MessageID)) as IUserMessage;
            if (status != null)
            {
                string[] data = new string[1];
                var embed = GetEmbed(EmbedTypes.ServerStatus, data);
                var content = GetContent(ContentTypes.ServerStatus, data);
                await status.ModifyAsync(msg =>
                {
                    msg.Content = string.IsNullOrEmpty(content) ? null : content;
                    msg.Embed = IsEmbedValid(embed) ? embed.Build() : null;
                    msg.Components = addComponents ? components.Build() : null;
                });
            }
            else
            {
                SendConsoleMessage($"[Discord Utilities] Invalid Message ID '{Config.ServerStatus.MessageID}' in Server Status.", ConsoleColor.Red);
            }
        }
    }
}