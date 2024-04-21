using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace ServerStatus;

public class Config : BasePluginConfig
{
    [JsonPropertyName("Update Time")] public int UpdateTimer { get; set; } = 0;
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Message ID")] public string MessageID { get; set; } = "";
    [JsonPropertyName("Server Status Embed")] public ServerStatusEmbed ServerStatusEmbed { get; set; } = new ServerStatusEmbed();
}

public class ServerStatusEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "{Server.Name}";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> IP: `{Server.IP}`\n> Timeleft: `{Server.Timeleft}`";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "👥 Players;{Server.OnlinePlayers}/{Server.MaxPlayers};True|🗺️ Map;{Server.MapName};True";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "https://i.imgur.com/uZfZ0sr.png";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ffad33";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "Last update";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = true;
    [JsonPropertyName("Join Button")] public JoinButton JoinButton { get; set; } = new JoinButton();
}

public class JoinButton
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Button Text")] public string Text { get; set; } = "Join Server";
    [JsonPropertyName("Button URL")] public string URL { get; set; } = "LINK TO CONNECT TO YOUR CS2 SERVER";
    [JsonPropertyName("Button Emoji")] public string Emoji { get; set; } = "";
}