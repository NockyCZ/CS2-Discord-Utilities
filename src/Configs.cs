using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace DiscordUtilities;

public class DUConfig : BasePluginConfig
{
    [JsonPropertyName("Bot Token")] public string Token { get; set; } = "";
    [JsonPropertyName("Server ID")] public string ServerID { get; set; } = "";
    [JsonPropertyName("Database Connection")] public Database Database { get; set; } = new Database();
    [JsonPropertyName("BOT Status Section")] public BotStatus BotStatus { get; set; } = new BotStatus();
    [JsonPropertyName("Report Section")] public Report Report { get; set; } = new Report();
    [JsonPropertyName("Link Section")] public Link Link { get; set; } = new Link();
    [JsonPropertyName("Ingame Chatlog Section")] public Chatlog Chatlog { get; set; } = new Chatlog();
    [JsonPropertyName("Discord Relay Section")] public DiscordRelay DiscordRelay { get; set; } = new DiscordRelay();
    [JsonPropertyName("Server Status Section")] public ServerStatus ServerStatus { get; set; } = new ServerStatus();
    [JsonPropertyName("Conneced Players Role")] public ConnectedPlayers ConnectedPlayers { get; set; } = new ConnectedPlayers();
    [JsonPropertyName("Event Notifications")] public EventNotifications EventNotifications { get; set; } = new EventNotifications();
    [JsonPropertyName("Manage Roles and Permissions")] public CustomFlagsAndRoles CustomFlagsAndRoles { get; set; } = new CustomFlagsAndRoles();
    [JsonPropertyName("ConfigVersion")] public new int Version { get; set; } = 3;
}

public class Database
{
    [JsonPropertyName("Host")] public string Host { get; set; } = "";
    [JsonPropertyName("Port")] public int Port { get; set; } = 3306;
    [JsonPropertyName("User")] public string User { get; set; } = "";
    [JsonPropertyName("Database")] public string DatabaseName { get; set; } = "";
    [JsonPropertyName("Password")] public string Password { get; set; } = "";
}

public class Report
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Report Commands")] public string ReportCommands { get; set; } = "report,calladmin";
    [JsonPropertyName("Unreportable Flag")] public string UnreportableFlag { get; set; } = "@discord_utilities/antireport";
    [JsonPropertyName("Report Command Method")] public int ReportMethod { get; set; } = 1;
    [JsonPropertyName("Report Reasons")] public string ReportReasons { get; set; } = "#CUSTOMREASON,Cheating,Trolling,AFK";
    [JsonPropertyName("Custom Reason Minimum Length")] public int ReasonLength { get; set; } = 5;
    [JsonPropertyName("Cancel Report Command")] public string CancelCommand { get; set; } = "cancel";
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Report Embed")] public ReportEmbed ReportEmbed { get; set; } = new ReportEmbed();
}

public class ReportEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "New Report (@everyone)";
    [JsonPropertyName("Title")] public string Title { get; set; } = "{Server.Name}";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> From [{Player.Name}]({Player.CommunityUrl}) ({Player.DiscordPing})";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "Reported player;{Target.CountryEmoji} [{Target.Name}]({Target.CommunityUrl}) ({Target.DiscordPing});true|Reason;`{REASON}`;true";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ffff66";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class Link
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Code Length")] public int CodeLength { get; set; } = 7;
    [JsonPropertyName("Ingame Link Commands")] public string IngameCommands { get; set; } = "link,discord";
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
public class Chatlog
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Blocked Words")] public string BlockedWords { get; set; } = "@everyone,@here";
    [JsonPropertyName("All Chat Embed")] public AllChatEmbed AllChatEmbed { get; set; } = new AllChatEmbed();
    [JsonPropertyName("Team Chat Embed")] public TeamChatEmbed TeamChatEmbed { get; set; } = new TeamChatEmbed();
}

