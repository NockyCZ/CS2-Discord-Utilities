using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace ManageRolesAndPermissions;

public class Config : BasePluginConfig
{
    [JsonPropertyName("Remove Roles On Permission Loss")] public bool removeRolesOnPermissionLoss { get; set; } = false;
    [JsonPropertyName("Role To Permission")]
    public Dictionary<string, string> RoleToPermission { get; set; } = new Dictionary<string, string>()
    {
        ["123456789"] = "@discord_utilities/testflag",
        ["987654321"] = "#discord_utilities/testgroup"
    };

    [JsonPropertyName("Permission To Role")]
    public Dictionary<string, string> PermissionToRole { get; set; } = new Dictionary<string, string>()
    {
        ["@discord_utilities/testflag"] = "123456789",
        ["#discord_utilities/testgroup"] = "987654321"
    };
}
