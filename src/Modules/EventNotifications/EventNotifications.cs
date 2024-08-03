
using System.Data;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Builders;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;

namespace EventNotifications
{
    public class EventNotifications : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "[Discord Utilities] Event Notifications";
        public override string ModuleAuthor => "SourceFactory.eu";
        public override string ModuleVersion => "1.3";
        private IDiscordUtilitiesAPI? DiscordUtilities { get; set; }
        public Config Config { get; set; } = new();
        public bool IsMapEnding;
        public void OnConfigParsed(Config config) { Config = config; }
        public override void OnAllPluginsLoaded(bool hotReload)
        {
            GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers += DiscordUtilitiesEventHandler;
            DiscordUtilities!.CheckVersion(ModuleName, ModuleVersion);
        }
        public override void Load(bool hotReload)
        {
            RegisterListener<Listeners.OnMapStart>(mapName =>
            {
                if (Config.PlayerConnect.DisabledOnMapEnding)
                {
                    IsMapEnding = true;
                    AddTimer(20.0f, () =>
                    {
                        IsMapEnding = false;
                    });
                }
                else
                    IsMapEnding = false;
            });
            RegisterListener<Listeners.OnMapEnd>(() =>
            {
                PerformMapEnd();
            });
        }

        public override void Unload(bool hotReload)
        {
            GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers -= DiscordUtilitiesEventHandler;
        }

