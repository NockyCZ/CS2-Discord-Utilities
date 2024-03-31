using CounterStrikeSharp.API;
using Discord.WebSocket;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public void PerformChatRelay(SocketGuildUser user, SocketMessage message)
        {
            string messageFormat = ReplaceDiscordUserVariables(user, Config.DiscordRelay.MessageFormat);
            messageFormat = ReplaceDiscordChannelVariables(message, messageFormat);
            messageFormat = ReplaceColors(messageFormat);
            Server.PrintToChatAll(messageFormat);
        }
    }
}