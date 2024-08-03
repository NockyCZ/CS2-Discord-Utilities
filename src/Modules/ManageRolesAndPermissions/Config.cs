using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;
using static ManageRolesAndPermissions.ManageRolesAndPermissions;

namespace ManageRolesAndPermissions;

public class Config : BasePluginConfig
{
    [JsonPropertyName("Remove Roles On Permission Loss")] public bool removeRolesOnPermissionLoss { get; set; } = false;
    [JsonPropertyName("Role To Permission")]
    public Dictionary<string, RoleGroupData> RoleToPermission { get; set; } = new Dictionary<string, RoleGroupData>()
    {
        ["123456789"] = new RoleGroupData
        {
            flags = { "@css/test", "@css/admin" },
            immunity = 100,
            command_overrides = { { "css_yourcommand", true } }
        },
        ["987654321"] = new RoleGroupData
        {
            flags = { "@css/ban", "@css/kick" },
            immunity = 90,
            command_overrides = { { "css_slay", true } }
        }
    };

    [JsonPropertyName("Permission To Role")]
    public Dictionary<string, string> PermissionToRole { get; set; } = new Dictionary<string, string>()
    {
        ["@discord_utilities/testflag"] = "123456789",
        ["#discord_utilities/testgroup"] = "987654321"
    };
}
