﻿﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using CounterStrikeSharp.API.Core.Capabilities;
using DiscordUtilitiesAPI.Events;

namespace DiscordUtilities
{
    public partial class DiscordUtilities : BasePlugin, IPluginConfig<DUConfig>
    {
        public override string ModuleName => "Discord Utilities";
        public override string ModuleAuthor => "Nocky (SourceFactory.eu)";
        public override string ModuleVersion => "2.2.0";
        public void OnConfigParsed(DUConfig config)
        {
            Config = config;
        }
        public override void OnAllPluginsLoaded(bool hotReload)
        {
            // Every missing value is reported at once, so the config does not have to be fixed one error at a time
            var missingDatabase = new List<string>();
            if (string.IsNullOrWhiteSpace(Config.Database.Host)) missingDatabase.Add("Host");
            if (string.IsNullOrWhiteSpace(Config.Database.User)) missingDatabase.Add("User");
            if (string.IsNullOrWhiteSpace(Config.Database.DatabaseName)) missingDatabase.Add("Database");
            if (string.IsNullOrEmpty(Config.Database.Password)) missingDatabase.Add("Password");

            if (string.IsNullOrWhiteSpace(Config.Token))
                Perform_SendConsoleMessage("'Bot Token' is empty in config! The Discord BOT will not be loaded.", ConsoleColor.Red);
            if (string.IsNullOrWhiteSpace(Config.ServerID))
                Perform_SendConsoleMessage("'Discord Server ID' is empty in config! The Discord BOT will not be loaded.", ConsoleColor.Red);
            else if (!ulong.TryParse(Config.ServerID.Trim(), out _))
                Perform_SendConsoleMessage($"'Discord Server ID' in config is not a valid ID ('{Config.ServerID}')! The Discord BOT will not be loaded.", ConsoleColor.Red);

            if (!Config.Link.Enabled)
                Perform_SendConsoleMessage("Link is disabled in config ('Link System' -> 'Enabled'), the link commands are not registered on this server.", ConsoleColor.DarkYellow);

            if (Config.Link.Enabled && !Config.Link.ResponseServer)
                Perform_SendConsoleMessage("Link: 'Link System' -> 'Response Server' is disabled on this server. Exactly one server must have it enabled, otherwise the Link Slash Command gets no response in Discord.", ConsoleColor.DarkYellow);

            if (missingDatabase.Count == 0)
            {
                databaseData = new DatabaseConnection
                {
                    Server = Config.Database.Host,
                    Port = (uint)Config.Database.Port,
                    User = Config.Database.User,
                    Database = Config.Database.DatabaseName,
                    Password = Config.Database.Password,
                };

                // Started on the thread pool: the synchronous part (driver/HTTP stack initialization, DNS) must not stall the game thread
                _ = Task.Run(InitializeAsync);
            }
            else
            {
                Perform_SendConsoleMessage($"You need to setup Database credentials in config ('Database Connection' is missing: '{string.Join("', '", missingDatabase)}')", ConsoleColor.Red);
            }
        }
        private async Task InitializeAsync()
        {
            await CreateDatabaseConnection();
            if (IsDbConnected)
                Server.NextFrame(LoadConnectedPlayers);

            // Without a valid Server ID or Bot Token the bot cannot do anything, so it is not started at all
            // Both are already reported in 'OnAllPluginsLoaded'
            if (string.IsNullOrWhiteSpace(Config.Token) || string.IsNullOrWhiteSpace(Config.ServerID) || !ulong.TryParse(Config.ServerID.Trim(), out _))
                return;

            _ = LoadDiscordBOT();

            int counter = 0;
            while (!IsBotConnected)
            {
                counter++;
                if (counter > 5 || BotConnectFatal)
                {
                    var reason = BotConnectError;
                    Perform_SendConsoleMessage(string.IsNullOrEmpty(reason) ? "Discord BOT failed to connect!" : $"Discord BOT failed to connect! Reason: {reason}", ConsoleColor.Red);
                    break;
                }
                else
                    Perform_SendConsoleMessage("Loading Discord BOT...", ConsoleColor.DarkYellow);
                await Task.Delay(3000);
            }
        }
        public override void Load(bool hotReload)
        {
            var DUApi = new DiscordUtilities();
            Capabilities.RegisterPluginCapability(DiscordUtilitiesAPI, () => DUApi);

            CreateCustomCommands();
            if (Config.UseCustomVariables)
                LoadCustomConditions();

            // Started on the thread pool: the first HTTP request initializes the HTTP stack synchronously
            _ = Task.Run(LoadVersions);
            _ = Task.Run(LoadMapImages);
            IsDbConnected = false;
            IsBotConnected = false;
            BotConnectError = "";
            BotConnectFatal = false;
            IsDebug = Config.Debug;
            ServerId = Config.ServerID;
            LinkRoleId = Config.Link.Enabled ? Config.Link.LinkDiscordSettings.LinkRole.Trim() : "";
            UseCustomVariables = Config.UseCustomVariables;
            DateFormat = Config.DateFormat;
            savedInteractions.Clear();

            serverData.ModuleDirectory = ModuleDirectory;
            serverData.IP = Config.ServerIP;

            Server.ExecuteCommand("sv_hibernate_when_empty false");
            bool mapStarted = false;
            void OnMapStarted(string mapName)
            {
                if (!mapStarted)
                {
                    mapStarted = true;
                    Server.ExecuteCommand("sv_hibernate_when_empty false");
                    playerData.Clear();
                    AddTimer(4.0f, () =>
                    {
                        if (Config.TimedRoles)
                            CheckExpiredTimedRoles();

                        UpdateServerData();
                        serverData.MapName = mapName;
                        serverData.GameDirectory = Server.GameDirectory;
                        serverData.MaxPlayers = Server.MaxPlayers.ToString();
                        ServerDataLoaded();
                    });

                    AddTimer(60.0f, () =>
                    {
                        UpdateServerData();
                        foreach (var player in Utilities.GetPlayers().Where(p => !p.IsBot && !p.IsHLTV && p.Connected == PlayerConnectedState.Connected && p.AuthorizedSteamID != null && playerData.ContainsKey(p.Slot)))
                        {
                            playerData[player.Slot].PlayedTime++;
                        }
                    }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
                }
            }
            RegisterListener<Listeners.OnMapStart>(OnMapStarted);
            RegisterListener<Listeners.OnMapEnd>(() => { mapStarted = false; });

            // 'OnMapStart' is not fired again for the running map, without this the timers and server data wait for the next map
            if (hotReload)
                OnMapStarted(Server.MapName);
        }

        private async Task LoadDiscordBOT()
        {
            try
            {
                BotClient = new DiscordSocketClient(new DiscordSocketConfig()
                {
                    AlwaysDownloadUsers = true,
                    UseInteractionSnowflakeDate = false,
                    GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildMembers | GatewayIntents.GuildScheduledEvents
                });

                BotCommands = new CommandService();
                BotServices = new ServiceCollection()
                    .AddSingleton(BotClient)
                    .AddSingleton(BotCommands)
                    .BuildServiceProvider();

                await BotClient.LoginAsync(TokenType.Bot, Config.Token);
                var missingIntents = await CheckPrivilegedIntents();
                if (missingIntents.Count > 0)
                {
                    // The gateway would close the connection with code 4014 anyway, so the bot is not started
                    BotConnectError = $"'Disallowed intent(s)' - enable '{string.Join("' and '", missingIntents)}' in the Discord Developer Portal";
                    BotConnectFatal = true;
                    return;
                }

                // Subscribed once here, 'Ready' is fired again on every new gateway session
                BotClient.Disconnected += BotDisconnected;
                BotClient.Ready += ReadyAsync;
                BotClient.SlashCommandExecuted += SlashCommandHandler;
                BotClient.MessageReceived += MessageReceivedHandler;
                BotClient.InteractionCreated += InteractionCreatedHandler;
                BotClient.GuildScheduledEventCreated += ScheduledEventCreated;
                BotClient.GuildMemberUpdated += GuildMemberUpdated;
                BotClient.UserLeft += GuildMemberLeft;

                await BotClient.StartAsync();
            }
            catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Unauthorized)
            {
                BotConnectError = "'Invalid Bot Token' (401 Unauthorized) - check 'Bot Token' in config";
                BotConnectFatal = true;
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while initializing the Discord BOT: '{ex.Message}'", ConsoleColor.Red);
                BotConnectError = $"'{ex.Message}'";
                BotConnectFatal = true;
            }
        }

