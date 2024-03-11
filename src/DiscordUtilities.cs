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
    [MinimumApiVersion(166)]
    public partial class DiscordUtilities : BasePlugin, IPluginConfig<DUConfig>
    {
        public override string ModuleName => "Discord Utilities";
        public override string ModuleAuthor => "Nocky (SourceFactory.eu)";
        public override string ModuleVersion => "1.0.2";
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
            IsDbConnected = false;
            IsBotConnected = false;
            if (!string.IsNullOrEmpty(Config.Database.Password) && !string.IsNullOrEmpty(Config.Database.Host) && !string.IsNullOrEmpty(Config.Database.DatabaseName) && !string.IsNullOrEmpty(Config.Database.User))
                _ = CreateDatabaseConnection();

            AddCommandListener("say", OnPlayerSay, HookMode.Post);
            AddCommandListener("say_team", OnPlayerSayTeam, HookMode.Post);
            CreateCustomCommands();
            LoadManageRolesAndFlags();

            RegisterListener<Listeners.OnMapStart>(mapName =>
            {
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
                if (!string.IsNullOrEmpty(Config.Token))
                    _ = LoadDiscordBOT();

                Server.NextFrame(() =>
                {
                    if (Config.ConnectedPlayers.Enabled)
                        _ = ClearConnectedPlayersRole();

                    AddTimer(5.0f, () =>
                    {
                        UpdateServerData();

                    }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);

                    if (Config.BotStatus.UpdateTimer > 5)
                    {
                        AddTimer(Config.BotStatus.UpdateTimer, () =>
                        {
                            if (IsBotConnected)
                                _ = UpdateBotStatus();

                        }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
                    }
                    if (Config.ServerStatus.UpdateTimer > 29)
                    {
                        _ = UpdateServerStatus();
                        AddTimer(Config.ServerStatus.UpdateTimer, () =>
                        {
                            if (IsBotConnected && !string.IsNullOrEmpty(Config.ServerStatus.ChannelID))
                                _ = UpdateServerStatus();

                        }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
                    }
                });
            });
        }
        public override void Unload(bool hotReload)
        {
            linkCodes.Clear();
            linkedPlayers.Clear();
            playerData.Clear();
            //_ = UpdateServerStatus(true);
            //_ = UnLoadDiscordBOT();
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
                .WithName(Config.Link.DiscordCommand)
                .WithDescription(Config.Link.DiscordDescription)
                .AddOption(Config.Link.DiscordOptionName.ToLower(), ApplicationCommandOptionType.String, Config.Link.DiscordOptionDescription, isRequired: true);

            try
            {
                await BotClient.CreateGlobalApplicationCommandAsync(linkCommand.Build());
            }
            catch (HttpException ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while updating Slash Commands: {ex.Message}", ConsoleColor.Red);
            }

            if (Config.Link.Enabled && IsDbConnected)
                BotClient.SlashCommandExecuted += SlashCommandHandler;
            if (Config.DiscordRelay.Enabled && !string.IsNullOrEmpty(Config.DiscordRelay.ChannelID))
                BotClient.MessageReceived += MessageReceived;
            if (Config.ServerStatus.UpdateTimer > 29 && Config.ServerStatus.ServerStatusEmbed.ServerStatusDropdown.Enabled)
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
                        IReadOnlyCollection<string> selectedValues = componentData.Values;
                        string[] selectedPlayer = new string[1];
                        selectedPlayer[0] = selectedValues.FirstOrDefault()!;

                        var content = GetContent(ContentTypes.ServerStatus_Player, selectedPlayer);
                        var embed = GetEmbed(EmbedTypes.ServerStatus_Player, selectedPlayer);
                        await component.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
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
            if (command.CommandName == Config.Link.DiscordCommand)
                await DiscordLink_CMD(command);
        }
        private async Task DiscordLink_CMD(SocketSlashCommand command)
        {
            Console.WriteLine("0");
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

            var linkedSteamd = await CheckIsPlayerLinked(user.Id.ToString());
            if (!string.IsNullOrEmpty(linkedSteamd))
            {
                data[0] = linkedSteamd;
                content = GetContent(ContentTypes.AlreadyLinked, data);
                embed = GetEmbed(EmbedTypes.AlreadyLinked, data);
                await command.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
                return;
            }

            var code = command.Data.Options.First().Value.ToString();
            if (!string.IsNullOrEmpty(code) && linkCodes.ContainsKey(code))
            {
                data[0] = code!;
                content = GetContent(ContentTypes.LinkSuccess, data);
                embed = GetEmbed(EmbedTypes.LinkSuccess, data);
                await InsertPlayerData(linkCodes[code].ToString(), command.User.Id.ToString());
                await command.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
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
