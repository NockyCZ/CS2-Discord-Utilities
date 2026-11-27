using System.Collections.Concurrent;
using CounterStrikeSharp.API.Core;
using Discord;
using Discord.WebSocket;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        // Written from Discord gateway threads and read on the game thread
        public static ConcurrentDictionary<int, SocketInteraction> savedInteractions = new();
        public static ConcurrentDictionary<ulong, IUserMessage> savedMessages = new();
        public static Dictionary<int, PlayerData> playerData = new();
        public static ConcurrentDictionary<ulong, ulong> linkedPlayers = new();
        public static ConcurrentDictionary<string, string> linkCodes = new();
        public static Dictionary<string, List<ConditionData>> customConditions = new();
        public static Dictionary<string, replaceDataType> customVariables = new();
        public static List<string> mapImagesList = new();
        public static Dictionary<string, string> moduleVersions = new();
    }
}