using CounterStrikeSharp.API;
using Discord.WebSocket;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public List<ulong> relaysList = new List<ulong>();
        public void PerformChatRelay(SocketGuildUser user, SocketMessage message)
        {
            if (relaysList.Contains(message.Id))
                return;

            relaysList.Add(message.Id);

            string messageFormat = ReplaceDiscordUserVariables(user, Config.DiscordRelay.MessageFormat);
            messageFormat = ReplaceDiscordChannelVariables(message, messageFormat);
            messageFormat = ReplaceColors(messageFormat);
            Server.PrintToChatAll(messageFormat);
        }
    }
}