using CounterStrikeSharp.API.Core;
using Discord;
using Discord.WebSocket;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
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
        public async Task PerformFirstServerStatus(ComponentBuilder components, int playersCount)
        {
            if (BotClient?.GetChannel(ulong.Parse(Config.ServerStatus.ChannelID)) is not IMessageChannel channel)
            {
                SendConsoleMessage($"[Discord Utilities] Invalid Channel ID '{Config.ServerStatus.ChannelID}' in the Server Status Section!", ConsoleColor.Red);
                return;
            }

            string[] data = new string[1];
            var embed = GetEmbed(EmbedTypes.ServerStatus, data);
            var content = GetContent(ContentTypes.ServerStatus, data);

            var sentMessage = await channel.SendMessageAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: playersCount > 0 ? components.Build() : null);
            UpdateDiscordChannelID(sentMessage.Id.ToString());
        }

        public async Task UpdateServerStatus(ComponentBuilder components, int playersCount)
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
                    msg.Components = playersCount > 0 ? components.Build() : null;
                });
            }
            else
            {
                SendConsoleMessage($"[Discord Utilities] Invalid Message ID '{Config.ServerStatus.MessageID}' in Server Status.", ConsoleColor.Red);
            }
        }
    }
}