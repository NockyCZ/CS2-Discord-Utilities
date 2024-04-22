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
        public override string ModuleVersion => "1.0.0";
        private IDiscordUtilitiesAPI? DiscordUtilities { get; set; }
        public DUConfig Config { get; set; } = null!;
        public void OnConfigParsed(DUConfig config) { Config = config; }
        public override void OnAllPluginsLoaded(bool hotReload)
        {
            GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers += DiscordUtilitiesEventHandler;
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
                    DiscordUtilities.SendConsoleMessage($"[Discord Utilities] DEBUG: Slash command '{command.CommandName}' has been successfully logged", MessageType.Debug);

                var options = command.OptionsData;
                string[] data = new string[2];
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
                if (data[1] != Config.Server)
                {
                    if (!data[1].Equals("All"))
                    {
                        if (DiscordUtilities!.Debug())
                            DiscordUtilities.SendConsoleMessage($"[Discord Utilities] DEBUG: This server is not '{data[1]}'!", MessageType.Debug);
                        return;
                    }
                }
                var userRoles = user.RolesIds;
                if (userRoles.Contains(ulong.Parse(Config.AdminRoleId)))
                {
                    var replaceVariablesBuilder = new ReplaceVariables.Builder
                    {
                        CustomVariables = new Dictionary<string, string>{
                            { "{COMMAND}", data[0] },
                            { "{SERVER}", data[1] },
                        },
                    };
                    var config = Config.RconReplyEmbed;
                    var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, replaceVariablesBuilder);
                    var content = DiscordUtilities.ReplaceVariables(Config.RconReplyEmbed.Content, replaceVariablesBuilder);
                    content = DiscordUtilities.ReplaceVariables(content, replaceVariablesBuilder);

                    Server.ExecuteCommand(data[0]);
                    DiscordUtilities.SendRespondMessageToSlashCommand(command.InteractionId, content, embedBuider, null);
                }
                else
                {
                    var config = Config.RconFailedEmbed;
                    var embedBuider = DiscordUtilities!.GetEmbedBuilderFromConfig(config, null);
                    var content = Config.RconReplyEmbed.Content;
                    DiscordUtilities!.SendRespondMessageToSlashCommand(command.InteractionId, content, embedBuider, null);
                }
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
            string[] Servers = Config.ServerList.Split(',');
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