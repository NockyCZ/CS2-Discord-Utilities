using CounterStrikeSharp.API;
using Discord;
using CounterStrikeSharp.API.Modules.Cvars;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using Newtonsoft.Json;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };

        public async Task LoadMapImages()
        {
            mapImagesList.Clear();
            var client = httpClient;
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync("https://nockycz.github.io/CS2-Discord-Utilities/MapImages/map_list.json");
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    mapImagesList = JsonConvert.DeserializeObject<List<string>>(responseBody)!;
                    if (Config.Debug)
                        Perform_SendConsoleMessage($"Loaded total '{mapImagesList.Count} Map Images'", ConsoleColor.Cyan);
                }
                catch (HttpRequestException ex)
                {
                    Perform_SendConsoleMessage($"An error occurred while loading Map Images: '{ex.Message}'", ConsoleColor.Red);
                }
                catch (Exception ex)
                {
                    Perform_SendConsoleMessage($"An error occurred while loading Map Images: '{ex.Message}'", ConsoleColor.Red);
                }
            }
        }

        public async Task LoadVersions()
        {
            moduleVersions.Clear();
            var client = httpClient;
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync("https://nockycz.github.io/CS2-Discord-Utilities/module_versions.json");
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    var versions = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody)!;
                    if (versions != null)
                    {
                        foreach (var module in versions)
                        {
                            string moduleName = module.Key;
                            if (!moduleName.Equals("Main"))
                                moduleName = "[Discord Utilities] " + moduleName;

                            moduleVersions.Add(moduleName, module.Value);
                        }

                        if (moduleVersions.TryGetValue("Main", out var latestVersion))
                        {
                            if (!ModuleVersion.Trim().Equals(latestVersion.Trim()))
                            {
                                Console.WriteLine("====================================================================================");
                                Console.WriteLine("");
                                Perform_SendConsoleMessage($"Plugin is outdated! (Latest Version: {latestVersion})", ConsoleColor.DarkRed);
                                Perform_SendConsoleMessage($"Check: https://github.com/NockyCZ/CS2-Discord-Utilities/", ConsoleColor.DarkRed);
                                Console.WriteLine("");
                                Console.WriteLine("====================================================================================");
                            }
                        }
                    }
                    else
                        Perform_SendConsoleMessage($"Version je null", ConsoleColor.Red);

                }
                catch (HttpRequestException ex)
                {
                    Perform_SendConsoleMessage($"An error occurred while loading module versions: '{ex.Message}'", ConsoleColor.Red);
                }
                catch (Exception ex)
                {
                    Perform_SendConsoleMessage($"An error occurred while loading module versions: '{ex.Message}'", ConsoleColor.Red);
                }
            }
        }

        public void LoadCustomConditions()
        {
            customConditions.Clear();
            customVariables.Clear();
            // <css>/plugins/<Plugin> -> <css>/configs/plugins/<Plugin>/<Plugin>.json
            string pluginName = Path.GetFileName(ModuleDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string filePath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "configs", "plugins", pluginName, $"{pluginName}.json"));
            if (File.Exists(filePath))
            {
                try
                {
                    var jsonData = File.ReadAllText(filePath);
                    dynamic deserializedJson = JsonConvert.DeserializeObject(jsonData)!;

                    var conditions = deserializedJson["Custom Variables"].ToObject<Dictionary<string, List<ConditionData>>>();
                    if (conditions != null)
                    {
                        foreach (KeyValuePair<string, List<ConditionData>> item in conditions)
                        {
                            customConditions.Add($"[{item.Key}]", item.Value);

                            if (item.Key.Contains("Player."))
                                customVariables.Add($"[{item.Key}]", replaceDataType.Player);
                            else if (item.Key.Contains("Target."))
                                customVariables.Add($"[{item.Key}]", replaceDataType.Target);
                            else if (item.Key.Contains("Server."))
                                customVariables.Add($"[{item.Key}]", replaceDataType.Server);
                            else if (item.Key.Contains("DiscordUser."))
                                customVariables.Add($"[{item.Key}]", replaceDataType.DiscordUser);
                            else if (item.Key.Contains(value: "DiscordChannel."))
                                customVariables.Add($"[{item.Key}]", replaceDataType.DiscordChannel);
                            else
                            {
                                Perform_SendConsoleMessage($"Invalid Custom Variable Name '{item.Key}'", ConsoleColor.Red);
                                customConditions.Remove($"[{item.Key}]");
                            }
                        }
                    }
                    if (Config.Debug)
                        Perform_SendConsoleMessage($"Loaded total '{customConditions.Count} Custom Variables'", ConsoleColor.Cyan);
                }
                catch (Exception ex)
                {
                    Perform_SendConsoleMessage($"An error occurred while loading the Custom Variables: '{ex.Message}'", ConsoleColor.Red);
                }
            }
        }
        private static bool linkRoleMissingReported;

        private async Task LoadPlayerData(string steamid, ulong discordID)
        {
            try
            {
                if (!ulong.TryParse(steamid, out var steamId64))
                    return;

                var client = BotClient;
                if (client == null || !TryGetGuild(out var guild))
                    return;

                var user = guild.GetUser(discordID);
                if (user == null)
                {
                    // The member cache can be incomplete (right after a restart, or without the 'Server Members Intent'),
                    // so ask Discord directly before treating the link as dead.
                    var restUser = await client.Rest.GetGuildUserAsync(guild.Id, discordID);
                    if (restUser == null)
                    {
                        await RemovePlayerData(steamid);
                        Perform_SendConsoleMessage($"User with ID '{discordID}' is no longer on the Discord server! Player has been removed from the Linked players.", ConsoleColor.DarkYellow);
                        return;
                    }

                    if (!guild.HasAllMembers)
                        await guild.DownloadUsersAsync();

                    user = guild.GetUser(discordID);
                    if (user == null)
                    {
                        Perform_SendConsoleMessage($"User with ID '{discordID}' is on the Discord server but could not be loaded yet (Link). Check that the 'Server Members Intent' is enabled.", ConsoleColor.DarkYellow);
                        return;
                    }
                }

                var linkRoleValue = Config.Link.LinkDiscordSettings.LinkRole;
                if (!string.IsNullOrWhiteSpace(linkRoleValue))
                {
                    var linkRole = ulong.TryParse(linkRoleValue.Trim(), out var linkRoleId) ? guild.GetRole(linkRoleId) : null;
                    if (linkRole != null)
                        await PerformLinkRole(user, linkRole);
                    else if (!linkRoleMissingReported)
                    {
                        // Reported once, not on every player connect
                        linkRoleMissingReported = true;
                        Perform_SendConsoleMessage($"Role with id '{linkRoleValue}' was not found on the Discord server, the Link role will not be added! Check 'Link Role ID' in config.", ConsoleColor.Red);
                    }
                }

                Server.NextFrame(() =>
                {
                    LoadPlayerDiscordData(steamId64, discordID);
                    PerformLinkPermission(steamId64);
                });
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while loading linked player data: '{ex.Message}'", ConsoleColor.Red);
            }
        }

        private void LoadPlayerDiscordData(ulong steamid, ulong discordid)
        {
            if (!TryGetGuild(out var guild))
                return;
            var user = guild.GetUser(discordid);
            if (user == null)
                return;

            var target = GetTargetBySteamID64(steamid);
            if (target != null)
            {
                if (playerData.TryGetValue(target.Slot, out var p) && p != null)
                {
                    p.DiscordGlobalname = user.GlobalName;
                    p.DiscordDisplayName = user.DisplayName;
                    p.DiscordID = user.Id.ToString();
                    p.DiscordPing = $"<@{user.Id}>";
                    p.DiscordAvatar = user.GetAvatarUrl();
                }
                LinkedUserLoaded(user, target);
            }
        }
        private const string GeoDatabaseFile = "GeoLite2-Country.mmdb";
        private static bool geoDatabaseMissingReported;

        // The plugin folder is checked first, then '<css>/configs/geoip/' where some CounterStrikeSharp builds keep
        // an automatically updated copy. When both exist, the most recently written file is used.
        private string? GetGeoDatabasePath()
        {
            try
            {
                var pluginFile = Path.Combine(ModuleDirectory, GeoDatabaseFile);
                var sharedFile = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "configs", "geoip", GeoDatabaseFile));

                bool pluginExists = File.Exists(pluginFile);
                bool sharedExists = File.Exists(sharedFile);
                if (pluginExists && sharedExists)
                    return File.GetLastWriteTimeUtc(sharedFile) > File.GetLastWriteTimeUtc(pluginFile) ? sharedFile : pluginFile;
                if (pluginExists)
                    return pluginFile;
                if (sharedExists)
                    return sharedFile;
            }
            catch (Exception ex)
            {
                if (IsDebug)
                    Perform_SendConsoleMessage($"An error occurred while searching for '{GeoDatabaseFile}': '{ex.Message}'", ConsoleColor.Cyan);
            }
            return null;
        }

        // Does not touch any game API, so it can run outside of the game thread.
        // Returns [country name, ISO code] or null when the country is unknown.
        private string[]? LookupPlayerCountry(string IpAddress, ulong steamid)
        {
            var geoDatabasePath = GetGeoDatabasePath();
            if (geoDatabasePath == null)
            {
                // Reported once, not on every player connect
                if (!geoDatabaseMissingReported)
                {
                    geoDatabaseMissingReported = true;
                    Perform_SendConsoleMessage($"File '{GeoDatabaseFile}' was not found, player country variables will be empty. Copy it from the release package to '{ModuleDirectory}'", ConsoleColor.DarkYellow);
                }
                return null;
            }

            try
            {
                using (var reader = new DatabaseReader(geoDatabasePath))
                {
                    var response = reader.Country(IpAddress);
                    string[] country = new string[2];
                    country[0] = response.Country.Name!;
                    country[1] = response.Country.IsoCode!;
                    return country;
                }
            }
            catch (AddressNotFoundException ex)
            {
                if (IsDebug)
                    Perform_SendConsoleMessage($"Country of the player '{steamid}' was not found in '{GeoDatabaseFile}': '{ex.Message}'", ConsoleColor.Cyan);
            }
            catch (GeoIP2Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while searching IP adress: '{ex.Message}'.", ConsoleColor.Red);
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while searching IP adress: '{ex.Message}'.", ConsoleColor.Red);
            }
            return null;
        }

        private void UpdateServerData()
        {
            var timelimit = (ConVar<float>.Find("mp_timelimit")?.Value ?? 0f) * 60;
            var gameStart = GameRules().GameStartTime;
            var currentTime = Server.CurrentTime;

            var timeleft = (int)timelimit - (int)(currentTime - gameStart);
            TimeSpan time = TimeSpan.FromSeconds(timeleft);

            serverData.Name = ConVar<string>.Find("hostname")?.Value ?? serverData.Name;
            serverData.Timeleft = $"{time:mm\\:ss}";
            serverData.OnlinePlayers = GetPlayersCount().ToString();
            serverData.OnlinePlayersAndBots = GetPlayersCountWithBots().ToString();
            serverData.OnlineBots = GetBotsCounts().ToString();
            if (Config.BotStatus.UpdateStatus)
                _ = UpdateBotStatus();
        }

        private void UpdatePlayerCountry(ulong steamid, string[] country)
        {
            var target = GetTargetBySteamID64(steamid);
            if (target == null)
                return;

            var p = playerData[target.Slot];
            if (p != null)
            {
                if (!string.IsNullOrWhiteSpace(country[0]))
                    p.CountryLong = country[0];
                else
                    p.CountryLong = "Unknown";
                if (!string.IsNullOrWhiteSpace(country[1]))
                {
                    p.CountryShort = country[1].ToUpper();
                    string emoji = $":flag_{country[1].ToLower()}:";
                    p.CountryEmoji = IsValidFlag(emoji);
                }
                else
                {
                    p.CountryShort = "??";
                    p.CountryEmoji = ":flag_white:";
                }
            }
        }
        public async Task UpdateBotStatus()
        {
            try
            {
                if (BotClient == null || !IsBotConnected)
                    return;

                string ActivityFormat = ReplaceServerDataVariables(Config.BotStatus.ActivityFormat);
                if (LastBotActivityText == ActivityFormat)
                    return;

                //Console.WriteLine(ActivityFormat);
                LastBotActivityText = ActivityFormat;
                if ((ActivityType)Config.BotStatus.ActivityType == ActivityType.CustomStatus)
                    await BotClient.SetCustomStatusAsync(ActivityFormat);
                else
                    await BotClient.SetActivityAsync(new Game(ActivityFormat, (ActivityType)Config.BotStatus.ActivityType));
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"UpdateBotStatus Error: {ex.Message}", ConsoleColor.Red);
            }
        }
    }
}
