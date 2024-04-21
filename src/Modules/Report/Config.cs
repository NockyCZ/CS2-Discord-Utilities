using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace Report;

public class Config : BasePluginConfig
{
    [JsonPropertyName("Report Commands")] public string ReportCommands { get; set; } = "report,calladmin";
    [JsonPropertyName("Unreportable Flag")] public string UnreportableFlag { get; set; } = "@discord_utilities/antireport";
    [JsonPropertyName("Report Cooldown")] public int ReportCooldown { get; set; } = 60;
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
    [JsonPropertyName("Button Settings")] public ReportButton ReportButton { get; set; } = new ReportButton();
}

public class ReportButton
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Admin Role ID")] public string AdminRoleId { get; set; } = "";
    [JsonPropertyName("Button Color")] public int Color { get; set; } = 1;
    [JsonPropertyName("Button Text")] public string Text { get; set; } = "Mark as solved";
    [JsonPropertyName("Button Emoji")] public string Emoji { get; set; } = "";
    [JsonPropertyName("Button Reply Embeds")] public ReportReplyEmbeds ReportReplyEmbeds { get; set; } = new ReportReplyEmbeds();
    [JsonPropertyName("Modified Report Embed")] public UpdatedReportEmbed UpdatedReportEmbed { get; set; } = new UpdatedReportEmbed();
}

public class UpdatedReportEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "Solved by <@{Discord.UserID}>";
    [JsonPropertyName("Title")] public string Title { get; set; } = "Report solved by {Discord.UserDisplayName}!";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#00ff99";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "Solved at";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = true;
}

public class ReportReplyEmbeds
{
    [JsonPropertyName("Button Success Embed")] public ReportSucces ReportSucces { get; set; } = new ReportSucces();
    [JsonPropertyName("Button Failed Embed")] public ReportFailed ReportFailed { get; set; } = new ReportFailed();
}

public class ReportSucces
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> The report has been successfully marked as solved!";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ff0066";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class ReportFailed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> You don't have enough permissions to mark the report as solved.";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ff3333";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

