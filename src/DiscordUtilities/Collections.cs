using CounterStrikeSharp.API.Core;
using Discord;
using Discord.WebSocket;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public static Dictionary<int, SocketInteraction> savedInteractions = new();
        public static Dictionary<ulong, IUserMessage> savedMessages = new();
        public static Dictionary<int, PlayerData> playerData = new();
        public static Dictionary<ulong, ulong> linkedPlayers = new();
        public static Dictionary<string, string> linkCodes = new();
        public static Dictionary<string, List<ConditionData>> customConditions = new();
        public static Dictionary<string, replaceDataType> customVariables = new();
        public static List<string> mapImagesList = new();
    }
}