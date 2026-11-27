using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Builders;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;

namespace RCON
{
    public class RCON : BasePlugin, IPluginConfig<DUConfig>
    {
        public override string ModuleName => "[Discord Utilities] RCON";
        public override string ModuleAuthor => "SourceFactory.eu";
        public override string ModuleVersion => "1.3";
        private IDiscordUtilitiesAPI? DiscordUtilities { get; set; }
        public DUConfig Config { get; set; } = new();
        public void OnConfigParsed(DUConfig config) { Config = config; }
        public override void OnAllPluginsLoaded(bool hotReload)
        {
            GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers += DiscordUtilitiesEventHandler;
            DiscordUtilities!.CheckVersion(ModuleName, ModuleVersion);
        }
        public override void Unload(bool hotReload)
        {
            GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers -= DiscordUtilitiesEventHandler;
        }

        private void DiscordUtilitiesEventHandler(object? _, IDiscordUtilitiesEvent @event)
        {
            switch (@event)
            {
                case BotLoaded:
                    OnBotLoaded();
                    break;
                case SlashCommandExecuted slashCommand:
                    OnSlashCommandExecuted(slashCommand.Command, slashCommand.User);
                    break;
                default:
                    break;
            }
        }
        private void OnSlashCommandExecuted(CommandData command, UserData user)
        {
            if (command.CommandName == Config.CommandName)
            {
                if (DiscordUtilities!.Debug())
                    DiscordUtilities.SendConsoleMessage($"Slash command '{command.CommandName}' has been successfully logged", MessageType.Debug);

                if (!string.IsNullOrEmpty(Config.AdminRolesId))
                {
                    var requiredRoles = Config.AdminRolesId.Trim().Split(',').ToList();
                    if (requiredRoles != null && requiredRoles.Count > 0)
                    {
                        bool hasPermission = requiredRoles.Any(role => ulong.TryParse(role.Trim(), out var roleId) && user.RolesIds.Contains(roleId));
                        if (!hasPermission)
                        {
                            var failedConfig = Config.RconFailedEmbed;
                            var embed = DiscordUtilities!.GetEmbedBuilderFromConfig(failedConfig, null);
                            var failedContent = Config.RconFailedEmbed.Content;
                            DiscordUtilities!.SendRespondMessageToSlashCommand(command.InteractionId, failedContent, embed, null, Config.RconFailedEmbed.SilentResponse);
                            return;
                        }
                    }
                }

                var options = command.OptionsData;
                string?[] data = new string?[2];
                foreach (var option in options)
                {
                    if (option.Name == Config.ServerOptionName)
                    {
                        data[1] = option.Value;
                    }
                    else if (option.Name == Config.CommandOptionName)
                    {
                        data[0] = option.Value;
                    }
                }
                var rconCommand = data[0];
                var rconServer = data[1];
                if (string.IsNullOrEmpty(rconCommand) || string.IsNullOrEmpty(rconServer))
                {
                    if (DiscordUtilities!.Debug())
                        DiscordUtilities.SendConsoleMessage("Slash command is missing the command or the server option! (RCON)", MessageType.Debug);
                    return;
                }

                rconServer = rconServer.Trim();
                if (rconServer != Config.Server.Trim())
                {
                    if (!rconServer.Equals("All"))
                    {
                        if (DiscordUtilities!.Debug())
                            DiscordUtilities.SendConsoleMessage($"Command ignored: it was sent to the server '{rconServer}', but 'Server' in the config of this server is '{Config.Server}' (RCON)", MessageType.Debug);
                        return;
                    }
                }

                var replaceVariablesBuilder = new ReplaceVariables.Builder
                {
                    CustomVariables = new Dictionary<string, string>{
                            { "{COMMAND}", rconCommand },
                            { "{SERVER}", rconServer },
                        },
                };
                var config = Config.RconReplyEmbed;
                var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
                var content = DiscordUtilities.ReplaceVariables(Config.RconReplyEmbed.Content, replaceVariablesBuilder);
                content = DiscordUtilities.ReplaceVariables(content, replaceVariablesBuilder);

                Server.ExecuteCommand(rconCommand);
                DiscordUtilities.SendRespondMessageToSlashCommand(command.InteractionId, content, embedBuider, null, Config.RconReplyEmbed.SilentResponse);

            }
        }

        private void OnBotLoaded()
        {
            var commandData = new Commands.SlashCommandData
            {
                Name = Config.CommandName,
                Description = Config.CommandDescription
            };

            var optionChoices = new List<Commands.SlashCommandOptionChoices>();
            string[] Servers = Config.ServerList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!Servers.Contains(Config.Server.Trim()))
                DiscordUtilities?.SendConsoleMessage($"'Server' ('{Config.Server}') is not in 'Servers List' ('{Config.ServerList}'), this server cannot be selected in the Slash Command! (RCON)", MessageType.Error);
            foreach (var server in Servers)
            {
                var choice = new Commands.SlashCommandOptionChoices
                {
                    Name = server,
                    Value = server
                };
                optionChoices.Add(choice);
            }
            if (Servers.Count() > 1)
            {
                optionChoices.Add(new Commands.SlashCommandOptionChoices
                {
                    Name = "All",
                    Value = "All"
                });
            }

            var commandOptions = new List<Commands.SlashCommandOptionsData>
            {
                new Commands.SlashCommandOptionsData
                {
                    Name = Config.ServerOptionName,
                    Description =  Config.ServerOptionDescription,
                    Type = SlashCommandOptionsType.String,
                    Required = true,
                    Choices = optionChoices.Count() > 0 ? optionChoices : null
                },
                new Commands.SlashCommandOptionsData
                {
                    Name = Config.CommandOptionName,
                    Description =  Config.CommandOptionDescription,
                    Type = SlashCommandOptionsType.String,
                    Required = true
                }
            };

            var command = new Commands.Builder
            {
                commandData = commandData,
                commandOptions = commandOptions
            };

            if (DiscordUtilities != null)
                DiscordUtilities.RegisterNewSlashCommand(command);
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