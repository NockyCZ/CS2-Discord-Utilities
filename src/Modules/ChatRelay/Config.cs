using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace ChatRelay;

public class Config : BasePluginConfig
{
    [JsonPropertyName("Chatlog")] public Chatlog Chatlog { get; set; } = new Chatlog();
    [JsonPropertyName("Admin Chat")] public AdminChat AdminChat { get; set; } = new AdminChat();
    [JsonPropertyName("Discord Relay")] public DiscordRelay DiscordRelay { get; set; } = new DiscordRelay();
}

public class Chatlog
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Blocked Words")] public string BlockedWords { get; set; } = "@everyone,@here";
    [JsonPropertyName("Display Commands")] public bool DisplayCommands { get; set; } = true;
    [JsonPropertyName("Silenced Prefix Emoji")] public string SilencedPrefixEmoji { get; set; } = ":mute:";
    [JsonPropertyName("Gag Prefix Emoji")] public string GagPrefixEmoji { get; set; } = ":no_entry_sign:";
    [JsonPropertyName("Muted Prefix Emoji")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyMutedPrefixEmoji
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
                SilencedPrefixEmoji = value;
        }
    }
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("All Chat Embed")] public AllChatEmbed AllChatEmbed { get; set; } = new AllChatEmbed();
    [JsonPropertyName("Team Chat Embed")] public TeamChatEmbed TeamChatEmbed { get; set; } = new TeamChatEmbed();
}

public class AdminChat
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Admin Flag")] public string AdminFlag { get; set; } = "@css/chat";
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Admin Chat Embed")] public AdminChatEmbed AdminChatEmbed { get; set; } = new AdminChatEmbed();
}

public class AllChatEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "{SILENCE_PREFIX}{GAG_PREFIX}{Player.CountryEmoji} **[{Player.NameWithoutEmoji}](<{Player.CommunityUrl}>)**: {MESSAGE}";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class TeamChatEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "{SILENCE_PREFIX}{GAG_PREFIX}{Player.CountryEmoji} [{Player.TeamShortName}] **[{Player.NameWithoutEmoji}](<{Player.CommunityUrl}>)**: {MESSAGE}";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class AdminChatEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "**[Admin Chat]** **[{Player.NameWithoutEmoji}](<{Player.CommunityUrl}>)**: {MESSAGE}";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class DiscordRelay
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Ingame Message Format")] public string MessageFormat { get; set; } = "{Blue}[{DiscordChannel.Name}] {Green}{DiscordUser.DisplayName}: {Default}{DiscordChannel.Message}";
}