        // Privileged intents requested in GatewayIntents must be enabled in the Developer Portal,
        // otherwise the gateway closes the connection with code 4014.
        private async Task<List<string>> CheckPrivilegedIntents()
        {
            var missing = new List<string>();
            try
            {
                var application = await BotClient!.GetApplicationInfoAsync();
                var flags = application.Flags;

                if (!flags.HasFlag(ApplicationFlags.GatewayGuildMembers) && !flags.HasFlag(ApplicationFlags.GatewayGuildMembersLimited))
                    missing.Add("Server Members Intent");
                if (!flags.HasFlag(ApplicationFlags.GatewayMessageContent) && !flags.HasFlag(ApplicationFlags.GatewayMessageContentLimited))
                    missing.Add("Message Content Intent");

                if (missing.Count == 0)
                    return missing;

                foreach (var intent in missing)
                    Perform_SendConsoleMessage($"Discord BOT is missing privileged intent '{intent}'!", ConsoleColor.Red);
                Perform_SendConsoleMessage("Enable it at 'https://discord.com/developers/applications' -> Bot -> Privileged Gateway Intents", ConsoleColor.Red);
            }
            catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // 'LoginAsync' does not verify the token against Discord, this is the first real request.
                // Rethrown so the bot is never started with a token that Discord rejects.
                throw;
            }
            catch (Exception ex)
            {
                if (IsDebug)
                    Perform_SendConsoleMessage($"Unable to check privileged intents of the Discord BOT: '{ex.Message}'", ConsoleColor.Cyan);
                missing.Clear();
            }
            return missing;
        }

