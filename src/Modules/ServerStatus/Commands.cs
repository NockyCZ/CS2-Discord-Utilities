using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using DiscordUtilitiesAPI.Builders;
using DiscordUtilitiesAPI.Helpers;

namespace ServerStatus
{
    public partial class ServerStatus
    {
        [ConsoleCommand("css_du_serverstatus", "Perform and setup the Server Status")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void PerformFirstServerStatus_CMD(CCSPlayerController player, CommandInfo info)
        {
            if (DiscordUtilities == null || !DiscordUtilities.IsBotLoaded())
            {
                info.ReplyToCommand("[Discord Utilities] Discord BOT is not connected!");
                return;
            }
            if (string.IsNullOrEmpty(Config.ChannelID))
            {
                DiscordUtilities.SendConsoleMessage("[Discord Utilities] You do not have a Channel ID set for Server Status.", MessageType.Failed);
                return;
            }
            if (Config.UpdateTimer < 30)
            {
                DiscordUtilities.SendConsoleMessage("[Discord Utilities] You do not have Server Status enabled! The minimum value of Update Time must be more than 30.", MessageType.Failed);
                return;
            }

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
            DiscordUtilities!.SendCustomMessageToChannel("serverstatus", ulong.Parse(Config.ChannelID), content, embedBuider, componentsBuilder);
        }
    }
}