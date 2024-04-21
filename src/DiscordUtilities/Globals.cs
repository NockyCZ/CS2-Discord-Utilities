using CounterStrikeSharp.API.Core.Capabilities;
using Discord.Commands;
using Discord.WebSocket;
using DiscordUtilitiesAPI;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public static PluginCapability<IDiscordUtilitiesAPI> DiscordUtilitiesAPI { get; } = new("discord_utilities");
        public DUConfig Config { get; set; } = null!;
        public static DiscordSocketClient? BotClient;
        public static CommandService? BotCommands;
        public static IServiceProvider? BotServices;
        public static DatabaseConnection? databaseData;
        public static ServerData? serverData;
        public static bool IsBotConnected;
        public static bool IsDbConnected;
        public static bool IsDebug;
        public static string ServerId = "";
    }
}