
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Builders;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ServerStatus
{
    public partial class ServerStatus : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "[Discord Utilities] Server Status";
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

        private void OnBotLoaded()
        {
            if (Config.UpdateTimer > 29 && !string.IsNullOrEmpty(Config.ChannelID) && !string.IsNullOrEmpty(Config.MessageID))
            {
                if (DiscordUtilities!.Debug())
                    DiscordUtilities.SendConsoleMessage($"[Discord Utilities] Starting Repeateable Timer ({Config.UpdateTimer} secs) for the Server Status update.", MessageType.Debug);

                AddTimer(Config.UpdateTimer, () =>
                {
                    var replaceVariablesBuilder = new ReplaceVariables.Builder
                    {
                        ServerData = true
                    };

                    var config = new ServerStatusEmbed();
                    var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
                    var content = DiscordUtilities!.ReplaceVariables(Config.ServerStatusEmbed.Content, replaceVariablesBuilder);

                    var componentsBuilder = new Components.Builder();
                    if (Config.ServerStatusEmbed.JoinButton.Enabled)
                    {
                        var linkButton = new List<Components.LinkButtonsBuilder>
                        {
                            new Components.LinkButtonsBuilder
                            {
                                Label = Config.ServerStatusEmbed.JoinButton.Text,
                                URL = Config.ServerStatusEmbed.JoinButton.URL,
                                Emoji = DiscordUtilities!.IsValidEmoji(Config.ServerStatusEmbed.JoinButton.Emoji) ? Config.ServerStatusEmbed.JoinButton.Emoji : "",
                            }
                        };
                        componentsBuilder.LinkButtons = linkButton;
                    }

                    DiscordUtilities!.UpdateMessage(ulong.Parse(Config.MessageID), ulong.Parse(Config.ChannelID), content, embedBuider, componentsBuilder);
                }, TimerFlags.REPEAT);
            }
        }
        private void DiscordUtilitiesEventHandler(object? _, IDiscordUtilitiesEvent @event)
        {
            switch (@event)
            {
                case BotLoaded:
                    OnBotLoaded();
                    break;
                case CustomMessageReceived message:
                    OnCustomMessageReceived(message.CustomID, message.Message);
                    break;
                default:
                    break;
            }
        }
        private void OnCustomMessageReceived(string customId, MessageData message)
        {
            if (customId.Equals("serverstatus"))
            {
                string filePath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/DU_ServerStatus/DU_ServerStatus.json";
                try
                {
                    string jsonContent = File.ReadAllText(filePath);
                    JObject jsonObject = JObject.Parse(jsonContent);

                    jsonObject["Message ID"] = message.MessageID.ToString();
                    File.WriteAllText(filePath, jsonObject.ToString(Formatting.Indented));

                    DiscordUtilities!.SendConsoleMessage($"[Discord Utilities] Server Status successfully configured. Message ID has been automatically added ({message.ChannelID})", MessageType.Success);
                    DiscordUtilities.SendConsoleMessage($"[Discord Utilities] Restart the server to load the Server Status correctly!", MessageType.Other);
                }
                catch (Exception ex)
                {
                    DiscordUtilities!.SendConsoleMessage($"[Discord Utilities] An error occurred while creating Server Status Message: {ex.Message}", MessageType.Failed);
                }
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