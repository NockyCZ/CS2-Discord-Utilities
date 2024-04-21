using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace ConnectedPlayersRole;

public class Config : BasePluginConfig
{
    [JsonPropertyName("Role ID")] public string RoleID { get; set; } = "";
}