        public void PerformMatchEnd()
        {
            if (!Config.MatchEndStats.Enabled)
                return;

            if (string.IsNullOrEmpty(Config.MatchEndStats.ChannelID))
            {
                DiscordUtilities!.SendConsoleMessage("Can't send a message to Discord because the 'Channel ID' is empty! ('Event Notifications (Match End Stats)')", MessageType.Error);
                return;
            }

            var playerList = Utilities.GetPlayers().Where(p => !p.IsBot && !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected && (p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist) && DiscordUtilities!.IsPlayerDataLoaded(p)).ToList();
            if (playerList.Count <= 1)
                return;

            var replaceVariablesBuilder = new ReplaceVariables.Builder()
            {
                ServerData = true
            };
            var config = Config.MatchEndStats.MatchEndEmbed;
            var Embed = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
            var content = DiscordUtilities!.ReplaceVariables(Config.MatchEndStats.MatchEndEmbed.Content, replaceVariablesBuilder);
            string NameFormat;

            if (Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_FFA.Enabled)
            {
                var sb1 = new StringBuilder();
                var sb2 = new StringBuilder();
                var sb3 = new StringBuilder();
                int count = 0;

                foreach (var player in playerList.OrderByDescending(p => p.Score).Take(25))
                {
                    var replacePlayerVariables = new ReplaceVariables.Builder()
                    {
                        PlayerData = player,
                        ServerData = true
                    };

                    if (count == 0 && !string.IsNullOrEmpty(Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_FFA.MVPPlayerFormat))
                        NameFormat = DiscordUtilities!.ReplaceVariables(Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_FFA.MVPPlayerFormat, replacePlayerVariables);
                    else
                        NameFormat = DiscordUtilities!.ReplaceVariables(Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_FFA.PlayersFormat, replacePlayerVariables);

                    if (playerList.Count <= 5)
                    {
                        sb1.Append($"{NameFormat}\n");
                    }
                    else if (playerList.Count <= 15)
                    {
                        if (count < playerList.Count / 2)
                            sb1.Append($"{NameFormat}\n");
                        else
                            sb2.Append($"{NameFormat}\n");
                    }
                    else
                    {
                        if (count < playerList.Count / 3)
                            sb1.Append($"{NameFormat}\n");
                        else if (count < 2 * playerList.Count / 3)
                            sb2.Append($"{NameFormat}\n");
                        else
                            sb3.Append($"{NameFormat}\n");
                    }
                    count++;
                }

                Embed.Fields.Add(
                    new Embeds.FieldsData
                    {
                        Title = DiscordUtilities!.ReplaceVariables(Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_FFA.Title, replaceVariablesBuilder),
                        Description = sb1.Length > 0 ? sb1.ToString() : "** **",
                        Inline = true
                    }
                );

                if (sb2.Length > 0)
                {
                    Embed.Fields.Add(
                        new Embeds.FieldsData
                        {
                            Title = DiscordUtilities!.ReplaceVariables(Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_FFA.Title, replaceVariablesBuilder),
                            Description = sb2.ToString(),
                            Inline = true
                        }
                    );
                }

                if (sb3.Length > 0)
                {
                    Embed.Fields.Add(
                        new Embeds.FieldsData
                        {
                            Title = DiscordUtilities!.ReplaceVariables(Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_FFA.Title, replaceVariablesBuilder),
                            Description = sb3.ToString(),
                            Inline = true
                        }
                    );
                }
            }
            else
            {
                var sb = new StringBuilder();
                int count = 0;
                foreach (var player in playerList.Where(p => p.Team == CsTeam.Terrorist).OrderByDescending(p => p.Score).Take(12))
                {
                    var replacePlayerVariables = new ReplaceVariables.Builder()
                    {
                        PlayerData = player,
                        ServerData = true
                    };
                    if (count == 0 && !string.IsNullOrEmpty(Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_Teams.PlayersFormat_Teams_T.MVPPlayerFormat))
                        NameFormat = DiscordUtilities!.ReplaceVariables(Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_Teams.PlayersFormat_Teams_T.MVPPlayerFormat, replacePlayerVariables);
                    else
                        NameFormat = DiscordUtilities!.ReplaceVariables(Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_Teams.PlayersFormat_Teams_T.PlayersFormat, replacePlayerVariables);
                    sb.Append($"{NameFormat}\n");
                    count++;
                }
                Embed.Fields.Add(
                    new Embeds.FieldsData
                    {
                        Title = DiscordUtilities!.ReplaceVariables(Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_Teams.PlayersFormat_Teams_T.Title, replaceVariablesBuilder),
                        Description = sb.Length > 0 ? sb.ToString() : "** **",
                        Inline = true
                    }
                );

                sb = new StringBuilder();
                count = 0;
                foreach (var player in playerList.Where(p => p.Team == CsTeam.CounterTerrorist).OrderByDescending(p => p.Score).Take(12))
                {
                    var replacePlayerVariables = new ReplaceVariables.Builder()
                    {
                        PlayerData = player,
                        ServerData = true
                    };
                    if (count == 0 && !string.IsNullOrEmpty(Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_Teams.PlayersFormat_Teams_CT.MVPPlayerFormat))
                        NameFormat = DiscordUtilities!.ReplaceVariables(Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_Teams.PlayersFormat_Teams_CT.MVPPlayerFormat, replacePlayerVariables);
                    else
                        NameFormat = DiscordUtilities!.ReplaceVariables(Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_Teams.PlayersFormat_Teams_CT.PlayersFormat, replacePlayerVariables);
                    sb.Append($"{NameFormat}\n");
                    count++;
                }
                Embed.Fields.Add(
                    new Embeds.FieldsData
                    {
                        Title = DiscordUtilities!.ReplaceVariables(Config.MatchEndStats.MatchEndEmbed.PlayersFormat.PlayersFormat_Teams.PlayersFormat_Teams_CT.Title, replaceVariablesBuilder),
                        Description = sb.Length > 0 ? sb.ToString() : "** **",
                        Inline = true
                    }
                );
            }
            DiscordUtilities.SendMessageToChannel(ulong.Parse(Config.MatchEndStats.ChannelID), content, Embed, null);
        }