public class AllChatEmbed
{
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Content")] public string Content { get; set; } = "{Player.CountryEmoji} **[{Player.NameWithoutEmoji}](<{Player.CommunityUrl}>)**: {MESSAGE}";
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
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Content")] public string Content { get; set; } = "{Player.CountryEmoji} [{Player.TeamShortName}] **[{Player.NameWithoutEmoji}](<{Player.CommunityUrl}>)**: {MESSAGE}";
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
    [JsonPropertyName("Ingame Message Format")] public string MessageFormat { get; set; } = "{Blue}[{Discord.ChannelName}] {Green}{Discord.UserNickName}: {Default}{Discord.Message}";
}

public class ServerStatus
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
    [JsonPropertyName("Description")] public string Description { get; set; } = "> IP: `123.46.78.45:27015`\n> Timeleft: `{Server.Timeleft}`";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "👥 Players;{Server.OnlinePlayers}/{Server.MaxPlayers};True|🗺️ Map;{Server.MapName};True";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "https://i.imgur.com/uZfZ0sr.png";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ffad33";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "Last update";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = true;
    [JsonPropertyName("Players Menu")] public ServerStatusDropdown ServerStatusDropdown { get; set; } = new ServerStatusDropdown();
}

public class ServerStatusDropdown
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Menu Name")] public string MenuName { get; set; } = "Select Players";
    [JsonPropertyName("Players Format")] public string PlayersFormat { get; set; } = "{Player.Name} | {Player.Kills}/{Player.Deaths}";
    [JsonPropertyName("On Click On Player in Menu Embed")] public ServerStatusDropdownClick ServerStatusDropdownClick { get; set; } = new ServerStatusDropdownClick();
}

public class ServerStatusDropdownClick
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "{Player.Name}";
    [JsonPropertyName("Description")] public string Description { get; set; } = "{Player.DiscordPing} Player is in **{Player.TeamLongName}** Team";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "Kills;{Player.Kills};true|Deaths;{Player.Deaths};true|Assists;{Player.Assists};true";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#39e600";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;

}

public class BotStatus
{
    [JsonPropertyName("Update Time")] public int UpdateTimer { get; set; } = 0;
    [JsonPropertyName("Status")] public int Status { get; set; } = 1;
    [JsonPropertyName("Activity Type")] public int ActivityType { get; set; } = 1;
    [JsonPropertyName("Activity Text")] public string ActivityFormat { get; set; } = "{Server.MapName} ({Server.OnlinePlayers}/{Server.MaxPlayers})";
}

public class ConnectedPlayers
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Role ID")] public string Role { get; set; } = "";
}

public class EventNotifications
{
    [JsonPropertyName("Player Connect")] public Connect Connect { get; set; } = new Connect();
    [JsonPropertyName("Player Disconnect")] public Disconnect Disconnect { get; set; } = new Disconnect();
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
    [JsonPropertyName("Description")] public string Description { get; set; } = "> Player {Player.CountryEmoji} **[{Player.NameWithoutEmoji}]({Player.CommunityUrl})** has connected to the server.";
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
    [JsonPropertyName("Description")] public string Description { get; set; } = "> Player {Player.CountryEmoji} **[{Player.NameWithoutEmoji}]({Player.CommunityUrl})** has disconnected from the server.";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ff9933";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class CustomFlagsAndRoles
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;

    [JsonPropertyName("Role To Permission")]
    public Dictionary<string, string> RoleToPermission { get; set; } = new Dictionary<string, string>()
    {
        ["ROLE_ID1"] = "@discord_utilities/flag",
        ["ROLE_ID2"] = "#discord_utilities/group"
    };

    [JsonPropertyName("Permission To Role")]
    public Dictionary<string, string> PermissionToRole { get; set; } = new Dictionary<string, string>()
    {
        ["@discord_utilities/flag"] = "ROLE_ID1",
        ["#discord_utilities/group"] = "ROLE_ID2"
    };
}