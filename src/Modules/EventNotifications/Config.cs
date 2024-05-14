using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace EventNotifications;

public class Config : BasePluginConfig
{
    [JsonPropertyName("Player Connect")] public Connect Connect { get; set; } = new Connect();
    [JsonPropertyName("Player Disconnect")] public Disconnect Disconnect { get; set; } = new Disconnect();
    [JsonPropertyName("Map Changed")] public MapChanged MapChanged { get; set; } = new MapChanged();
}
public class MapChanged
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Map Changed Embed")] public MapChangedEmbed MapChangedEmbed { get; set; } = new MapChangedEmbed();
}
public class Connect
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Connect Embed")] public ConnectedEmbed ConnectedEmbed { get; set; } = new ConnectedEmbed();
}
public class Disconnect
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Connect Embed")] public DisconnectdEmbed DisconnectdEmbed { get; set; } = new DisconnectdEmbed();
}

public class ConnectedEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> Player {Player.CountryEmoji} **[{Player.NameWithoutEmoji}]({Player.CommunityUrl})** (`{Player.PlayedTime}h`) has connected to the server.";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ffff66";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class DisconnectdEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> Player {Player.CountryEmoji} **[{Player.NameWithoutEmoji}]({Player.CommunityUrl})** (`{Player.PlayedTime}h`) has disconnected from the server.";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ff9933";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class MapChangedEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> The `{Server.MapName}` map has just started!";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#993366";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}