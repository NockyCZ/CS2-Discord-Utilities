namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public class ServerData
        {
            public required string GameDirectory { get; set; }
            public required string Name { get; set; }
            public required string MaxPlayers { get; set; }
            public required string MapName { get; set; }
            public required string OnlinePlayers { get; set; }
            public required string OnlinePlayersAndBots { get; set; }
            public required string OnlineBots { get; set; }
            public required string Timeleft { get; set; }
            public required string IP { get; set; }
        }

        public class PlayerData
        {
            public required string Name { get; set; }
            public required string NameWithoutEmoji { get; set; }
            public required string SteamId32 { get; set; }
            public required string SteamId64 { get; set; }
            public required string IpAddress { get; set; }
            public required string CommunityUrl { get; set; }
            /*public required string TeamShortName { get; set; }
            public required string TeamLongName { get; set; }
            public required string TeamNumber { get; set; }
            public required string Kills { get; set; }
            public required string Deaths { get; set; }
            public required string Assists { get; set; }
            public required string Points { get; set; }*/
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