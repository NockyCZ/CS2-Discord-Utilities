
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Builders;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;

namespace ChatRelay
{
    public class ChatRelay : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "[Discord Utilities] Chat Relay";
        public override string ModuleAuthor => "SourceFactory.eu";
        public override string ModuleVersion => "1.3";
        private IDiscordUtilitiesAPI? DiscordUtilities { get; set; }
        public Config Config { get; set; } = new();
        public void OnConfigParsed(Config config) { Config = config; }
        public override void OnAllPluginsLoaded(bool hotReload)
        {
            GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers += DiscordUtilitiesEventHandler;
            DiscordUtilities!.CheckVersion(ModuleName, ModuleVersion);
        }
        public override void Unload(bool hotReload)
        {
            GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers -= DiscordUtilitiesEventHandler;
        }

        public override void Load(bool hotReload)
        {
            AddCommandListener("say", OnPlayerSay, HookMode.Post);
            AddCommandListener("say_team", OnPlayerSayTeam, HookMode.Post);
        }
        private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid || player.AuthorizedSteamID == null || !DiscordUtilities!.IsPlayerDataLoaded(player))
                return HookResult.Continue;

            if (Config.Chatlog.Enabled)
            {
                string msg = info.GetArg(1);
                if ((msg.StartsWith('!') || msg.StartsWith('/')) && !Config.Chatlog.DisplayCommands)
                    return HookResult.Continue;

                string[] blockedWords = Config.Chatlog.BlockedWords.Split(',');
                foreach (var word in blockedWords)
                {
                    if (msg.Contains(word))
                        msg = msg.Replace(word, "");
                }
                if (!string.IsNullOrEmpty(msg))
                    PerformChatlog(player, msg, false);
            }
            return HookResult.Continue;
        }
        private HookResult OnPlayerSayTeam(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid || player.AuthorizedSteamID == null || !DiscordUtilities!.IsPlayerDataLoaded(player))
                return HookResult.Continue;

            string msg = info.GetArg(1);
            if (msg.StartsWith('@') && Config.AdminChat.Enabled && AdminManager.PlayerHasPermissions(player, Config.AdminChat.AdminFlag))
            {
                msg = msg.Replace("@", string.Empty);
                if (!string.IsNullOrEmpty(msg))
                    PerformAdminChatlog(player, msg);
                return HookResult.Handled;
            }
            if (Config.Chatlog.Enabled)
            {
                if ((msg.StartsWith('!') || msg.StartsWith('/')) && !Config.Chatlog.DisplayCommands)
                    return HookResult.Continue;

                string[] blockedWords = Config.Chatlog.BlockedWords.Split(',');
                foreach (var word in blockedWords)
                {
                    if (msg.Contains(word))
                        msg = msg.Replace(word, "");
                }
                if (!string.IsNullOrEmpty(msg))
                    PerformChatlog(player, msg, true);
            }

            return HookResult.Continue;
        }
        private void DiscordUtilitiesEventHandler(object? _, IDiscordUtilitiesEvent @event)
        {
            switch (@event)
            {
                case MessageReceived messageReceived:
                    OnMessageReceived(messageReceived.Message, messageReceived.User);
                    break;
                default:
                    break;
            }
        }
        private void OnMessageReceived(MessageData message, UserData user)
        {
            if (DiscordUtilities!.Debug())
                DiscordUtilities.SendConsoleMessage($"Discord Message '{message.Text}' was logged in channel '{message.ChannelID}' (Chat Relay)", MessageType.Debug);
            if (string.IsNullOrEmpty(Config.DiscordRelay.ChannelID))
                return;

            if (ulong.TryParse(Config.DiscordRelay.ChannelID, out var relayChannelId) && message.ChannelID == relayChannelId)
            {
                var replaceVariablesBuilder = new ReplaceVariables.Builder
                {
                    DiscordChannel = message,
                    DiscordUser = user,
                };

                var messageFormat = DiscordUtilities!.ReplaceVariables(Config.DiscordRelay.MessageFormat, replaceVariablesBuilder);
                messageFormat = ReplaceColors(messageFormat);
                if (!string.IsNullOrEmpty(messageFormat))
                {
                    Server.PrintToChatAll(messageFormat);
                    if (DiscordUtilities.Debug())
                        DiscordUtilities.SendConsoleMessage($"Discord Message '{message.Text}' in channel '{message.ChannelID}' has been sent to the server! (Chat Relay)", MessageType.Debug);
                }
            }
        }

        public void PerformChatlog(CCSPlayerController player, string message, bool isTeamMessage)
        {
            if (string.IsNullOrEmpty(Config.Chatlog.ChannelID))
            {
                DiscordUtilities!.SendConsoleMessage("Can't send a message to Discord because the 'Channel ID' is empty! (Chat Relay (ChatLog))", MessageType.Error);
                return;
            }
            var replaceVariablesBuilder = new ReplaceVariables.Builder
            {
                ServerData = true,
                PlayerData = player,
                CustomVariables = new Dictionary<string, string>{
                    { "{MESSAGE}", message }
                },
            };
            if (isTeamMessage)
            {
                var config = Config.Chatlog.TeamChatEmbed;
                var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
                var content = DiscordUtilities!.ReplaceVariables(Config.Chatlog.TeamChatEmbed.Content, replaceVariablesBuilder);
                if (TryGetChannelId(Config.Chatlog.ChannelID, "Chatlog", out var chatlogChannelId))
                    DiscordUtilities.SendMessageToChannel(chatlogChannelId, content, embedBuider, null);
            }
            else
            {
                var config = Config.Chatlog.AllChatEmbed;
                var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
                var content = DiscordUtilities!.ReplaceVariables(Config.Chatlog.AllChatEmbed.Content, replaceVariablesBuilder);
                if (TryGetChannelId(Config.Chatlog.ChannelID, "Chatlog", out var chatlogChannelId))
                    DiscordUtilities.SendMessageToChannel(chatlogChannelId, content, embedBuider, null);
            }
        }

        public void PerformAdminChatlog(CCSPlayerController player, string message)
        {
            if (string.IsNullOrEmpty(Config.AdminChat.ChannelID))
            {
                DiscordUtilities!.SendConsoleMessage("Can't send a message to Discord because the 'Channel ID' is empty! (Chat Relay (Admin Chat))", MessageType.Error);
                return;
            }

            var replaceVariablesBuilder = new ReplaceVariables.Builder
            {
                ServerData = true,
                PlayerData = player,
                CustomVariables = new Dictionary<string, string>{
                    { "{MESSAGE}", message }
                },
            };

            var config = Config.AdminChat.AdminChatEmbed;
            var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
            var content = DiscordUtilities!.ReplaceVariables(Config.AdminChat.AdminChatEmbed.Content, replaceVariablesBuilder);
            if (TryGetChannelId(Config.AdminChat.ChannelID, "AdminChat", out var adminChatChannelId))
                DiscordUtilities.SendMessageToChannel(adminChatChannelId, content, embedBuider, null);
        }

        private bool TryGetChannelId(string value, string section, out ulong channelId)
        {
            if (ulong.TryParse(value, out channelId))
                return true;

            DiscordUtilities!.SendConsoleMessage($"Invalid 'Channel ID' '{value}'! ({section})", MessageType.Error);
            return false;
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

        private string ReplaceColors(string message)
        {
            var modifiedValue = message;
            foreach (var field in typeof(ChatColors).GetFields())
            {
                string pattern = $"{{{field.Name}}}";
                if (modifiedValue.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    modifiedValue = modifiedValue.Replace(pattern, field.GetValue(null)?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                }
            }
            return modifiedValue.Equals(message) ? message : $" {modifiedValue}";
        }
    }
}