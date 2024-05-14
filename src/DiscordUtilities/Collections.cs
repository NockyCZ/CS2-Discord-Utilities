using CounterStrikeSharp.API.Core;
using Discord.WebSocket;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public static Dictionary<int, SocketSlashCommand> savedCommandInteractions = new();
        public static Dictionary<int, SocketInteraction> savedInteractions = new();
        public static Dictionary<CCSPlayerController, PlayerData> playerData = new();
        public static Dictionary<ulong, ulong> linkedPlayers = new();
        public static Dictionary<string, string> linkCodes = new();
        public static Dictionary<string, List<ConditionData>> customConditions = new();
        public static Dictionary<string, replaceDataType> customVariables = new();
    }
}