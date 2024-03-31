using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Discord.Net;

namespace DiscordUtilities
{
    [MinimumApiVersion(202)]
    public partial class DiscordUtilities : BasePlugin, IPluginConfig<DUConfig>
    {
        public override string ModuleName => "Discord Utilities";
        public override string ModuleAuthor => "Nocky (SourceFactory.eu)";
        public override string ModuleVersion => "1.0.8";
        private DiscordSocketClient? BotClient;
        private CommandService? BotCommands;
        private IServiceProvider? BotServices;
        public static DiscordUtilities Instance { get; private set; } = new();
        public static bool IsBotConnected;
        public static bool IsDbConnected;
        public DUConfig Config { get; set; } = null!;
        public void OnConfigParsed(DUConfig config)
        {
            Config = config;
        }
        public override void Load(bool hotReload)
        {
            AddCommandListener("say", OnPlayerSay, HookMode.Post);
            AddCommandListener("say_team", OnPlayerSayTeam, HookMode.Post);
            CreateCustomCommands();
            LoadManageRolesAndFlags();

            IsDbConnected = false;
            IsBotConnected = false;
            serverData = new ServerData
            {
                GameDirectory = Server.GameDirectory,
                Name = "Counter-Strike Server",
                MaxPlayers = 10.ToString(),
                MapName = "",
                OnlinePlayers = 0.ToString(),
                OnlinePlayersAndBots = 0.ToString(),
                OnlineBots = 0.ToString(),
                Timeleft = 60.ToString()
            };

            _ = LoadDiscordBOT();
            if (!string.IsNullOrEmpty(Config.Database.Password) && !string.IsNullOrEmpty(Config.Database.Host) && !string.IsNullOrEmpty(Config.Database.DatabaseName) && !string.IsNullOrEmpty(Config.Database.User))
                _ = CreateDatabaseConnection();
                
            RegisterListener<Listeners.OnMapStart>(mapName =>
            {
                playerData.Clear();
                reportCooldowns.Clear();
                performReport.Clear();

                if (Config.ServerStatus.UpdateTimer > 29 || Config.BotStatus.UpdateTimer > 29)
                    Server.ExecuteCommand("sv_hibernate_when_empty false");

                while (!IsBotConnected)
                {
                    SendConsoleMessage($"[Discord Utilities] BOT is not loaded...", ConsoleColor.DarkYellow);
                    Thread.Sleep(3000);
                }

                if (IsBotConnected)
                {
                    if (Config.ConnectedPlayers.Enabled)
                        _ = ClearConnectedPlayersRole();

                    if (Config.EventNotifications.MapChanged.Enabled)
                        PerformMapStart();

                    AddTimer(5.0f, () =>
                    {
                        UpdateServerData();

                    }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);

                    if (Config.BotStatus.UpdateTimer > 29)
                    {
                        AddTimer(Config.BotStatus.UpdateTimer, () =>
                        {
                            _ = UpdateBotStatus();

                        }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
                    }
                    if (Config.ServerStatus.UpdateTimer > 29 && !string.IsNullOrEmpty(Config.ServerStatus.ChannelID))
                    {
                        _ = UpdateServerStatus(components: null!, false);
                        AddTimer(Config.ServerStatus.UpdateTimer, () =>
                        {
                            var componentsBuilder = new ComponentBuilder();
                            bool addComponents = false;
                            if (Config.ServerStatus.ServerStatusEmbed.JoinButton.Enabled || Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.Enabled)
                            {
                                addComponents = true;
                                componentsBuilder = GetServerStatusComponents(componentsBuilder);
                            }
                            _ = UpdateServerStatus(componentsBuilder, addComponents);

                        }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
                    }
                }
            });
        }
        private async Task LoadDiscordBOT()
        {
            try
            {
                BotClient = new DiscordSocketClient(new DiscordSocketConfig()
                {
                    AlwaysDownloadUsers = true,
                    GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildMembers
                });

                BotCommands = new CommandService();
                BotServices = new ServiceCollection()
                    .AddSingleton(BotClient)
                    .AddSingleton(BotCommands)
                    .BuildServiceProvider();

                await BotClient.LoginAsync(TokenType.Bot, Config.Token);
                await BotClient.StartAsync();

                BotClient.Ready += ReadyAsync;
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while initializing the Discord BOT: {ex.Message}", ConsoleColor.Red);
            }
        }

        private async Task ReadyAsync()
        {
            SendConsoleMessage("[Discord Utilities] Discord BOT has been connected!", ConsoleColor.Green);
            IsBotConnected = true;

            string ActivityFormat = ReplaceServerDataVariables(Config.BotStatus.ActivityFormat);
            await BotClient!.SetGameAsync(ActivityFormat, null, (ActivityType)Config.BotStatus.ActivityType);
            await BotClient.SetStatusAsync((UserStatus)Config.BotStatus.Status);

            var linkCommand = new SlashCommandBuilder()
                .WithName(Config.Link.DiscordCommand.ToLower())
                .WithDescription(Config.Link.DiscordDescription)
                .AddOption(Config.Link.DiscordOptionName.ToLower(), ApplicationCommandOptionType.String, Config.Link.DiscordOptionDescription, isRequired: true);

            var rconCommand = new SlashCommandBuilder()
                .WithName(Config.Rcon.Command.ToLower())
                .WithDescription(Config.Rcon.Description);

            var serverOption = new SlashCommandOptionBuilder()
                .WithName(Config.Rcon.ServerOptionName.ToLower())
                .WithDescription(Config.Rcon.ServerOptionDescription)
                .WithType(ApplicationCommandOptionType.String)
                .WithRequired(true);

            string[] Servers = Config.Rcon.ServerList.Split(',');
            if (Servers.Count() > 1)
                serverOption.AddChoice("All", "All");

            foreach (var server in Servers)
                serverOption.AddChoice(server, server);
            rconCommand.AddOption(serverOption);
            rconCommand.AddOption(Config.Rcon.CommandOptionName.ToLower(), ApplicationCommandOptionType.String, Config.Rcon.CommandOptionDescription, isRequired: true);

            try
            {
                if (Config.Link.Enabled && IsDbConnected)
                    await BotClient.CreateGlobalApplicationCommandAsync(linkCommand.Build());
                if (Config.Rcon.Enabled)
                    await BotClient.CreateGlobalApplicationCommandAsync(rconCommand.Build());
            }
            catch (HttpException ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while updating Slash Commands: {ex.Message}", ConsoleColor.Red);
            }

            if (Config.Link.Enabled || Config.Rcon.Enabled)
            {
                BotClient.SlashCommandExecuted += SlashCommandHandler;
                if (Config.Debug)
                    SendConsoleMessage($"[Discord Utilities] DEBUG: Link or RCON Slash Command Loaded (Slash Command Handler)", ConsoleColor.Cyan);
            }

            if (Config.DiscordRelay.Enabled && !string.IsNullOrEmpty(Config.DiscordRelay.ChannelID))
            {
                BotClient.MessageReceived += MessageReceived;
                if (Config.Debug)
                    SendConsoleMessage($"[Discord Utilities] DEBUG: Discord Relay Loaded (Message Received Handler)", ConsoleColor.Cyan);
            }

            if ((Config.ServerStatus.UpdateTimer > 29 && Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.Enabled) || Config.Report.ReportEmbed.ReportButton.Enabled)
            {
                BotClient.InteractionCreated += OnInteractionCreatedAsync;
                if (Config.Debug)
                    SendConsoleMessage($"[Discord Utilities] DEBUG: Server Status or Report Button Loaded (Interaction Handler)", ConsoleColor.Cyan);
            }
        }
        private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
        {
            if (interaction is SocketMessageComponent component)
            {
                IDiscordInteractionData data = component.Data;
                if (data is IComponentInteractionData componentData)
                {
                    if (componentData.CustomId == "serverstatus-players")
                    {
                        Server.NextFrame(() =>
                        {
                            SelectMenuResponse(component);
                        });
                    }
                    else if (componentData.CustomId.Contains("report-"))
                    {
                        ulong guildId = interaction.GuildId!.Value;
                        var guild = BotClient!.GetGuild(guildId);
                        if (guild == null)
                        {
                            SendConsoleMessage($"[Discord Utilities] Report: Guild has not been found ('{guildId}')", ConsoleColor.Red);
                            return;
                        }

                        string reportedId = componentData.CustomId.Replace("report-", "");
                        var user = guild.GetUser(interaction.User.Id);

                        if (string.IsNullOrEmpty(Config.Report.ReportEmbed.ReportButton.AdminRoleId))
                        {
                            await component.RespondAsync(text: "The Admin role ID is not set!", ephemeral: true);
                            return;
                        }
                        var role = guild.GetRole(ulong.Parse(Config.Report.ReportEmbed.ReportButton.AdminRoleId));
                        if (role == null)
                        {
                            SendConsoleMessage($"[Discord Utilities] Admin Role with ID '{Config.Report.ReportEmbed.ReportButton.AdminRoleId}' was not found (Report Admin Role)!", ConsoleColor.Red);
                            return;
                        }

                        var permissions = user.GuildPermissions;
                        if (permissions.Administrator || user.Roles.Any(id => id == role))
                        {
                            await UpdateReportMessage(user, ulong.Parse(reportedId));
                            var content = GetContent(ContentTypes.Report_Reply, new string[0]);
                            var embed = GetEmbed(EmbedTypes.Report_Reply, new string[0]);
                            await component.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
                        }
                        else
                        {
                            await component.RespondAsync(text: "You're not the Administrator!", ephemeral: true);
                        }
                    }
                }
            }
        }
        private Task MessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot || message.Author.IsWebhook)
                return Task.CompletedTask;

            if (Config.Debug)
                SendConsoleMessage($"[Discord Utilities] DEBUG: Discord Message '{message}' was logged in channel '{message.Channel.Id}'", ConsoleColor.Cyan);

            if (message.Channel.Id == ulong.Parse(Config.DiscordRelay.ChannelID))
            {
                var guild = BotClient!.GetGuild(ulong.Parse(Config.ServerID));
                if (guild == null)
                {
                    SendConsoleMessage($"[Discord Utilities] Discord Relay: Guild has not been found ('{Config.ServerID}')", ConsoleColor.Red);
                    return Task.CompletedTask;
                }

                var user = guild.GetUser(message.Author.Id);
                Server.NextFrame(() =>
                {
                    if (Config.Debug)
                        SendConsoleMessage($"[Discord Utilities] DEBUG: Discord Message '{message}' was sent to the server by user '{user.DisplayName}'", ConsoleColor.Cyan);
                    PerformChatRelay(user, message);
                });
            }
            return Task.CompletedTask;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (Config.Debug)
                SendConsoleMessage($"[Discord Utilities] DEBUG: User '{command.User.GlobalName}' executed Slash Command '{command.CommandName}'", ConsoleColor.Cyan);

            if (command.CommandName == Config.Link.DiscordCommand.ToLower())
                await DiscordLink_CMD(command);
            if (command.CommandName == Config.Rcon.Command.ToLower())
                await DiscordRcon_CMD(command);
        }

