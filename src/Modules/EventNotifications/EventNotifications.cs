
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Timers;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Builders;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;

namespace EventNotifications
{
    public class ConnectedPlayersRole : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "[Discord Utilities] Event Notifications";
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

        public void PerformMapStart()
        {
            if (!Config.MapChanged.Enabled)
                return;

            if (string.IsNullOrEmpty(Config.MapChanged.ChannelID))
            {
                DiscordUtilities!.SendConsoleMessage("[Discord Utilities] Discord Event Notifications (Map Changed) ERROR: Can't send a message to Discord because the Channel ID is empty!", MessageType.Error);
                return;
            }
            var replaceVariablesBuilder = new ReplaceVariables.Builder
            {
                ServerData = true
            };

            var config = Config.MapChanged.MapChangedEmbed;
            var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
            var content = DiscordUtilities!.ReplaceVariables(Config.MapChanged.MapChangedEmbed.Content, replaceVariablesBuilder);
            DiscordUtilities.SendMessageToChannel(ulong.Parse(Config.MapChanged.ChannelID), content, embedBuider, null);
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (Config.Disconnect.Enabled && player != null && player.IsValid && DiscordUtilities!.IsPlayerDataLoaded(player))
            {
                if (string.IsNullOrEmpty(Config.Disconnect.ChannelID))
                {
                    DiscordUtilities!.SendConsoleMessage("[Discord Utilities] Discord Event Notifications (Disconnect) ERROR: Can't send a message to Discord because the Channel ID is empty!", MessageType.Error);
                    return HookResult.Continue;
                }
                var replaceVariablesBuilder = new ReplaceVariables.Builder
                {
                    ServerData = true,
                    PlayerData = player
                };

                var config = Config.Disconnect.DisconnectdEmbed;
                var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
                var content = DiscordUtilities!.ReplaceVariables(Config.Disconnect.DisconnectdEmbed.Content, replaceVariablesBuilder);
                DiscordUtilities.SendMessageToChannel(ulong.Parse(Config.Disconnect.ChannelID), content, embedBuider, null);
            }
            return HookResult.Continue;
        }
        private void OnPlayerDataLoaded(CCSPlayerController player)
        {
            if (Config.Connect.Enabled && player != null && player.IsValid && DiscordUtilities!.IsPlayerDataLoaded(player))
            {
                if (string.IsNullOrEmpty(Config.Connect.ChannelID))
                {
                    DiscordUtilities!.SendConsoleMessage("[Discord Utilities] Discord Event Notifications (Connect) ERROR: Can't send a message to Discord because the Channel ID is empty!", MessageType.Error);
                    return;
                }
                var replaceVariablesBuilder = new ReplaceVariables.Builder
                {
                    ServerData = true,
                    PlayerData = player
                };
                var config = Config.Connect.ConnectedEmbed;
                var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
                var content = DiscordUtilities!.ReplaceVariables(Config.Connect.ConnectedEmbed.Content, replaceVariablesBuilder);
                DiscordUtilities.SendMessageToChannel(ulong.Parse(Config.Connect.ChannelID), content, embedBuider, null);
            }
        }

        private void DiscordUtilitiesEventHandler(object? _, IDiscordUtilitiesEvent @event)
        {
            switch (@event)
            {
                case PlayerDataLoaded playerData:
                    OnPlayerDataLoaded(playerData.player);
                    break;
                case ServerDataLoaded:
                    PerformMapStart();
                    break;
                default:
                    break;
            }
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