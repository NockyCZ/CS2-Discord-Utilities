using CounterStrikeSharp.API.Core;
using DiscordUtilitiesAPI.Helpers;

namespace DiscordUtilitiesAPI.Builders;

public interface ReplaceVariables
{
    public class Builder
    {
        public bool? ServerData { get; set; } = false;
        public CCSPlayerController? PlayerData { get; set; } = null;
        public CCSPlayerController? TargetData { get; set; } = null;
        public MessageData? DiscordChannel { get; set; }
        public UserData? DiscordUser { get; set; }
        public Dictionary<string, string>? CustomVariables { get; set; }
    }
}