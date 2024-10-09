using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace Report;

public class Config : BasePluginConfig
{
    [JsonPropertyName("Report Commands")] public string ReportCommands { get; set; } = "report,calladmin";
    [JsonPropertyName("Reports List Commands")] public string ReportsListCommands { get; set; } = "reports,reportslist";
    [JsonPropertyName("Blocked Words In Reason")] public string BlockedReason { get; set; } = "idiot,test,rtv,nominate";
    [JsonPropertyName("Allow Ingame Reports List")] public bool ReportsListMenu { get; set; } = true;
    [JsonPropertyName("Allow Self Report")] public bool SelfReport { get; set; } = false;
    [JsonPropertyName("Report Expiration")] public int ReportExpiration { get; set; } = 120;
    [JsonPropertyName("Anti Report Spam")] public bool AntiSpamReport { get; set; } = true;
    [JsonPropertyName("Send Message To Sender On Solved")] public bool SendMessageOnSolved { get; set; } = true;
    [JsonPropertyName("Admin Flag")] public string AdminFlag { get; set; } = "@discord_utilities/report";
    [JsonPropertyName("Unreportable Flag")] public string UnreportableFlag { get; set; } = "@discord_utilities/antireport";
    [JsonPropertyName("Report Cooldown")] public int ReportCooldown { get; set; } = 60;
    [JsonPropertyName("Report Command Method")] public int ReportMethod { get; set; } = 1;
    [JsonPropertyName("Report Reasons")] public string ReportReasons { get; set; } = "#CUSTOMREASON,Cheating,Trolling,AFK";
    [JsonPropertyName("Custom Reason Minimum Length")] public int ReasonLength { get; set; } = 5;
    [JsonPropertyName("Cancel Report Command")] public string CancelCommand { get; set; } = "cancel";
    [JsonPropertyName("Date Time Format")] public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Report Embed")] public ReportEmbed ReportEmbed { get; set; } = new ReportEmbed();
    [JsonPropertyName("Solved Embeds")] public SolvedEmbeds SolvedEmbeds { get; set; } = new SolvedEmbeds();
}

public class SolvedEmbeds
{
    [JsonPropertyName("Discord Solved Report Embed")] public SolvedReportEmbed SolvedReportEmbed { get; set; } = new SolvedReportEmbed();
    [JsonPropertyName("Ingame Solved Report Embed")] public IngameSolvedReportEmbed IngameSolvedReportEmbed { get; set; } = new IngameSolvedReportEmbed();
    [JsonPropertyName("Expired Report Embed")] public ExpiredReportEmbed ExpiredReportEmbed { get; set; } = new ExpiredReportEmbed();
}

public class ReportEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "New Report (@everyone)";
    [JsonPropertyName("Title")] public string Title { get; set; } = "{Server.Name}";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> From [{Player.Name}]({Player.CommunityUrl}) [Player.DiscordNameWithPing]";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "Reported player;{Target.CountryEmoji} [{Target.Name}]({Target.CommunityUrl}) (First Join: `{Target.FirstJoin}`);true|Reason;`{REASON}`;true";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ffff66";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
    [JsonPropertyName("Admin Button")] public ReportButton ReportButton { get; set; } = new();
    [JsonPropertyName("Player Stats Button")] public SearchPlayerButton SearchPlayerButton { get; set; } = new();
    [JsonPropertyName("Player Punishments Button")] public BanlistButton BanlistButton { get; set; } = new();
}

public class BanlistButton
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Button Text")] public string Text { get; set; } = "Player Punishments";
    [JsonPropertyName("Button Color")] public int Color { get; set; } = 4;
    [JsonPropertyName("Button Emoji")] public string Emoji { get; set; } = ":no_entry_sign:";
}

public class SearchPlayerButton
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Server Name")] public string ServerName { get; set; } = "Public";
    [JsonPropertyName("Button Text")] public string Text { get; set; } = "Player Stats";
    [JsonPropertyName("Button Color")] public int Color { get; set; } = 3;
    [JsonPropertyName("Button Emoji")] public string Emoji { get; set; } = ":mag:";
}

public class ReportButton
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Admin Roles ID")] public string AdminRolesId { get; set; } = "";
    [JsonPropertyName("Button Color")] public int Color { get; set; } = 1;
    [JsonPropertyName("Button Text")] public string Text { get; set; } = "Mark as resolved";
    [JsonPropertyName("Button Emoji")] public string Emoji { get; set; } = ":white_check_mark:";
    [JsonPropertyName("Button Reply Embeds")] public ReportReplyEmbeds ReportReplyEmbeds { get; set; } = new ReportReplyEmbeds();
}

public class SolvedReportEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "resolved by <@{DiscordUser.ID}>";
    [JsonPropertyName("Title")] public string Title { get; set; } = "Report resolved by {DiscordUser.DisplayName}!";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#00ff99";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "Resolved at";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = true;
}

public class IngameSolvedReportEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "Report resolved on **{Server.Name}** by {Player.DiscordPing}";
    [JsonPropertyName("Title")] public string Title { get; set; } = "Report resolved by {Player.Name}(`Discord:` {Player.DiscordDisplayName})";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#00ff99";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "Resolved at";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = true;
}

public class ExpiredReportEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "Report Expired! ({Server.Name})";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#99ff33";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "Expired at";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = true;
}

public class ReportReplyEmbeds
{
    [JsonPropertyName("Button Success Embed")] public ReportSucces ReportSucces { get; set; } = new ReportSucces();
    [JsonPropertyName("Button Failed Embed")] public ReportFailed ReportFailed { get; set; } = new ReportFailed();
}

public class ReportSucces
{
    [JsonPropertyName("Silent Response")] public bool SilentResponse { get; set; } = true;
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> The report has been successfully marked as resolved!";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ff0066";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class ReportFailed
{
    [JsonPropertyName("Silent Response")] public bool SilentResponse { get; set; } = true;
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> You don't have enough permissions to mark the report as resolved!";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ff3333";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

