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
        public async Task LoadMapImages()
        {
            mapImagesList.Clear();
            using (HttpClient client = new HttpClient())
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
            using (HttpClient client = new HttpClient())
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
            string filePath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/DiscordUtilities/DiscordUtilities.json";
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
                                customConditions.Remove(item.Key);
                            }
                        }
                    }
                    if (Config.Debug)
                        Perform_SendConsoleMessage($"Loaded total '{customConditions.Count} Custom Variables'", ConsoleColor.Cyan);
                }
                catch (Exception ex)
                {
                    Perform_SendConsoleMessage($"An error occurred while loading the Custom Variables: '{ex.Message}'", ConsoleColor.Red);
                    throw new Exception($"An error occurred while loading the Custom Variables: {ex.Message}");
                }
            }
        }
        private async Task LoadPlayerData(string steamid, ulong discordID)
        {
            var guild = BotClient!.GetGuild(ulong.Parse(ServerId));
            if (guild == null)
            {
                Perform_SendConsoleMessage($"Guild with id '{ServerId}' was not found!", ConsoleColor.Red);
                return;
            }
            var user = guild.GetUser(discordID);
            if (user == null)
            {
                await RemovePlayerData(steamid);
                Perform_SendConsoleMessage($"User with ID '{discordID}' was not found! Player has been removed from the Linked players.", ConsoleColor.DarkYellow);
                return;
            }

            await PerformLinkRole(user, guild.GetRole(ulong.Parse(Config.Link.LinkDiscordSettings.LinkRole)));
            LoadPlayerDiscordData(ulong.Parse(steamid), discordID);

            Server.NextFrame(() =>
            {
                PerformLinkPermission(ulong.Parse(steamid));
            });
        }

        private void LoadPlayerDiscordData(ulong steamid, ulong discordid)
        {
            var guild = BotClient!.GetGuild(ulong.Parse(ServerId));
            if (guild == null)
            {
                Perform_SendConsoleMessage($"Guild with id '{ServerId}' was not found!", ConsoleColor.Red);
                return;
            }
            var user = guild.GetUser(discordid);
            if (user == null)
                return;

            var target = GetTargetBySteamID64(steamid);
            if (target != null)
            {
                var p = playerData[target.Slot];
                if (p != null)
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
        private void LoadPlayerCountry(string IpAddress, ulong steamid)
        {
            try
            {
                using (var reader = new DatabaseReader(ModuleDirectory + "/GeoLite2-Country.mmdb"))
                {
                    var response = reader.Country(IpAddress);
                    string[] country = new string[2];
                    country[0] = response.Country.Name!;
                    country[1] = response.Country.IsoCode!;
                    UpdatePlayerCountry(steamid, country);
                }
            }
            catch (AddressNotFoundException ex)
            {
                Perform_SendConsoleMessage($"IP Adress '{ex}' was not found.", ConsoleColor.Red);
            }
            catch (GeoIP2Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while searching IP adress: '{ex.Message}'.", ConsoleColor.Red);
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while searching IP adress: '{ex.Message}'.", ConsoleColor.Red);
            }
        }

        private void UpdateServerData()
        {
            var timelimit = ConVar.Find("mp_timelimit")!.GetPrimitiveValue<float>() * 60;
            var gameStart = GameRules().GameStartTime;
            var currentTime = Server.CurrentTime;

            var timeleft = (int)timelimit - (int)(currentTime - gameStart);
            TimeSpan time = TimeSpan.FromSeconds(timeleft);

            serverData.Name = ConVar.Find("hostname")!.StringValue;
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