        private async Task DiscordRcon_CMD(SocketSlashCommand command)
        {
            if (Config.Debug)
                SendConsoleMessage($"[Discord Utilities] DEBUG: Slash command '{command.CommandName}' has been successfully logged", ConsoleColor.Cyan);

            ulong guildId = command.GuildId!.Value;
            var guild = BotClient!.GetGuild(guildId);
            if (guild == null)
            {
                SendConsoleMessage($"[Discord Utilities] RCON Slash Command Error: Guild has not been found ('{guildId}')", ConsoleColor.Red);
                return;
            }

            var user = guild.GetUser(command.User.Id);
            if (user == null)
            {
                SendConsoleMessage($"[Discord Utilities] RCON Slash Command Error: User was not found!", ConsoleColor.Red);
                return;
            }

            if (string.IsNullOrEmpty(Config.Rcon.AdminRoleId))
            {
                await command.RespondAsync(text: "The Admin role ID is not set!", ephemeral: true);
                return;
            }

            var role = guild.GetRole(ulong.Parse(Config.Rcon.AdminRoleId));
            if (role == null)
            {
                SendConsoleMessage($"[Discord Utilities] RCON Slash Command Error: Admin Role with ID '{Config.Rcon.AdminRoleId}' was not found!", ConsoleColor.Red);
                return;
            }

            string[] data = new string[2];
            foreach (var option in command.Data.Options)
            {
                if (option.Name == Config.Rcon.ServerOptionName)
                {
                    data[1] = option.Value.ToString()!;
                }
                else if (option.Name == Config.Rcon.CommandOptionName)
                {
                    data[0] = option.Value.ToString()!;
                }
            }
            if (data[1] != Config.Rcon.Server)
            {
                if (data[1] != "All")
                {
                    if (Config.Debug)
                        SendConsoleMessage($"[Discord Utilities] DEBUG: This server is not '{data[1]}'!", ConsoleColor.Cyan);
                    return;
                }
            }

            var permissions = user.GuildPermissions;
            if (permissions.Administrator || user.Roles.Any(id => id == role))
            {
                data[1] = Config.Rcon.Server;
                Server.NextFrame(() =>
                {
                    Server.ExecuteCommand(data[0]);
                });

                var content = GetContent(ContentTypes.Rcon, data);
                var embed = GetEmbed(EmbedTypes.Rcon, data);
                await command.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
            }
            else
            {
                await command.RespondAsync(text: "You're not the Administrator!", ephemeral: true);
            }
        }
        private async Task DiscordLink_CMD(SocketSlashCommand command)
        {
            if (Config.Debug)
                SendConsoleMessage($"[Discord Utilities] DEBUG: Slash command '{command.CommandName}' has been successfully logged", ConsoleColor.Cyan);

            ulong guildId = command.GuildId!.Value;
            var guild = BotClient!.GetGuild(guildId);

            if (guild == null)
            {
                SendConsoleMessage($"[Discord Utilities] LINK Slash Command Error: Guild has not been found ('{guildId}')", ConsoleColor.Red);
                return;
            }

            var user = guild.GetUser(command.User.Id);
            if (user == null)
            {
                SendConsoleMessage($"[Discord Utilities] LINK Slash Command Error: User was not found!", ConsoleColor.Red);
                return;
            }

            var role = guild.GetRole(ulong.Parse(Config.Link.LinkRole));
            if (role == null)
            {
                SendConsoleMessage($"[Discord Utilities] LINK Slash Command Error: Role with id '{Config.Link.LinkRole}' was not found!", ConsoleColor.Red);
                return;
            }
            string content = string.Empty;
            EmbedBuilder embed;
            string[] data = new string[1];

            var linkedPlayers = await GetLinkedPlayers();
            if (linkedPlayers.ContainsValue(user.Id.ToString()))
            {
                var findSteamIdByUserId = linkedPlayers.FirstOrDefault(x => x.Value == user.Id.ToString()).Key;
                data[0] = findSteamIdByUserId.ToString();
                content = GetContent(ContentTypes.AlreadyLinked, data);
                embed = GetEmbed(EmbedTypes.AlreadyLinked, data);
                await command.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
                return;
            }

            var code = command.Data.Options.First().Value.ToString();
            code = code!.Replace(" ", "");
            data[0] = code;

            var codesList = await GetCodesList();
            if (!string.IsNullOrEmpty(code) && codesList.ContainsKey(code))
            {
                data[0] = codesList[code];
                content = GetContent(ContentTypes.LinkSuccess, data);
                embed = GetEmbed(EmbedTypes.LinkSuccess, data);
                await InsertPlayerData(codesList[code].ToString(), command.User.Id.ToString());
                await RemoveCode(code, false);
                await command.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
                if (!user.Roles.Any(id => id == role))
                {
                    await user.AddRoleAsync(role);
                }
                return;
            }
            content = GetContent(ContentTypes.LinkFailed, data);
            embed = GetEmbed(EmbedTypes.LinkFailed, data);
            await command.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
        }
        public override void Unload(bool hotReload)
        {
            if (IsBotConnected)
            {
                if (Config.Link.Enabled || Config.Rcon.Enabled)
                {
                    BotClient!.SlashCommandExecuted -= SlashCommandHandler;
                    if (Config.Debug)
                        SendConsoleMessage($"[Discord Utilities] DEBUG: Link Slash Command Unloaded (Slash Command Handler)", ConsoleColor.Cyan);
                }

                if (Config.DiscordRelay.Enabled && !string.IsNullOrEmpty(Config.DiscordRelay.ChannelID))
                {
                    BotClient!.MessageReceived -= MessageReceived;
                    if (Config.Debug)
                        SendConsoleMessage($"[Discord Utilities] DEBUG: Discord Relay Unloaded (Message Received Handler)", ConsoleColor.Cyan);
                }

                if ((Config.ServerStatus.UpdateTimer > 29 && Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.Enabled) || Config.Report.ReportEmbed.ReportButton.Enabled)
                {
                    BotClient!.InteractionCreated -= OnInteractionCreatedAsync;
                    if (Config.Debug)
                        SendConsoleMessage($"[Discord Utilities] DEBUG: Server Status or Report Button Unloaded (Interaction Handler)", ConsoleColor.Cyan);
                }
            }
        }

        public static void SendConsoleMessage(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }
    }
}
