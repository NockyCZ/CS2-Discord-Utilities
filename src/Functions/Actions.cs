using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Discord;
using CounterStrikeSharp.API.Modules.Cvars;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        private async Task SendDiscordMessage(EmbedBuilder embed, string content, ulong channelid, string section)
        {
            if (BotClient?.GetChannel(channelid) is not IMessageChannel channel)
            {
                SendConsoleMessage($"[Discord Utilities] Invalid Channel ID '{channelid}'.", ConsoleColor.Red);
                return;
            }
            await channel.SendMessageAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null);
        }

        private void LoadPlayerDiscordData(ulong steamid, ulong discordid)
        {
            var guild = BotClient!.GetGuild(ulong.Parse(Config.ServerID));
            if (guild == null)
            {
                SendConsoleMessage($"[Discord Utilities] Guild with id '{Config.ServerID}' was not found!", ConsoleColor.Red);
                return;
            }
            var user = guild.GetUser(discordid);
            if (user == null)
                return;
            
            var target = GetTargetBySteamID64(steamid);
            var p = playerData[target];
            if (p != null)
            {
                p.DiscordGlobalname = user.GlobalName;
                p.DiscordDisplayName = user.DisplayName;
                p.DiscordID = user.Id.ToString();
                p.DiscordPing = $"<@{user.Id}>";
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
                SendConsoleMessage($"[Discord Utilities] IP Adress '{ex}' was not found.", ConsoleColor.Red);
            }
            catch (GeoIP2Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while searching IP adress: {ex.Message}.", ConsoleColor.Red);
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while searching IP adress: {ex.Message}.", ConsoleColor.Red);
            }
        }

        private void UpdateServerData()
        {
            var timelimit = ConVar.Find("mp_timelimit")!.GetPrimitiveValue<float>() * 60;
            var gameStart = GameRules().GameStartTime;
            var currentTime = Server.CurrentTime;

            var timeleft = (int)timelimit - (int)(currentTime - gameStart);
            TimeSpan time = TimeSpan.FromSeconds(timeleft);

            serverData!.Timeleft = $"{time:mm\\:ss}";
            serverData.OnlinePlayers = GetPlayersCount().ToString();
            serverData.OnlinePlayersAndBots = GetPlayersCountWithBots().ToString();
            serverData.OnlineBots = GetBotsCounts().ToString();
        }
        private void UpdatePlayerCountry(ulong steamid, string[] country)
        {
            var target = GetTargetBySteamID64(steamid);
            var p = playerData[target];
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
                    p.CountryEmoji = emoji;
                }
                else
                {
                    p.CountryShort = "??";
                    p.CountryEmoji = ":flag_white:";
                }
            }
        }
        private void UpdatePlayerData(CCSPlayerController player, int Event, int team = 0)
        {
            if (player == null || !player.IsValid || player.AuthorizedSteamID == null)
                return;

            var p = playerData[player];
            if (p != null)
            {
                switch (Event)
                {
                    case 1: //playerDeath
                        p.Kills = player.ActionTrackingServices!.MatchStats.Kills.ToString();
                        p.Deaths = player.ActionTrackingServices.MatchStats.Deaths.ToString();
                        p.Assists = player.ActionTrackingServices.MatchStats.Assists.ToString();
                        p.Points = player.Score.ToString();
                        break;
                    case 2: //teamChange
                        p.TeamShortName = GetTeamShortName(team);
                        p.TeamLongName = GetTeamLongName(team);
                        p.TeamNumber = team.ToString();
                        break;
                }
            }
        }

        public void UpdateDiscordChannelID(string channelid)
        {
            string filePath = $"{serverData!.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/DiscordUtilities/DiscordUtilities.json";
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                JObject jsonObject = JObject.Parse(jsonContent);

                jsonObject["Server Status Section"]!["Message ID"] = channelid;
                File.WriteAllText(filePath, jsonObject.ToString(Formatting.Indented));

                SendConsoleMessage($"[Discord Utilities] Server Status successfully configured. Message ID has been automatically added ({channelid})", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Discord Utilities] An error occurred while creating Server Status Message: {ex.Message}", ConsoleColor.Green);
            }
        }

    }
}
