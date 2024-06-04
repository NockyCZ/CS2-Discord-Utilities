using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;
using static DiscordUtilities.DiscordUtilities;

namespace DiscordUtilities;

public class DUConfig : BasePluginConfig
{
    [JsonPropertyName("Bot Token")] public string Token { get; set; } = "";
    [JsonPropertyName("Discord Server ID")] public string ServerID { get; set; } = "";
    [JsonPropertyName("Server IP")] public string ServerIP { get; set; } = "0.0.0.0:00000";
    [JsonPropertyName("Use Custom Variables")] public bool UseCustomVariables { get; set; } = true;
    [JsonPropertyName("Date Format")] public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    [JsonPropertyName("Database Connection")] public Database Database { get; set; } = new Database();
    [JsonPropertyName("BOT Status")] public BotStatus BotStatus { get; set; } = new BotStatus();
    [JsonPropertyName("Link System")] public Link Link { get; set; } = new Link();

    [JsonPropertyName("Custom Variables")]
    public Dictionary<string, List<ConditionData>> customConditions { get; set; } = new()
    {
        {
            "Player.DiscordNameWithPing", new List<ConditionData>
            {
                new ConditionData { Value = "{Player.DiscordDisplayName}", Operator = "==", ValueToCheck = "", ReplacementValue = "Not Linked" },
                new ConditionData { ReplacementValue = "{Player.DiscordDisplayName} ({Player.DiscordPing})" }
            }
        },
        {
            "Player.PlayedTimeNames", new List<ConditionData>
            {
                new ConditionData { Value = "{Player.PlayedTime}", Operator = ">", ValueToCheck = "100", ReplacementValue = "Active Player ({Player.PlayedTime}h)" },
                new ConditionData { Value = "{Player.PlayedTime}", Operator = ">", ValueToCheck = "75", ReplacementValue = "Advanced ({Player.PlayedTime}h)" },
                new ConditionData { Value = "{Player.PlayedTime}", Operator = ">=", ValueToCheck = "50", ReplacementValue = "Beginner ({Player.PlayedTime}h)" },
                new ConditionData { ReplacementValue = "Newbie ({Player.PlayedTime}h)" }
            }
        },
        {
            "Server.RemoveMapPrefix", new List<ConditionData>
            {
                new ConditionData { Value = "{Server.MapName}", Operator = "~", ValueToCheck = "de_", ReplacementValue = "{Replace(de_)()}" },
                new ConditionData { Value = "{Server.MapName}", Operator = "~", ValueToCheck = "cs_", ReplacementValue = "{Replace(cs_)()}" }
            }
        },
        {
            "Server.CustomOnlinePlayers", new List<ConditionData>
            {
                new ConditionData { Value = "{Server.OnlinePlayers}", Operator = "!=", ValueToCheck = "0", ReplacementValue = "({Server.OnlinePlayers}/{Server.MaxPlayers})" },
                new ConditionData { ReplacementValue = "(Server Is Empty)" }
            }
        }
    };
    [JsonPropertyName("Debug Messages")] public bool Debug { get; set; } = false;
}

public class Database
{
    [JsonPropertyName("Host")] public string Host { get; set; } = "";
    [JsonPropertyName("Port")] public int Port { get; set; } = 3306;
    [JsonPropertyName("User")] public string User { get; set; } = "";
    [JsonPropertyName("Database")] public string DatabaseName { get; set; } = "";
    [JsonPropertyName("Password")] public string Password { get; set; } = "";
}

public class Link
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Response Server")] public bool ResponseServer { get; set; } = true;
    [JsonPropertyName("Code Length")] public int CodeLength { get; set; } = 7;
    [JsonPropertyName("Ingame Link Commands")] public string IngameLinkCommands { get; set; } = "link,discord";
    [JsonPropertyName("Ingame Unlink Commands")] public string IngameUnlinkCommands { get; set; } = "unlink,logout";
    [JsonPropertyName("Discord Link Command")] public string DiscordCommand { get; set; } = "link";
    [JsonPropertyName("Discord Link Description")] public string DiscordDescription { get; set; } = "Link your Discord profile with your Steam Account";
    [JsonPropertyName("Discord Link Option Description")] public string DiscordOptionDescription { get; set; } = "Insert your link code";
    [JsonPropertyName("Discord Link Option Name")] public string DiscordOptionName { get; set; } = "code";
    [JsonPropertyName("Link Ingame Permissions")] public string LinkPermissions { get; set; } = "@discord_utilities/linked";
    [JsonPropertyName("Link Role ID")] public string LinkRole { get; set; } = "";
    [JsonPropertyName("Link Embed")] public LinkEmbed LinkEmbed { get; set; } = new LinkEmbed();
}

public class LinkEmbed
{
    [JsonPropertyName("Success Embed")] public Success Success { get; set; } = new Success();
    [JsonPropertyName("Failed Embed")] public Failed Failed { get; set; } = new Failed();
    [JsonPropertyName("Already Linked Embed")] public AlreadyLinked AlreadyLinked { get; set; } = new AlreadyLinked();
}
public class Success
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> Your account has been successfully linked to your [Steam account](https://steamcommunity.com/profiles/{STEAM})";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#99ff33";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}
public class Failed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> The account link failed! Because the entered code (`{CODE}`) is not correct.";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ff3333";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class AlreadyLinked
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> Your Discord account is already linked with the [Steam Account](https://steamcommunity.com/profiles/{STEAM})";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#66ffcc";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class BotStatus
{
    [JsonPropertyName("Update Status")] public bool UpdateStatus { get; set; } = true;
    [JsonPropertyName("Status")] public int Status { get; set; } = 1;
    [JsonPropertyName("Activity Type")] public int ActivityType { get; set; } = 0;
    [JsonPropertyName("Activity Text")] public string ActivityFormat { get; set; } = "{Server.MapName} [Server.CustomOnlinePlayers]";
}