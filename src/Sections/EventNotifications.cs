using CounterStrikeSharp.API;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public void PerformMapStart()
        {
            serverData!.MapName = Server.MapName;
            var embedBuiler = GetEmbed(EmbedTypes.MapChanged, new string[1]);
            var content = GetContent(ContentTypes.MapChanged, new string[1]);

            _ = SendDiscordMessage(embedBuiler, content, ulong.Parse(Config.EventNotifications.MapChanged.ChannelID), "Map Changed Event");
        }
        public void PerformConnectEvent(ulong steamid)
        {
            string[] data = new string[1];
            data[0] = steamid.ToString();

            var embedBuiler = GetEmbed(EmbedTypes.Connect, data);
            var content = GetContent(ContentTypes.Connect, data);

            _ = SendDiscordMessage(embedBuiler, content, ulong.Parse(Config.EventNotifications.Connect.ChannelID), "Connect Event");
        }

        public async Task PerformDisconnectEvent(ulong steamid)
        {
            string[] data = new string[1];
            data[0] = steamid.ToString();

            var embedBuiler = GetEmbed(EmbedTypes.Disconnect, data);
            var content = GetContent(ContentTypes.Disconnect, data);

            await SendDiscordMessage(embedBuiler, content, ulong.Parse(Config.EventNotifications.Disconnect.ChannelID), "Disconnect Event");
        }
    }
}