using CounterStrikeSharp.API.Core.Capabilities;
using Discord.Commands;
using Discord.WebSocket;
using DiscordUtilitiesAPI;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public static CounterStrikeSharp.API.Modules.Timers.Timer? updateTimer;
        public static PluginCapability<IDiscordUtilitiesAPI> DiscordUtilitiesAPI { get; } = new("discord_utilities");
        public DUConfig Config { get; set; } = new();
        public static DiscordSocketClient? BotClient;
        public static CommandService? BotCommands;
        public static IServiceProvider? BotServices;
        public static DatabaseConnection? databaseData;
        public static ServerData serverData = new ServerData();
        public static bool IsBotConnected;
        public static bool IsDbConnected;
        public static bool IsDebug;
        public static bool UseCustomVariables;
        public static string ServerId = "";
        public static string DateFormat = "";
        public static string LastBotActivityText = "";
        public static DateTime LastInteractionTime = DateTime.Now;
    }
}