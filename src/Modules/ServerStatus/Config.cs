using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace ServerStatus;

public class Config : BasePluginConfig
{
    [JsonPropertyName("Update Time")] public int UpdateTimer { get; set; } = 0;
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Message ID")] public string MessageID { get; set; } = "";
    [JsonPropertyName("Server Status Embed")] public ServerStatusEmbed ServerStatusEmbed { get; set; } = new();
}

public class ServerStatusEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "{Server.Name}";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> IP: `{Server.IP}`\n> Timeleft: `{Server.Timeleft}`";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "👥 Players;{Server.OnlinePlayers}/{Server.MaxPlayers};True|🗺️ Map;{Server.MapName};True";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "{Server.MapImageUrl}";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ffad33";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "Last update";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = true;
    [JsonPropertyName("Buttons")] public Buttons Buttons { get; set; } = new();
}

public class Buttons
{
    [JsonPropertyName("Join Button")] public JoinButton JoinButton { get; set; } = new();
    [JsonPropertyName("Banlist Button")] public BanlistButton BanlistButton { get; set; } = new();
    [JsonPropertyName("Leaderboard Button")] public LeaderboardButton LeaderboardButton { get; set; } = new();
    [JsonPropertyName("Search Player Button")] public SearchPlayerButton SearchPlayerButton { get; set; } = new();
}

public class JoinButton
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Button Text")] public string Text { get; set; } = "Join Server";
    [JsonPropertyName("Button URL")] public string URL { get; set; } = "{Server.JoinUrl}";
    [JsonPropertyName("Button Emoji")] public string Emoji { get; set; } = ":rocket:";
}

public class LeaderboardButton
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Server Name")] public string ServerName { get; set; } = "Public";
    [JsonPropertyName("Button Text")] public string Text { get; set; } = "Leaderboard";
    [JsonPropertyName("Button Color")] public int Color { get; set; } = 3;
    [JsonPropertyName("Button Emoji")] public string Emoji { get; set; } = ":trophy:";
}

public class SearchPlayerButton
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Server Name")] public string ServerName { get; set; } = "Public";
    [JsonPropertyName("Button Text")] public string Text { get; set; } = "Search Players Stats";
    [JsonPropertyName("Button Color")] public int Color { get; set; } = 1;
    [JsonPropertyName("Button Emoji")] public string Emoji { get; set; } = ":bar_chart:";
}

public class BanlistButton
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Button Text")] public string Text { get; set; } = "Banlist";
    [JsonPropertyName("Button Color")] public int Color { get; set; } = 4;
    [JsonPropertyName("Button Emoji")] public string Emoji { get; set; } = ":no_entry_sign:";
}