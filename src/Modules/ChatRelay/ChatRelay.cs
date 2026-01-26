
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Reflection;
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
        public override string ModuleVersion => "1.2";
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

            if (message.ChannelID == ulong.Parse(Config.DiscordRelay.ChannelID))
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

            var chatEmbedContent = isTeamMessage ? Config.Chatlog.TeamChatEmbed.Content : Config.Chatlog.AllChatEmbed.Content;
            var silencePrefix = GetSilencePrefix(player);
            var gagPrefix = GetGagPrefix(player);
            var hasPrefixPlaceholder = chatEmbedContent.Contains("{SILENCE_PREFIX}", StringComparison.OrdinalIgnoreCase)
                || chatEmbedContent.Contains("{GAG_PREFIX}", StringComparison.OrdinalIgnoreCase)
                || chatEmbedContent.Contains("{MUTE_PREFIX}", StringComparison.OrdinalIgnoreCase);
            var combinedPrefix = $"{silencePrefix}{gagPrefix}";
            var messageWithPrefix = !string.IsNullOrEmpty(combinedPrefix) && !hasPrefixPlaceholder ? $"{combinedPrefix}{message}" : message;

            var replaceVariablesBuilder = new ReplaceVariables.Builder
            {
                ServerData = true,
                PlayerData = player,
                CustomVariables = new Dictionary<string, string>{
                    { "{MESSAGE}", messageWithPrefix },
                    { "{SILENCE_PREFIX}", silencePrefix },
                    { "{GAG_PREFIX}", gagPrefix },
                    { "{MUTE_PREFIX}", silencePrefix }
                },
            };

            var embedBuider = isTeamMessage ? DiscordUtilities!.GetEmbedBuilderFromConfig(Config.Chatlog.TeamChatEmbed, replaceVariablesBuilder) : DiscordUtilities!.GetEmbedBuilderFromConfig(Config.Chatlog.AllChatEmbed, replaceVariablesBuilder);
            var content = DiscordUtilities!.ReplaceVariables(chatEmbedContent, replaceVariablesBuilder);
            DiscordUtilities.SendMessageToChannel(ulong.Parse(Config.Chatlog.ChannelID), content, embedBuider, null);
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
            DiscordUtilities.SendMessageToChannel(ulong.Parse(Config.AdminChat.ChannelID), content, embedBuider, null);
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

        private string GetSilencePrefix(CCSPlayerController player)
        {
            if (string.IsNullOrWhiteSpace(Config.Chatlog.SilencedPrefixEmoji))
                return string.Empty;

            if (!IsPlayerSilenced(player))
                return string.Empty;

            var prefix = Config.Chatlog.SilencedPrefixEmoji.Trim();
            return string.IsNullOrEmpty(prefix) ? string.Empty : $"{prefix} ";
        }

        private string GetGagPrefix(CCSPlayerController player)
        {
            if (string.IsNullOrWhiteSpace(Config.Chatlog.GagPrefixEmoji))
                return string.Empty;

            if (!IsPlayerGagged(player))
                return string.Empty;

            var prefix = Config.Chatlog.GagPrefixEmoji.Trim();
            return string.IsNullOrEmpty(prefix) ? string.Empty : $"{prefix} ";
        }

        private bool IsPlayerSilenced(CCSPlayerController player)
        {
            if (player == null || !player.IsValid)
                return false;

            try
            {
                return player.VoiceFlags.HasFlag(VoiceFlags.Muted);
            }
            catch
            {
                return false;
            }
        }

        private bool IsPlayerGagged(CCSPlayerController player)
        {
            if (player == null || !player.IsValid)
                return false;

            return HasCommunicationMute(player);
        }

        private bool HasCommunicationMute(CCSPlayerController player)
        {
            bool hasCommAbuse = false;
            ChatIgnoreType_t? ignoreChat = null;
            uint? muteFlagsValue = null;
            bool isSimpleAdminGag = false;

            try
            {
                hasCommAbuse = player.HasCommunicationAbuseMute;
                if (hasCommAbuse)
                    return true;
            }
            catch
            {
                // ignore and continue with other checks
            }

            // Scoreboard/server gag (chat ignore) - non-zero means text is blocked.
            try
            {
                ignoreChat = player.IgnoreGlobalChat;
                if (ignoreChat != ChatIgnoreType_t.CHAT_IGNORE_NONE)
                    return true;
            }
            catch { /* ignore and continue with other checks */ }

            // Some builds expose additional flags/fields for communication blocks.
            try
            {
                var controllerType = player.GetType();
                var muteFlagsProp = controllerType.GetProperty("UiCommunicationMuteFlags") ?? controllerType.GetProperty("CommunicationMuteFlags");
                if (muteFlagsProp != null)
                {
                    var muteFlags = muteFlagsProp.GetValue(player);
                    var isMuted = muteFlags switch
                    {
                        uint flags => (muteFlagsValue = flags) != 0,
                        int flags => (muteFlagsValue = (uint)flags) != 0,
                        _ => false
                    };
                    if (isMuted)
                        return true;
                }

                // SimpleAdmin compatibility: check attached private data object fields named like "gagged"/"muted".
                try
                {
                    var privateDataProp = controllerType.GetProperty("PrivateData");
                    if (privateDataProp != null)
                    {
                        var privateData = privateDataProp.GetValue(player);
                        if (privateData != null)
                        {
                            var pdType = privateData.GetType();
                            // Common SimpleAdmin pattern: Gagged or Muted booleans.
                            var gagField = pdType.GetProperty("Gagged") ?? pdType.GetProperty("IsGagged");
                            var muteField = pdType.GetProperty("Muted") ?? pdType.GetProperty("IsMuted");

                            if (gagField != null)
                            {
                                var gagVal = gagField.GetValue(privateData);
                                if (gagVal is bool b && b)
                                    isSimpleAdminGag = true;
                            }
                            // If only a generic "Muted" exists, treat it as gag for chat relay purposes.
                            if (muteField != null && !isSimpleAdminGag)
                            {
                                var muteVal = muteField.GetValue(privateData);
                                if (muteVal is bool b && b)
                                    isSimpleAdminGag = true;
                            }
                            if (isSimpleAdminGag)
                                return true;
                        }
                    }
                }
                catch
                {
                    // ignore SimpleAdmin reflection errors
                }

                // SimpleAdmin penalty manager (API) check (Gag or Silence).
                try
                {
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    var saAssembly = assemblies.FirstOrDefault(a => (a.GetName()?.Name ?? string.Empty).Contains("SimpleAdmin", StringComparison.OrdinalIgnoreCase));

                    // Find the PlayerPenaltyManager type across loaded assemblies if direct lookup fails.
                    var penaltyManagerType =
                        Type.GetType("CS2_SimpleAdmin.Managers.PlayerPenaltyManager") ??
                        Type.GetType("CS2_SimpleAdmin.Managers.PlayerPenaltyManager, CS2_SimpleAdmin") ??
                        saAssembly?.GetType("CS2_SimpleAdmin.Managers.PlayerPenaltyManager") ??
                        assemblies.SelectMany(a =>
                        {
                            try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                        }).FirstOrDefault(t => t.FullName != null && t.FullName.Contains("PlayerPenaltyManager", StringComparison.OrdinalIgnoreCase));

                    // Find PenaltyType enum
                    var penaltyTypeEnum =
                        Type.GetType("CS2_SimpleAdminApi.PenaltyType") ??
                        Type.GetType("CS2_SimpleAdminApi.PenaltyType, CS2_SimpleAdminApi") ??
                        saAssembly?.GetType("CS2_SimpleAdminApi.PenaltyType") ??
                        assemblies.SelectMany(a =>
                        {
                            try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                        }).FirstOrDefault(t => t.FullName != null && t.FullName.EndsWith("PenaltyType", StringComparison.OrdinalIgnoreCase));

                    var isPenalizedMethod = penaltyManagerType?.GetMethod("IsPenalized", BindingFlags.Public | BindingFlags.Static);

                    if (isPenalizedMethod != null && penaltyTypeEnum != null)
                    {
                        foreach (var label in new[] { "Gag", "Silence" })
                        {
                            if (!Enum.TryParse(penaltyTypeEnum, label, out var penaltyValue))
                                continue;

                            object? endDate = null;
                            var args = new object?[] { player.Slot, penaltyValue, endDate };
                            var result = isPenalizedMethod.Invoke(null, args);
                            if (result is bool penalized && penalized)
                            {
                                isSimpleAdminGag = true;
                                return true;
                            }
                        }
                    }
                }
                catch
                {
                    // ignore SimpleAdmin API errors
                }
            }
            catch
            {
                // ignore and fall through
            }

            return false;
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
