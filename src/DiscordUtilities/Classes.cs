
using CounterStrikeSharp.API.Core;
using DiscordUtilitiesAPI.Helpers;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public enum replaceDataType
        {
            Server,
            Player,
            Target,
            DiscordChannel,
            DiscordUser,
        }
        public class replaceData
        {
            public bool Server { get; set; } = false;
            public CCSPlayerController? Player { get; set; }
            public CCSPlayerController? Target { get; set; }
            public UserData? DiscordUser { get; set; }
            public MessageData? DiscordChannel { get; set; }
        }

        public class ConditionData
        {
            public string Value { get; set; } = "";
            public string Operator { get; set; } = "";
            public string ValueToCheck { get; set; } = "";
            public string ReplacementValue { get; set; } = "";
        }

        public class ServerData
        {
            public string ModuleDirectory { get; set; } = "DiscordUtilities";
            public string GameDirectory { get; set; } = "csgo";
            public string Name { get; set; } = "Counter-Strike 2";
            public string MaxPlayers { get; set; } = "10";
            public string MapName { get; set; } = "de_mirage";
            public string OnlinePlayers { get; set; } = "0";
            public string OnlinePlayersAndBots { get; set; } = "0";
            public string OnlineBots { get; set; } = "0";
            public string Timeleft { get; set; } = "60";
            public string IP { get; set; } = "0.0.0.0:27015";
            public string TeamScoreCT { get; set; } = "0";
            public string TeamScoreT { get; set; } = "0";
        }

        public class PlayerData
        {
            public required string Name { get; set; }
            public required string UserId { get; set; }
            public required string SteamId32 { get; set; }
            public required string SteamId64 { get; set; }
            public required string IpAddress { get; set; }
            public required string CommunityUrl { get; set; }
            public required int PlayedTime { get; set; }
            public required DateTime FirstJoin { get; set; }
            public required DateTime LastSeen { get; set; }
            public required string CountryShort { get; set; }
            public required string CountryLong { get; set; }
            public required string CountryEmoji { get; set; }
            public required string DiscordGlobalname { get; set; }
            public required string DiscordDisplayName { get; set; }
            public required string DiscordPing { get; set; }
            public required string DiscordAvatar { get; set; }
            public required string DiscordID { get; set; }
            public required bool IsLinked { get; set; }
        }

        public class DatabaseConnection
        {
            public required string Server { get; set; }
            public required uint Port { get; set; }
            public required string User { get; set; }
            public required string Database { get; set; }
            public required string Password { get; set; }
        }
    }
}