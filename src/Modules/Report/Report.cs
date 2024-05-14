
using System.Drawing;
using System.Net.Mime;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Builders;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;

namespace Report
{
    public partial class Report : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "[Discord Utilities] Report System";
        public override string ModuleAuthor => "SourceFactory.eu";
        public override string ModuleVersion => "1.0.0";
        private IDiscordUtilitiesAPI? DiscordUtilities { get; set; }
        public Config Config { get; set; } = null!;
        public void OnConfigParsed(Config config) { Config = config; }
        public Dictionary<CCSPlayerController, CCSPlayerController> performReport = new();
        public Dictionary<CCSPlayerController, int> reportCooldowns = new();
        public Dictionary<string, ReportData> reportsList = new();
        public class ReportData
        {
            public required CCSPlayerController sender;
            public CCSPlayerController? target;
            public required string reason;
            public required ulong messageId;
        }
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
            CreateCustomCommands();
            AddCommandListener("say", OnPlayerSay, HookMode.Pre);
            AddCommandListener("say_team", OnPlayerSayTeam, HookMode.Pre);
        }
        private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid || player.AuthorizedSteamID == null)
                return HookResult.Continue;

            if (performReport.ContainsKey(player))
            {
                if (info.GetArg(1).Length < Config.ReasonLength)
                {
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer[name: "Chat.ReportShortReason"]}");
                    return HookResult.Handled;
                }
                if (info.GetArg(1).Contains(Config.CancelCommand))
                {
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ReportCancelled"]}");
                    performReport.Remove(player);
                    return HookResult.Handled;
                }
                SendReport(player, performReport[player], info.GetArg(1));
                performReport.Remove(player);
                return HookResult.Handled;
            }
            return HookResult.Continue;
        }
        private HookResult OnPlayerSayTeam(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid || player.AuthorizedSteamID == null)
                return HookResult.Continue;

            if (performReport.ContainsKey(player))
            {
                if (info.GetArg(1).Length < Config.ReasonLength)
                {
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer[name: "Chat.ReportShortReason"]}");
                    return HookResult.Handled;
                }
                if (info.GetArg(1).Contains(Config.CancelCommand))
                {
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ReportCancelled"]}");
                    performReport.Remove(player);
                    return HookResult.Handled;
                }
                SendReport(player, performReport[player], info.GetArg(1));
                performReport.Remove(player);
                return HookResult.Handled;
            }
            return HookResult.Continue;
        }
        public void SendReport(CCSPlayerController sender, CCSPlayerController target, string reason)
        {
            if (reportCooldowns.ContainsKey(sender))
            {
                var remainingTime = (int)Server.CurrentTime - reportCooldowns[sender];
                if (remainingTime < Config.ReportCooldown)
                {
                    sender.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ReportCooldown", Config.ReportCooldown]}");
                    return;
                }
                reportCooldowns.Remove(sender);
            }
            if (Config.ReportMethod != 3)
            {
                if (target == null || !target.IsValid || target.AuthorizedSteamID == null)
                {
                    sender.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.TargetNotConnected"]}");
                    return;
                }
                if (target == sender)
                {
                    sender.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SelfReport"]}");
                    return;
                }
            }

            var replaceVariablesBuilder = new ReplaceVariables.Builder
            {
                ServerData = true,
                PlayerData = sender,
                TargetData = Config.ReportMethod != 3 ? target : sender,
                CustomVariables = new Dictionary<string, string>{
                    { "{REASON}", reason }
                },
            };

            var config = Config.ReportEmbed;
            var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
            var content = DiscordUtilities!.ReplaceVariables(Config.ReportEmbed.Content, replaceVariablesBuilder);

            var reportData = new ReportData()
            {
                sender = sender,
                target = Config.ReportMethod != 3 ? target : sender,
                reason = reason,
                messageId = 0
            };

            string reportId = reportsList.Count() + 1.ToString();
            reportsList.Add(reportId, reportData);

            if (Config.ReportEmbed.ReportButton.Enabled)
                DiscordUtilities!.SendCustomMessageToChannel($"report_{reportId}", ulong.Parse(Config.ChannelID), content, embedBuider, null);
            else
                DiscordUtilities!.SendMessageToChannel(ulong.Parse(Config.ChannelID), content, embedBuider, null);

            reportCooldowns.Add(sender, (int)Server.CurrentTime);
            sender.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ReportSend", target.PlayerName, reason]}");
            foreach (var admin in Utilities.GetPlayers().Where(p => !p.IsBot && !p.IsHLTV && AdminManager.PlayerHasPermissions(p, Config.AdminFlag)))
            {
                admin.PrintToChat(Localizer["Chat.ReportSend", sender.PlayerName, target.PlayerName, reason]);
            }
        }
        public void CustomReasonReport(CCSPlayerController sender, CCSPlayerController target)
        {
            if (!performReport.ContainsKey(sender))
            {
                performReport.Add(sender, target);
            }
            sender.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.InserYourReason"]}");
        }
        private CCSPlayerController? GetTargetByName(string name, CCSPlayerController player)
        {
            int matchingCount = 0;
            CCSPlayerController? target = null;

            foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && p.SteamID.ToString().Length == 17 && !AdminManager.PlayerHasPermissions(p, Config.UnreportableFlag)))
            {
                if (p.PlayerName.Contains(name))
                {
                    target = p;
                    matchingCount++;
                }
            }

            if (matchingCount == 0)
            {
                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.TargetNotFound", name]}");
                return null;
            }
            if (matchingCount > 1)
            {
                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.MultipleTargetFound", name]}");
                return null;
            }

            return target;
        }

        private int GetTargetsForReportCount(CCSPlayerController player)
        {
            return Utilities.GetPlayers().Where(p => p.IsValid && p != player && !p.IsHLTV && !p.IsBot && p.Connected == PlayerConnectedState.PlayerConnected && p.SteamID.ToString().Length == 17 && !AdminManager.PlayerHasPermissions(p, Config.UnreportableFlag)).Count();
        }

        private void OnInteractionCreated(InteractionData interaction, UserData user)
        {
            var CustomId = interaction.CustomId;
            if (CustomId.Contains("report_"))
            {
                if (user.RolesIds.Contains(ulong.Parse(Config.ReportEmbed.ReportButton.AdminRoleId)))
                {
                    CustomId = CustomId.Replace("report_", "");
                    var messageBuilders = interaction.Builders;
                    if (messageBuilders != null && messageBuilders.Embeds != null && messageBuilders.Embeds.Count() > 0)
                    {
                        if (reportsList.ContainsKey(CustomId))
                            reportsList.Remove(CustomId);

                        var replaceVariablesBuilder = new ReplaceVariables.Builder
                        {
                            DiscordUser = user,
                        };

                        var embedsBuilder = messageBuilders.Embeds.FirstOrDefault();
                        if (embedsBuilder != null)
                        {
                            embedsBuilder.Title = string.IsNullOrEmpty(Config.ReportEmbed.ReportButton.UpdatedReportEmbed.Title) ? embedsBuilder.Title : DiscordUtilities!.ReplaceVariables(Config.ReportEmbed.ReportButton.UpdatedReportEmbed.Title, replaceVariablesBuilder);
                            embedsBuilder.ThumbnailUrl = string.IsNullOrEmpty(Config.ReportEmbed.ReportButton.UpdatedReportEmbed.Thumbnail) ? embedsBuilder.ThumbnailUrl : Config.ReportEmbed.ReportButton.UpdatedReportEmbed.Thumbnail;
                            embedsBuilder.ImageUrl = string.IsNullOrEmpty(Config.ReportEmbed.ReportButton.UpdatedReportEmbed.Image) ? embedsBuilder.ImageUrl : Config.ReportEmbed.ReportButton.UpdatedReportEmbed.Image;
                            embedsBuilder.Color = Config.ReportEmbed.ReportButton.UpdatedReportEmbed.Color;
                            embedsBuilder.Footer = string.IsNullOrEmpty(Config.ReportEmbed.ReportButton.UpdatedReportEmbed.Footer) ? embedsBuilder.Footer : DiscordUtilities!.ReplaceVariables(Config.ReportEmbed.ReportButton.UpdatedReportEmbed.Footer, replaceVariablesBuilder);
                            embedsBuilder.FooterTimestamp = Config.ReportEmbed.ReportButton.UpdatedReportEmbed.FooterTimestamp;
                        }
                        var content = messageBuilders.Content;
                        if (content != null)
                            content = string.IsNullOrEmpty(Config.ReportEmbed.ReportButton.UpdatedReportEmbed.Content) ? content : DiscordUtilities!.ReplaceVariables(Config.ReportEmbed.ReportButton.UpdatedReportEmbed.Content, replaceVariablesBuilder);

                        DiscordUtilities!.UpdateMessage(ulong.Parse(CustomId), interaction.ChannelID, content, embedsBuilder, null);

                        var config = Config.ReportEmbed.ReportButton.ReportReplyEmbeds.ReportSucces;
                        embedsBuilder = DiscordUtilities!.GetEmbedBuilderFromConfig(config, null);
                        content = Config.ReportEmbed.ReportButton.ReportReplyEmbeds.ReportSucces.Content;
                        DiscordUtilities.SendRespondMessageToInteraction(interaction.InteractionId, content, embedsBuilder, null);
                    }
                }
                else
                {
                    var config = Config.ReportEmbed.ReportButton.ReportReplyEmbeds.ReportFailed;
                    var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, null);
                    string content = Config.ReportEmbed.ReportButton.ReportReplyEmbeds.ReportFailed.Content;
                    DiscordUtilities.SendRespondMessageToInteraction(interaction.InteractionId, content, embedBuider, null);
                }
            }
        }

        private void DiscordUtilitiesEventHandler(object? _, IDiscordUtilitiesEvent @event)
        {
            switch (@event)
            {
                case InteractionCreated interaction:
                    OnInteractionCreated(interaction.Interaction, interaction.User);
                    break;
                case CustomMessageReceived message:
                    OnCustomMessageReceived(message.CustomID, message.Message);
                    break;
                default:
                    break;
            }
        }
        private void OnCustomMessageReceived(string customID, MessageData message)
        {
            if (customID.Contains("report_"))
            {
                var reportId = customID.Replace("report_", "");
                if (!reportsList.ContainsKey(reportId))
                    return;

                var reportData = reportsList[reportId];
                var newReportData = new ReportData()
                {
                    sender = reportData.sender,
                    target = Config.ReportMethod != 3 ? reportData.target : reportData.sender,
                    reason = reportData.reason,
                    messageId = message.MessageID
                };
                var replaceVariablesBuilder = new ReplaceVariables.Builder
                {
                    ServerData = true,
                    PlayerData = reportData.sender,
                    TargetData = reportData.target,
                    CustomVariables = new Dictionary<string, string>{
                        { "{REASON}", reportData.reason }
                    },
                };
                reportsList.Remove(reportId);
                reportsList.Add(message.MessageID.ToString(), newReportData);

                var config = Config.ReportEmbed;
                var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
                var content = DiscordUtilities!.ReplaceVariables(Config.ReportEmbed.Content, replaceVariablesBuilder);

                var componentsBuilder = new Components.Builder
                {
                    InteractiveButtons = new List<Components.InteractiveButtonsBuilder>
                    {
                        new Components.InteractiveButtonsBuilder
                        {
                            CustomId = $"report_{message.MessageID}",
                            Label = Config.ReportEmbed.ReportButton.Text,
                            Color = (Components.ButtonColor)Config.ReportEmbed.ReportButton.Color,
                            Emoji = DiscordUtilities!.IsValidEmoji(Config.ReportEmbed.ReportButton.Emoji) ? Config.ReportEmbed.ReportButton.Emoji : "",
                        }
                    }
                };
                DiscordUtilities!.UpdateMessage(message.MessageID, message.ChannelID, content, embedBuider, componentsBuilder);
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