        public void PerformMapStart()
        {
            if (!Config.MapChanged.Enabled)
                return;

            if (string.IsNullOrEmpty(Config.MapChanged.ChannelID))
            {
                DiscordUtilities!.SendConsoleMessage("Can't send a message to Discord because the 'Channel ID' is empty! ('Event Notifications (Map Changed)')", MessageType.Error);
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

        public void PerformMapEnd()
        {
            if (!Config.MapEnd.Enabled)
                return;

            if (string.IsNullOrEmpty(Config.MapEnd.ChannelID))
            {
                DiscordUtilities!.SendConsoleMessage("Can't send a message to Discord because the 'Channel ID' is empty! ('Event Notifications (Map End)')", MessageType.Error);
                return;
            }

            var replaceVariablesBuilder = new ReplaceVariables.Builder
            {
                ServerData = true
            };

            var config = Config.MapEnd.MapEndEmbed;
            var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
            var content = DiscordUtilities!.ReplaceVariables(Config.MapEnd.MapEndEmbed.Content, replaceVariablesBuilder);
            DiscordUtilities.SendMessageToChannel(ulong.Parse(Config.MapEnd.ChannelID), content, embedBuider, null);
        }

        [GameEventHandler]
        public HookResult OnMatchEnd(EventCsWinPanelMatch @event, GameEventInfo info)
        {
            IsMapEnding = true;
            PerformMatchEnd();
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if (Config.PlayerDeath.Enabled)
            {
                if (string.IsNullOrEmpty(Config.PlayerDeath.ChannelID))
                {
                    DiscordUtilities!.SendConsoleMessage("Can't send a message to Discord because the 'Channel ID' is empty! ('Event Notifications (Player Death)')", MessageType.Error);
                    return HookResult.Continue;
                }

                var player = @event.Userid;
                var attacker = @event.Attacker;
                if (player != null && attacker != null && player != attacker && DiscordUtilities!.IsPlayerDataLoaded(player) && DiscordUtilities!.IsPlayerDataLoaded(attacker))
                {
                    var replaceVariablesBuilder = new ReplaceVariables.Builder
                    {
                        ServerData = true,
                        PlayerData = attacker,
                        TargetData = player
                    };

                    var config = Config.PlayerDeath.DeathEmbed;
                    var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
                    var content = DiscordUtilities!.ReplaceVariables(Config.PlayerDeath.DeathEmbed.Content, replaceVariablesBuilder);
                    DiscordUtilities.SendMessageToChannel(ulong.Parse(Config.PlayerDeath.ChannelID), content, embedBuider, null);
                }
            }
            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            if (Config.PlayerDisconnect.Enabled)
            {
                if (IsMapEnding && Config.PlayerDisconnect.DisabledOnMapEnding)
                    return HookResult.Continue;

                var player = @event.Userid;
                if (player != null && player.IsValid && DiscordUtilities!.IsPlayerDataLoaded(player))
                {
                    if (string.IsNullOrEmpty(Config.PlayerDisconnect.ChannelID))
                    {
                        DiscordUtilities!.SendConsoleMessage("Can't send a message to Discord because the 'Channel ID' is empty! ('Event Notifications (Player Disconnect)')", MessageType.Error);
                        return HookResult.Continue;
                    }
                    var replaceVariablesBuilder = new ReplaceVariables.Builder
                    {
                        ServerData = true,
                        PlayerData = player
                    };

                    var config = Config.PlayerDisconnect.DisconnectdEmbed;
                    var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
                    var content = DiscordUtilities!.ReplaceVariables(Config.PlayerDisconnect.DisconnectdEmbed.Content, replaceVariablesBuilder);
                    DiscordUtilities.SendMessageToChannel(ulong.Parse(Config.PlayerDisconnect.ChannelID), content, embedBuider, null);
                }
            }
            return HookResult.Continue;
        }
        private void OnPlayerDataLoaded(CCSPlayerController? player)
        {
            if (Config.PlayerConnect.Enabled)
            {
                if (IsMapEnding && Config.PlayerConnect.DisabledOnMapEnding)
                    return;

                if (player != null && player.IsValid && DiscordUtilities!.IsPlayerDataLoaded(player))
                {
                    if (string.IsNullOrEmpty(Config.PlayerConnect.ChannelID))
                    {
                        DiscordUtilities!.SendConsoleMessage("Can't send a message to Discord because the 'Channel ID' is empty! ('Event Notifications (Player Connect)')", MessageType.Error);
                        return;
                    }
                    var replaceVariablesBuilder = new ReplaceVariables.Builder
                    {
                        ServerData = true,
                        PlayerData = player
                    };
                    var config = Config.PlayerConnect.ConnectedEmbed;
                    var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
                    var content = DiscordUtilities!.ReplaceVariables(Config.PlayerConnect.ConnectedEmbed.Content, replaceVariablesBuilder);
                    DiscordUtilities.SendMessageToChannel(ulong.Parse(Config.PlayerConnect.ChannelID), content, embedBuider, null);
                }
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