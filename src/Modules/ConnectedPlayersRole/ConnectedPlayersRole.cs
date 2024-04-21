
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;

namespace ConnectedPlayersRole
{
    public class ConnectedPlayersRole : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "[Discord Utilities] Connected Players Role";
        public override string ModuleAuthor => "SourceFactory.eu";
        public override string ModuleVersion => "1.0.0";
        private IDiscordUtilitiesAPI? DiscordUtilities { get; set; }
        public Config Config { get; set; } = null!;
        public void OnConfigParsed(Config config) { Config = config; }
        public override void OnAllPluginsLoaded(bool hotReload)
        {
            GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers += DiscordUtilitiesEventHandler;
        }
        public override void Unload(bool hotReload)
        {
            GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers -= DiscordUtilitiesEventHandler;
        }

        public override void Load(bool hotReload)
        {
            RegisterListener<Listeners.OnMapStart>(mapName =>
            {
                if (DiscordUtilities != null && DiscordUtilities.IsBotLoaded())
                    DiscordUtilities.RemoveAllUsersFromRole(Config.RoleID);
            });
        }

        private void DiscordUtilitiesEventHandler(object? _, IDiscordUtilitiesEvent @event)
        {
            switch (@event)
            {
                case LinkedUserDataLoaded linkedUser:
                    OnLinkedUserDataLoaded(linkedUser.User);
                    break;
                default:
                    break;
            }
        }

        private void OnLinkedUserDataLoaded(UserData user)
        {
            if (string.IsNullOrEmpty(Config.RoleID))
            {
                DiscordUtilities!.SendConsoleMessage("[Discord Utilities] Discord Connected Players Role ERROR: The role could not be added because the Role ID is empty!", MessageType.Error);
                return;
            }
            DiscordUtilities!.AddRolesToUser(user, new List<string>() { Config.RoleID });
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            if (string.IsNullOrEmpty(Config.RoleID))
                return HookResult.Continue;

            var player = @event.Userid;
            if (player != null && player.IsValid && !player.IsBot)
            {
                if (DiscordUtilities!.IsPlayerLinked(player))
                {
                    var user = DiscordUtilities!.GetUserData(player);
                    if (user != null)
                    {
                        DiscordUtilities!.RemoveRolesFromUser(user, new List<string>() { Config.RoleID });
                    }
                }
            }
            return HookResult.Continue;
        }
        private IDiscordUtilitiesAPI GetDiscordUtilitiesEventSender()
        {
            if (DiscordUtilities is not null)
            {
                return DiscordUtilities;
            }

            var DUApi = new PluginCapability<IDiscordUtilitiesAPI>("discord_utilities").Get();
            if (DUApi is null)
            {
                throw new Exception("Couldn't load Discord Utilities plugin");
            }

            DiscordUtilities = DUApi;
            return DUApi;
        }
    }
}