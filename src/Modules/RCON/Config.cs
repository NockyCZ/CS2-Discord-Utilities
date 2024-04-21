using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace RCON;

public class DUConfig : BasePluginConfig
{
    [JsonPropertyName("Servers List")] public string ServerList { get; set; } = "Only Mirage,Public,AWP";
    [JsonPropertyName("Server")] public string Server { get; set; } = "Public";
    [JsonPropertyName("Admin Role ID")] public string AdminRoleId { get; set; } = "";
    [JsonPropertyName("Discord Rcon Command")] public string CommandName { get; set; } = "rcon";
    [JsonPropertyName("Discord Rcon Command Description")] public string CommandDescription { get; set; } = "Execute commands from the Discord server";
    [JsonPropertyName("Discord Server Option Name")] public string ServerOptionName { get; set; } = "server";
    [JsonPropertyName("Discord Server Option Description")] public string ServerOptionDescription { get; set; } = "On which server the command will be executed";
    [JsonPropertyName("Discord Command Option Name")] public string CommandOptionName { get; set; } = "command";
    [JsonPropertyName("Discord Command Option Description")] public string CommandOptionDescription { get; set; } = "What command will be executed";
    [JsonPropertyName("Command Sent Embed")] public RconReplyEmbed RconReplyEmbed { get; set; } = new RconReplyEmbed();
    [JsonPropertyName("Command Failed Embed")] public RconFailedEmbed RconFailedEmbed { get; set; } = new RconFailedEmbed();
}

public class RconReplyEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> Command `{COMMAND}` was executed on the `{SERVER}` server";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#0099cc";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class RconFailedEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> You do not have access to this command!";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ff3333";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}