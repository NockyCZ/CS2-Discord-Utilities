using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Discord;
using Discord.WebSocket;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public Dictionary<CCSPlayerController, CCSPlayerController> performReport = new Dictionary<CCSPlayerController, CCSPlayerController>();
        public Dictionary<CCSPlayerController, int> reportCooldowns = new Dictionary<CCSPlayerController, int>();
        public void SendReport(CCSPlayerController sender, CCSPlayerController target, string reason)
        {
            if (reportCooldowns.ContainsKey(sender))
            {
                var remainingTime = (int)Server.CurrentTime - reportCooldowns[sender];
                if (remainingTime < Config.Report.ReportCooldown)
                {
                    sender.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ReportCooldown", Config.Report.ReportCooldown]}");
                    return;
                }
                reportCooldowns.Remove(sender);
            }
            if (Config.Report.ReportMethod != 3)
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

            if (BotClient?.GetChannel(ulong.Parse(Config.Report.ChannelID)) is not IMessageChannel channel)
            {
                SendConsoleMessage($"[Discord Utilities] Invalid Channel ID '{Config.Report.ChannelID}' in the Report Section!", ConsoleColor.Red);
                return;
            }

            string[] data = new string[3];
            data[0] = sender.AuthorizedSteamID!.SteamId64.ToString();
            data[1] = Config.Report.ReportMethod != 3 ? target.AuthorizedSteamID!.SteamId64.ToString() : sender.AuthorizedSteamID!.SteamId64.ToString();
            data[2] = reason;

            EmbedBuilder? embed = GetEmbed(EmbedTypes.Report, data);
            string? content = GetContent(ContentTypes.Report, data);


            _ = SendReportMessage(embed, content);
            reportCooldowns.Add(sender, (int)Server.CurrentTime);
            //channel.SendMessageAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: Config.Report.ReportEmbed.ReportButton.Enabled ? components.Build() : null);
            sender.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.ReportSend", target.PlayerName, reason]}");
        }

        public async Task UpdateReportMessage(SocketGuildUser user, ulong messageId)
        {
            if (BotClient?.GetChannel(ulong.Parse(Config.Report.ChannelID)) is not IMessageChannel channel)
            {
                SendConsoleMessage($"[Discord Utilities] Invalid Channel ID '{Config.Report.ChannelID}' in the Report Section!", ConsoleColor.Red);
                return;
            }

            var report = await channel.GetMessageAsync(messageId) as IUserMessage;
            if (report != null && report.Embeds.Any())
            {
                var embed = report.Embeds.First();
                var editedEmbed = new EmbedBuilder()
                    .WithDescription(embed.Description); ;

                var title = Config.Report.ReportEmbed.ReportButton.UpdatedReportEmbed.Title;
                if (string.IsNullOrEmpty(title))
                {
                    editedEmbed.WithTitle(embed.Title);
                }
                else
                {
                    title = ReplaceDiscordUserVariables(user, title);
                    editedEmbed.WithTitle(title);
                }

                var content = Config.Report.ReportEmbed.ReportButton.UpdatedReportEmbed.Content;
                if (string.IsNullOrEmpty(content))
                {
                    content = report.Content;
                }
                else
                {
                    content = ReplaceDiscordUserVariables(user, content);
                }

                if (string.IsNullOrEmpty(Config.Report.ReportEmbed.ReportButton.UpdatedReportEmbed.Footer))
                    editedEmbed.WithFooter(Config.Report.ReportEmbed.ReportButton.UpdatedReportEmbed.Footer);

                if (Config.Report.ReportEmbed.ReportButton.UpdatedReportEmbed.FooterTimestamp)
                    editedEmbed.WithTimestamp(DateTimeOffset.UtcNow);

                var color = Config.Report.ReportEmbed.ReportButton.UpdatedReportEmbed.Color;
                if (color.StartsWith("#"))
                    color = color.Substring(1);
                editedEmbed.WithColor(new Color(Convert.ToUInt32(color, 16)));

                if (string.IsNullOrEmpty(Config.Report.ReportEmbed.ReportButton.UpdatedReportEmbed.Image))
                    editedEmbed.WithImageUrl(Config.Report.ReportEmbed.ReportButton.UpdatedReportEmbed.Image);

                if (string.IsNullOrEmpty(Config.Report.ReportEmbed.ReportButton.UpdatedReportEmbed.Thumbnail))
                    editedEmbed.WithThumbnailUrl(Config.Report.ReportEmbed.ReportButton.UpdatedReportEmbed.Thumbnail);

                foreach (var field in embed.Fields)
                {
                    editedEmbed.AddField(field.Name, field.Value, field.Inline);
                }

                await report.ModifyAsync(msg =>
                {
                    msg.Content = content;
                    msg.Embed = editedEmbed.Build();
                    msg.Components = null;
                });
            }
        }

        public async Task SendReportMessage(EmbedBuilder embed, string content)
        {
            if (BotClient?.GetChannel(ulong.Parse(Config.Report.ChannelID)) is not IMessageChannel channel)
            {
                SendConsoleMessage($"[Discord Utilities] Invalid Channel ID '{Config.Report.ChannelID}' in the Report Section!", ConsoleColor.Red);
                return;
            }

            var sentMessage = await channel.SendMessageAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, components: null);
            if (Config.Report.ReportEmbed.ReportButton.Enabled)
            {
                var components = new ComponentBuilder();
                var button = new ButtonBuilder()
                    .WithCustomId($"report-{sentMessage.Id}")
                    .WithLabel(Config.Report.ReportEmbed.ReportButton.Text)
                    .WithStyle((ButtonStyle)Config.Report.ReportEmbed.ReportButton.Color);

                if (!string.IsNullOrEmpty(Config.Report.ReportEmbed.ReportButton.Emoji))
                {
                    IEmote emote = Emote.Parse(Config.Report.ReportEmbed.ReportButton.Emoji);
                    button.WithEmote(emote);
                }
                components.WithButton(button);

                await sentMessage.ModifyAsync(msg =>
                {
                    msg.Content = string.IsNullOrEmpty(content) ? null : content;
                    msg.Embed = IsEmbedValid(embed) ? embed.Build() : null;
                    msg.Components = components.Build();
                });
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
    }
}