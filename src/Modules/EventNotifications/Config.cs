using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace EventNotifications;

public class Config : BasePluginConfig
{
    [JsonPropertyName("Player Connect")] public PlayerConnect PlayerConnect { get; set; } = new();
    [JsonPropertyName("Player Disconnect")] public PlayerDisconnect PlayerDisconnect { get; set; } = new();
    [JsonPropertyName("Player Death")] public PlayerDeath PlayerDeath { get; set; } = new();
    [JsonPropertyName("Map Changed")] public MapChanged MapChanged { get; set; } = new();
    [JsonPropertyName("Map End")] public MapEnd MapEnd { get; set; } = new();
    [JsonPropertyName("Match End Stats")] public MatchEndStats MatchEndStats { get; set; } = new();
}
public class MapEnd
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Map End Stats Embed")] public MapEndEmbed MapEndEmbed { get; set; } = new();
}

public class MatchEndStats
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Map End Stats Embed")] public MatchEndEmbed MatchEndEmbed { get; set; } = new();
}

public class MapChanged
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Map Changed Embed")] public MapChangedEmbed MapChangedEmbed { get; set; } = new();
}
public class PlayerConnect
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Player Connect Embed")] public ConnectedEmbed ConnectedEmbed { get; set; } = new();
}
public class PlayerDisconnect
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Player Disconnect Embed")] public DisconnectdEmbed DisconnectdEmbed { get; set; } = new();
}

public class PlayerDeath
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Channel ID")] public string ChannelID { get; set; } = "";
    [JsonPropertyName("Player Death Embed")] public DeathEmbed DeathEmbed { get; set; } = new();
}

public class ConnectedEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> Player {Player.CountryEmoji} **[{Player.NameWithoutEmoji}]({Player.CommunityUrl})** (`[Player.PlayedTimeNames]`) has connected to the server.";
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
    [JsonPropertyName("Description")] public string Description { get; set; } = "> Player {Player.CountryEmoji} **[{Player.NameWithoutEmoji}]({Player.CommunityUrl})** (`[Player.PlayedTimeNames]`) has disconnected from the server.";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ff9933";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class DeathEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> Player {Player.CountryEmoji} **[{Player.NameWithoutEmoji}]({Player.CommunityUrl})** (Team: {Player.TeamShortName}) eliminated {Target.CountryEmoji} **[{Target.NameWithoutEmoji}]({Target.CommunityUrl})** (Team: {Target.TeamShortName})";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#9966ff";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class MapChangedEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> Map `{Server.MapName}` has started!";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#ffcc66";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class MapEndEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> Map `{Server.MapName}` has ended!";
    [JsonPropertyName("Fields")] public string Fields { get; set; } = "";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#3333ff";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
}

public class MatchEndEmbed
{
    [JsonPropertyName("Content")] public string Content { get; set; } = "";
    [JsonPropertyName("Title")] public string Title { get; set; } = "Match End Stats";
    [JsonPropertyName("Description")] public string Description { get; set; } = "> :bar_chart: Scoreboard (Players: {Server.OnlinePlayers})";
    [JsonPropertyName("Thumbnail")] public string Thumbnail { get; set; } = "";
    [JsonPropertyName("Image")] public string Image { get; set; } = "";
    [JsonPropertyName("HEX Color")] public string Color { get; set; } = "#9999ff";
    [JsonPropertyName("Footer")] public string Footer { get; set; } = "";
    [JsonPropertyName("Footer Timestamp")] public bool FooterTimestamp { get; set; } = false;
    [JsonPropertyName("Players Stats Format")] public PlayersFormat PlayersFormat { get; set; } = new();
}

public class PlayersFormat
{
    [JsonPropertyName("Team Format")] public PlayersFormat_Teams PlayersFormat_Teams { get; set; } = new();
    [JsonPropertyName("FFA Format")] public PlayersFormat_FFA PlayersFormat_FFA { get; set; } = new();
}


public class PlayersFormat_Teams
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Team CT Format")] public PlayersFormat_Teams_CT PlayersFormat_Teams_CT { get; set; } = new();
    [JsonPropertyName("Team T Format")] public PlayersFormat_Teams_T PlayersFormat_Teams_T { get; set; } = new();
}

public class PlayersFormat_Teams_CT
{
    [JsonPropertyName("Title")] public string Title { get; set; } = "CT Team (Score: {Server.TeamScoreCT})";
    [JsonPropertyName("MVP Player Format")] public string MVPPlayerFormat { get; set; } = "> :crown: **[{Player.NameWithoutEmoji}]({Player.CommunityUrl})**\nKills: `{Player.Kills}` | Deaths: `{Player.Deaths}` | KD: `{Player.KD}`\n";
    [JsonPropertyName("Players Format")] public string PlayersFormat { get; set; } = "> {Player.CountryEmoji} **[{Player.NameWithoutEmoji}]({Player.CommunityUrl})**\nKills: {Player.Kills} | Deaths: {Player.Deaths} | KD: {Player.KD}\n";
}
public class PlayersFormat_Teams_T
{
    [JsonPropertyName("Title")] public string Title { get; set; } = "T Team (Score: {Server.TeamScoreT})";
    [JsonPropertyName("MVP Player Format")] public string MVPPlayerFormat { get; set; } = "> :crown: **[{Player.NameWithoutEmoji}]({Player.CommunityUrl})**\nKills: `{Player.Kills}` | Deaths: `{Player.Deaths}` | KD: `{Player.KD}`\n";
    [JsonPropertyName("Players Format")] public string PlayersFormat { get; set; } = "> {Player.CountryEmoji} **[{Player.NameWithoutEmoji}]({Player.CommunityUrl})**\nKills: {Player.Kills} | Deaths: {Player.Deaths} | KD: {Player.KD}\n";
}

public class PlayersFormat_FFA
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("Title")] public string Title { get; set; } = "All Players:";
    [JsonPropertyName("MVP Player Format")] public string MVPPlayerFormat { get; set; } = "> :crown: **[{Player.NameWithoutEmoji}]({Player.CommunityUrl})**\nKills: `{Player.Kills}` | Deaths: `{Player.Deaths}` | KD: `{Player.KD}`\n";
    [JsonPropertyName("Players Format")] public string PlayersFormat { get; set; } = "> {Player.CountryEmoji} **[{Player.NameWithoutEmoji}]({Player.CommunityUrl})**\nKills: {Player.Kills} | Deaths: {Player.Deaths} | KD: {Player.KD}\n";
}