        private Task BotDisconnected(Exception exception)
        {
            if (exception == null)
                return Task.CompletedTask;

            Discord.Net.WebSocketClosedException? closed = null;
            for (var e = exception; e != null; e = e.InnerException)
            {
                if (e is Discord.Net.WebSocketClosedException wsException)
                {
                    closed = wsException;
                    break;
                }
            }

            if (closed == null)
            {
                for (var e = exception; e != null; e = e.InnerException)
                {
                    if (e is Discord.Net.HttpException { HttpCode: System.Net.HttpStatusCode.Unauthorized })
                    {
                        // Discord.Net would retry forever with a rejected token
                        BotConnectError = "'Invalid Bot Token' (401 Unauthorized) - check 'Bot Token' in config";
                        BotConnectFatal = true;
                        Perform_SendConsoleMessage($"Discord BOT has been stopped! Reason: {BotConnectError}", ConsoleColor.Red);
                        _ = StopDiscordBOT(BotClient);
                        return Task.CompletedTask;
                    }
                }

                BotConnectError = $"'{(exception.InnerException ?? exception).Message}'";
                return Task.CompletedTask;
            }

            var reason = string.IsNullOrEmpty(closed.Reason) ? "Connection closed by Discord" : closed.Reason;
            switch (closed.CloseCode)
            {
                case 4004:
                    BotConnectError = $"'{reason}' (close code '4004') - check 'Bot Token' in config";
                    BotConnectFatal = true;
                    break;
                case 4013:
                case 4014:
                    BotConnectError = $"'{reason}' (close code '{closed.CloseCode}') - enable 'Server Members Intent' and 'Message Content Intent' in the Discord Developer Portal";
                    BotConnectFatal = true;
                    break;
                default:
                    BotConnectError = $"'{reason}' (close code '{closed.CloseCode}')";
                    break;
            }
            return Task.CompletedTask;
        }
        // Discord only answers "Missing Permissions" (50013) without saying which one
        private static string GetMissingPermissionHint(Exception ex, string requiredPermissions)
        {
            bool missingPermissions = ex is Discord.Net.HttpException { DiscordCode: DiscordErrorCode.MissingPermissions or DiscordErrorCode.InsufficientPermissions }
                || ex.Message.Contains("Missing required guild permission");

            return missingPermissions ? $" - give the role of the Discord BOT the permission(s) {requiredPermissions} in the Discord Server Settings" : "";
        }

        private void CheckBotPermissions(SocketGuild guild)
        {
            try
            {
                var permissions = guild.CurrentUser?.GuildPermissions;
                if (permissions == null || permissions.Value.Administrator)
                    return;

                // 'Create Events' (1 << 44) is not known by this Discord.Net version
                if ((permissions.Value.RawValue & (1UL << 44)) == 0)
                    Perform_SendConsoleMessage("Discord BOT is missing the permission 'Create Events'! Link codes and linked players will not be synced between servers.", ConsoleColor.Red);
                if (!permissions.Value.ManageEvents)
                    Perform_SendConsoleMessage("Discord BOT is missing the permission 'Manage Events'! Link codes and linked players will not be synced between servers.", ConsoleColor.Red);
                if (!permissions.Value.ManageRoles)
                    Perform_SendConsoleMessage("Discord BOT is missing the permission 'Manage Roles'! Roles (Link role, timed roles, module roles) cannot be added or removed.", ConsoleColor.Red);
            }
            catch (Exception ex)
            {
                if (IsDebug)
                    Perform_SendConsoleMessage($"Unable to check permissions of the Discord BOT: '{ex.Message}'", ConsoleColor.Cyan);
            }
        }

