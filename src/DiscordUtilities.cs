﻿using CounterStrikeSharp.API;
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
    [MinimumApiVersion(166)]
    public partial class DiscordUtilities : BasePlugin, IPluginConfig<DUConfig>
    {
        public override string ModuleName => "Discord Utilities";
        public override string ModuleAuthor => "Nocky (SourceFactory.eu)";
        public override string ModuleVersion => "1.0.7";
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

            RegisterListener<Listeners.OnMapStart>(mapName =>
            {
                linkCodes.Clear();
                linkedPlayers.Clear();
                playerData.Clear();
                relaysList.Clear();
                IsDbConnected = false;
                IsBotConnected = false;

                serverData = new ServerData
                {
                    GameDirectory = Server.GameDirectory,
                    Name = ConVar.Find("hostname")!.StringValue,
                    MaxPlayers = Server.MaxPlayers.ToString(),
                    MapName = Server.MapName,
                    OnlinePlayers = GetPlayersCount().ToString(),
                    OnlinePlayersAndBots = GetPlayersCountWithBots().ToString(),
                    OnlineBots = GetBotsCounts().ToString(),
                    Timeleft = 60.ToString()
                };

                if (Config.ServerStatus.UpdateTimer > 29 || Config.BotStatus.UpdateTimer > 29)
                    Server.ExecuteCommand("sv_hibernate_when_empty false");

                _ = LoadDiscordBOT();
                if (!string.IsNullOrEmpty(Config.Database.Password) && !string.IsNullOrEmpty(Config.Database.Host) && !string.IsNullOrEmpty(Config.Database.DatabaseName) && !string.IsNullOrEmpty(Config.Database.User))
                    _ = CreateDatabaseConnection();

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
                        _ = UpdateServerStatus(components: null!, 0);
                        AddTimer(Config.ServerStatus.UpdateTimer, () =>
                        {
                            var componentsBuilder = new ComponentBuilder();
                            int totalMenuPlayers = 0;
                            if (playerData.Count() > 0 && Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.Enabled)
                            {
                                var menuBuilder = new SelectMenuBuilder()
                                    .WithPlaceholder(Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.MenuName)
                                    .WithCustomId("serverstatus-players")
                                    .WithMinValues(1)
                                    .WithMaxValues(1);

                                foreach (var p in playerData!)
                                {
                                    if (p.Key == null || !p.Key.IsValid || p.Key.AuthorizedSteamID == null)
                                        continue;

                                    string replacedLabel = ReplacePlayerDataVariables(Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.PlayersFormat, p.Key.AuthorizedSteamID.SteamId64);
                                    menuBuilder.AddOption(label: replacedLabel, value: p.Key.AuthorizedSteamID.SteamId64.ToString());
                                    totalMenuPlayers++;
                                }
                                componentsBuilder.WithSelectMenu(menuBuilder);
                            }
                            _ = UpdateServerStatus(componentsBuilder, totalMenuPlayers);

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
                BotClient.SlashCommandExecuted += SlashCommandHandler;
            if (Config.DiscordRelay.Enabled && !string.IsNullOrEmpty(Config.DiscordRelay.ChannelID))
                BotClient.MessageReceived += MessageReceived;
            if ((Config.ServerStatus.UpdateTimer > 29 && Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.Enabled) || Config.Report.ReportEmbed.ReportButton.Enabled)
                BotClient.InteractionCreated += OnInteractionCreatedAsync;
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
                            return;

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

            if (message.Channel.Id == ulong.Parse(Config.DiscordRelay.ChannelID))
            {
                var guild = BotClient!.GetGuild(ulong.Parse(Config.ServerID));
                if (guild == null)
                    return Task.CompletedTask;

                var user = guild.GetUser(message.Author.Id);
                Server.NextFrame(() =>
                {
                    PerformChatRelay(user, message);
                });
            }
            return Task.CompletedTask;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.CommandName == Config.Link.DiscordCommand.ToLower())
                await DiscordLink_CMD(command);
            if (command.CommandName == Config.Rcon.Command.ToLower())
                await DiscordRcon_CMD(command);
        }
        private async Task DiscordRcon_CMD(SocketSlashCommand command)
        {
            ulong guildId = command.GuildId!.Value;
            var guild = BotClient!.GetGuild(guildId);

            if (guild == null)
                return;
            var user = guild.GetUser(command.User.Id);
            if (user == null)
                return;
            if (string.IsNullOrEmpty(Config.Rcon.AdminRoleId))
            {
                await command.RespondAsync(text: "The Admin role ID is not set!", ephemeral: true);
                return;
            }
            var role = guild.GetRole(ulong.Parse(Config.Rcon.AdminRoleId));
            if (role == null)
            {
                SendConsoleMessage($"[Discord Utilities] Admin Role with ID '{Config.Rcon.AdminRoleId}' was not found (Rcon Section)!", ConsoleColor.Red);
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
                    return;
            }

            var permissions = user.GuildPermissions;
            if (permissions.Administrator || user.Roles.Any(id => id == role))
            {
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
            ulong guildId = command.GuildId!.Value;
            var guild = BotClient!.GetGuild(guildId);

            if (guild == null)
                return;

            var user = guild.GetUser(command.User.Id);
            if (user == null)
                return;

            var role = guild.GetRole(ulong.Parse(Config.Link.LinkRole));
            if (role == null)
            {
                SendConsoleMessage($"[Discord Utilities] Role with id '{Config.Link.LinkRole}' was not found (Link Section)!", ConsoleColor.Red);
                return;
            }
            string content = string.Empty;
            EmbedBuilder embed;
            string[] data = new string[1];

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
            data[0] = code!;
            if (!string.IsNullOrEmpty(code) && linkCodes.ContainsKey(code))
            {
                content = GetContent(ContentTypes.LinkSuccess, data);
                embed = GetEmbed(EmbedTypes.LinkSuccess, data);
                await InsertPlayerData(linkCodes[code].ToString(), command.User.Id.ToString());
                await command.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
                if (!user.Roles.Any(id => id == role))
                {
                    await user.AddRoleAsync(role);
                }
                Server.NextFrame(() => { PerformLinkAccount(code, command.User.GlobalName, command.User.Id.ToString()); });
                return;
            }
            content = GetContent(ContentTypes.LinkFailed, data);
            embed = GetEmbed(EmbedTypes.LinkFailed, data);
            await command.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
        }
        private async Task UnLoadDiscordBOT()
        {
            if (BotClient != null)
                await BotClient.StopAsync();
        }

        public static void SendConsoleMessage(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }
    }
}
