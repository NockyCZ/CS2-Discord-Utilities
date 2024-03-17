using Discord;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public async Task PerformFirstServerStatus()
        {
            if (BotClient?.GetChannel(ulong.Parse(Config.ServerStatus.ChannelID)) is not IMessageChannel channel)
            {
                SendConsoleMessage($"[Discord Utilities] Invalid Channel ID '{Config.ServerStatus.ChannelID}' in the Server Status Section!", ConsoleColor.Red);
                return;
            }
            int totalMenuPlayers = 0;
            var componentsBuilder = new ComponentBuilder();
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
                    totalMenuPlayers++;
                }
                componentsBuilder.WithSelectMenu(menuBuilder);
            }

            string[] data = new string[1];
            var embed = GetEmbed(EmbedTypes.ServerStatus, data);
            var content = GetContent(ContentTypes.ServerStatus, data);

            var sentMessage = await channel.SendMessageAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: totalMenuPlayers > 0 ? componentsBuilder.Build() : null);
            UpdateDiscordChannelID(sentMessage.Id.ToString());
        }

        public async Task UpdateServerStatus()
        {
            if (BotClient?.GetChannel(ulong.Parse(Config.ServerStatus.ChannelID)) is not IMessageChannel channel)
            {
                SendConsoleMessage($"[Discord Utilities] Invalid Channel ID '{Config.ServerStatus.ChannelID}' in Server Status.", ConsoleColor.Red);
                return;
            }
            var status = await channel.GetMessageAsync(ulong.Parse(Config.ServerStatus.MessageID)) as IUserMessage;

            if (status != null)
            {
                int totalMenuPlayers = 0;
                var componentsBuilder = new ComponentBuilder();
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
                        totalMenuPlayers++;
                    }
                    componentsBuilder.WithSelectMenu(menuBuilder);
                }

                string[] data = new string[2];
                var embed = GetEmbed(EmbedTypes.ServerStatus, data);
                var content = GetContent(ContentTypes.ServerStatus, data);
                await status.ModifyAsync(msg =>
                {
                    msg.Content = string.IsNullOrEmpty(content) ? null : content;
                    msg.Embed = IsEmbedValid(embed) ? embed.Build() : null;
                    msg.Components = totalMenuPlayers > 0 ? componentsBuilder.Build() : null;
                });
            }
            else
            {
                SendConsoleMessage($"[Discord Utilities] Invalid Message ID '{Config.ServerStatus.MessageID}' in Server Status.", ConsoleColor.Red);
            }
        }
    }
}