        private static async Task StopDiscordBOT(DiscordSocketClient? client)
        {
            if (client == null)
                return;

            try
            {
                await client.StopAsync();
            }
            catch (Exception ex)
            {
                if (IsDebug)
                    Perform_SendConsoleMessage($"An error occurred while stopping the Discord BOT: '{ex.Message}'", ConsoleColor.Cyan);
            }
        }

        private async Task ReadyAsync()
        {
            bool firstReady = !IsBotConnected;
            IsBotConnected = true;

            if (firstReady)
            {
                Perform_SendConsoleMessage("Discord BOT has been connected!", ConsoleColor.Green);
                BotLoaded();
                Server.NextFrame(LoadConnectedLinkedPlayers);
            }
            else if (IsDebug)
                Perform_SendConsoleMessage("Discord BOT has been reconnected", ConsoleColor.Cyan);

            // Logs the reason itself when the guild cannot be resolved
            if (TryGetGuild(out var readyGuild) && firstReady)
                CheckBotPermissions(readyGuild);

            var client = BotClient;
            if (client == null)
                return;

            var ActivityFormat = ReplaceServerDataVariables(Config.BotStatus.ActivityFormat);
            await client.SetActivityAsync(new Game(ActivityFormat, (ActivityType)Config.BotStatus.ActivityType, ActivityProperties.None));
            await client.SetStatusAsync((UserStatus)Config.BotStatus.Status);

            if (!firstReady)
                return;

            var linkCommand = new SlashCommandBuilder()
                .WithName(Config.Link.LinkDiscordSettings.Name.ToLower())
                .WithDescription(Config.Link.LinkDiscordSettings.Description)
                .AddOption(Config.Link.LinkDiscordSettings.OptionName.ToLower(), ApplicationCommandOptionType.String, Config.Link.LinkDiscordSettings.OptionDescription, isRequired: true);

            try
            {
                if (Config.Link.Enabled)
                {
                    if (IsDbConnected)
                    {
                        if (IsDebug)
                            Perform_SendConsoleMessage($"Link Slash Command has been successfully updated/created", ConsoleColor.Cyan);
                        await client.CreateGlobalApplicationCommandAsync(linkCommand.Build());
                    }
                    else
                    {
                        Perform_SendConsoleMessage($"Link Slash Command was not created because you do not have a database connected", ConsoleColor.Red);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while updating Link Slash Commands: '{ex.Message}'", ConsoleColor.Red);
            }
        }

        private async Task GuildMemberLeft(SocketGuild guild, SocketUser user)
        {
            if (Config.Link.Enabled && Config.Link.ResponseServer)
            {
                if (TryGetSteamIdByDiscordId(user.Id, out var steamId))
                {
                    await RemovePlayerData(steamId.ToString());
                    await CreateScheduledEventAsync("refreshlinkedplayers");
                }
            }
        }

        private async Task GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
        {
            if (!TryGetSteamIdByDiscordId(after.Id, out _))
                return;

            var beforeGuildUser = await before.GetOrDownloadAsync();

            var beforeRoles = beforeGuildUser.Roles.Select(r => r.Id).ToList();
            var afterRoles = after.Roles.Select(r => r.Id).ToList();

            var addedRoles = afterRoles
                .Except(beforeRoles)
                .Select(role => role.ToString())
                .ToList();

            var removedRoles = beforeRoles
                .Except(afterRoles)
                .Select(role => role.ToString())
                .ToList();

            Server.NextFrame(() =>
            {
                var user = GetUserDataByUserID(after.Id);
                if (user == null)
                    return;

                DiscordUtilitiesAPI.Get()?.TriggerEvent(new LinkedUserRolesUpdated(user, addedRoles, removedRoles));
                if (IsDebug)
                    Perform_SendConsoleMessage("New Event Triggered: 'LinkedUserRolesUpdated'", ConsoleColor.Cyan);
            });
        }

        public async Task CreateScheduledEventAsync(string eventData)
        {
            try
            {
                if (!TryGetGuild(out var guild))
                    return;

                var guildEvent = await guild.CreateEventAsync($"Custom Event (DU)", DateTimeOffset.UtcNow.AddMinutes(1), GuildScheduledEventType.External, endTime: DateTimeOffset.UtcNow.AddMinutes(2), location: "Discord Utilities", description: eventData);
                await guildEvent.DeleteAsync();
            }
            catch (Exception ex)
            {
                // The description is not logged, it can contain an encoded link code
                Perform_SendConsoleMessage($"An error occurred while syncing data between servers: '{ex.Message}'{GetMissingPermissionHint(ex, "'Create Events' and 'Manage Events'")}", ConsoleColor.Red);
            }
        }

        private async Task ScheduledEventCreated(SocketGuildEvent scheduledEvent)
        {
            if (scheduledEvent.Location.Equals("Discord Utilities"))
            {
                if (scheduledEvent.Name.Contains("Custom Event (DU)"))
                {
                    var data = scheduledEvent.Description.Split(';');
                    var CustomId = data.FirstOrDefault();
                    if (CustomId == null)
                        return;

                    if (CustomId.Equals("addcode"))
                    {
                        if (data.Length < 3)
                            return;

                        var code = DecodeSecretString(data[1]);
                        if (!string.IsNullOrEmpty(code))
                            linkCodes.TryAdd(code, data[2]);
                    }
                    else if (CustomId.Equals("removecode"))
                    {
                        if (data.Length < 2)
                            return;

                        // Sent as plain code by the server that handled the link, older versions may send it encoded
                        if (!linkCodes.TryRemove(data[1], out _))
                            linkCodes.TryRemove(DecodeSecretString(data[1]), out _);
                    }
                    else if (CustomId.Equals("refreshlinkedplayers"))
                    {
                        await LoadLinkedPlayers();
                    }
                }
            }
            return;
        }

        private Task InteractionCreatedHandler(SocketInteraction interaction)
        {
            if (interaction is SocketMessageComponent MessageComponent)
            {
                Event_InteractionCreated(interaction, MessageComponent);
            }
            else if (interaction.Type == InteractionType.ModalSubmit)
            {
                Event_ModalSubmited(interaction);
            }
            return Task.CompletedTask;
        }

        private Task MessageReceivedHandler(SocketMessage message)
        {
            if (message.Author.IsBot || message.Author.IsWebhook)
                return Task.CompletedTask;

            Event_MessageReceived(message);
            return Task.CompletedTask;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.CommandName == Config.Link.LinkDiscordSettings.Name.ToLower())
                await DiscordLink_CMD(command);
            else
                Event_SlashCommand(command);
        }

        public override void Unload(bool hotReload)
        {
            if (updateTimer != null)
                updateTimer.Kill();

            var client = BotClient;
            BotClient = null;
            IsBotConnected = false;

            if (client != null)
            {
                client.Disconnected -= BotDisconnected;
                client.Ready -= ReadyAsync;
                client.SlashCommandExecuted -= SlashCommandHandler;
                client.MessageReceived -= MessageReceivedHandler;
                client.InteractionCreated -= InteractionCreatedHandler;
                client.GuildScheduledEventCreated -= ScheduledEventCreated;
                client.GuildMemberUpdated -= GuildMemberUpdated;
                client.UserLeft -= GuildMemberLeft;

                _ = ShutdownDiscordBOT(client);
            }

            (BotCommands as IDisposable)?.Dispose();
            BotCommands = null;
            (BotServices as IDisposable)?.Dispose();
            BotServices = null;
        }

        // Without this the old client keeps its gateway session alive after a plugin reload
        private static async Task ShutdownDiscordBOT(DiscordSocketClient client)
        {
            try
            {
                await client.StopAsync();
                await client.LogoutAsync();
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while stopping the Discord BOT: '{ex.Message}'", ConsoleColor.Red);
            }

            try
            {
                await client.DisposeAsync();
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while disposing the Discord BOT: '{ex.Message}'", ConsoleColor.Red);
            }
        }

        public static void Perform_SendConsoleMessage(string text, ConsoleColor color)
        {
            string prefix = "[Discord Utilities] ";
            string suffix = text;

            switch (color)
            {
                case ConsoleColor.Cyan:
                    prefix = "[Discord Utilities] (DEBUG): ";
                    break;
                case ConsoleColor.Red:
                    prefix = "[Discord Utilities] (ERROR): ";
                    break;
            }

            Console.ForegroundColor = color;
            Console.Write(prefix);

            Console.ForegroundColor = ConsoleColor.White;
            bool isInQuotes = false;

            foreach (char c in suffix)
            {
                if (c == '\'')
                {
                    isInQuotes = !isInQuotes;
                    continue;
                }
                Console.ForegroundColor = isInQuotes ? ConsoleColor.Yellow : ConsoleColor.White;
                Console.Write(c);
            }

            Console.WriteLine();
            Console.ResetColor();
        }
    }
}
