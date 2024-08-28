using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Admin;
using Discord;
using Discord.WebSocket;
using DiscordUtilitiesAPI.Builders;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public void PerformLinkPermission(ulong steamid)
        {
            var Permission = Config.Link.LinkIngameSettings.Flag;
            if (Permission.StartsWith('@') || Permission.StartsWith('#'))
            {

                var player = GetTargetBySteamID64(steamid);
                if (player == null || !player.IsValid)
                    return;

                if (!string.IsNullOrEmpty(Permission))
                {
                    if (Permission.StartsWith('@'))
                        AdminManager.AddPlayerPermissions(player, Permission);
                    else
                        AdminManager.AddPlayerToGroup(player, Permission);
                }
            }
            else
            {
                Perform_SendConsoleMessage($"Invalid permission '{Permission}'!", ConsoleColor.Red);
                return;
            }
        }
        public async Task RemoveLinkRole(ulong discordid)
        {
            try
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


                var role = guild.GetRole(ulong.Parse(Config.Link.LinkDiscordSettings.LinkRole));
                if (role == null)
                {
                    Perform_SendConsoleMessage($"Role with id '{Config.Link.LinkDiscordSettings.LinkRole}' was not found (Link Section)!", ConsoleColor.Red);
                    return;
                }
                if (user.Roles.Any(id => id == role))
                {
                    await user.RemoveRoleAsync(role);
                }
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while removing Link role: '{ex.Message}'", ConsoleColor.Red);
            }
        }

        public async Task PerformLinkRole(SocketGuildUser user, SocketRole? role)
        {
            try
            {
                if (role == null)
                    return;

                if (!user.Roles.Any(id => id == role))
                {
                    await user.AddRoleAsync(role);
                }
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while adding Link role: '{ex.Message}'", ConsoleColor.Red);
            }
        }

        public async Task SendUserLinkedAllMessage(SocketGuildUser user, string steamId)
        {
            if (string.IsNullOrEmpty(Config.Link.LinkDiscordSettings.SendLinkedMessageToAll) || !ulong.TryParse(Config.Link.LinkDiscordSettings.SendLinkedMessageToAll, out var channelId))
                return;

            if (BotClient!.GetChannel(channelId) is not IMessageChannel channel)
                return;

            var replaceVariablesBuilder = new ReplaceVariables.Builder
            {
                DiscordUser = GetUserDataByUserID(user.Id),
                CustomVariables = new(){
                    { "{STEAM}", steamId },
                }
            };

            var embed = GetEmbedBuilder(GetEmbedBuilderFromConfig(Config.Link.LinkEmbed.UserLinkedAll, replaceVariablesBuilder));
            var content = ReplaceVariables(Config.Link.LinkEmbed.UserLinkedAll.Content, replaceVariablesBuilder);
            await channel.SendMessageAsync(text: content, embed: embed.Build());
        }

        private async Task DiscordLink_CMD(SocketSlashCommand command)
        {
            if (IsDebug && Config.Link.ResponseServer)
                Perform_SendConsoleMessage($"Slash command '{command.CommandName}' has been successfully logged", ConsoleColor.Cyan);

            if (command.GuildId == null)
            {
                await command.RespondAsync(text: "This command cannot be used in a private message!");
                return;
            }

            ulong guildId = command.GuildId.Value;
            var guild = BotClient!.GetGuild(guildId);

            if (guild == null)
            {
                Perform_SendConsoleMessage($"Guild has not been found ('{guildId}') (DiscordLink_CMD)", ConsoleColor.Red);
                return;
            }

            var user = command.User as SocketGuildUser;
            if (user == null)
            {
                Perform_SendConsoleMessage($"User was not found! (DiscordLink_CMD)", ConsoleColor.Red);
                return;
            }

            var role = guild.GetRole(ulong.Parse(Config.Link.LinkDiscordSettings.LinkRole));
            if (role == null)
            {
                Perform_SendConsoleMessage($"Role with id '{Config.Link.LinkDiscordSettings.LinkRole}' was not found! (DiscordLink_CMD)", ConsoleColor.Red);
                return;
            }

            string content = string.Empty;
            var embed = new EmbedBuilder();
            if (linkedPlayers.ContainsValue(user.Id))
            {
                if (!Config.Link.ResponseServer)
                    return;

                var findSteamIdByUserId = linkedPlayers.FirstOrDefault(x => x.Value == user.Id).Key;
                var replaceVariablesBuilder = new ReplaceVariables.Builder
                {
                    CustomVariables = new(){
                        { "{STEAM}", findSteamIdByUserId.ToString() },
                    }
                };

                embed = GetEmbedBuilder(GetEmbedBuilderFromConfig(Config.Link.LinkEmbed.AlreadyLinked, replaceVariablesBuilder));
                content = Config.Link.LinkEmbed.AlreadyLinked.Content.Replace("{STEAM}", findSteamIdByUserId.ToString());

                await command.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
                return;
            }

            var code = command.Data.Options.First().Value.ToString();
            code = code!.Replace(" ", "");

            if (!string.IsNullOrEmpty(code) && linkCodes.ContainsKey(code))
            {
                var steamId = linkCodes[code];
                if (!Config.Link.ResponseServer)
                {
                    if (!linkedPlayers.ContainsKey(ulong.Parse(steamId)))
                    {
                        Server.NextFrame(() =>
                        {
                            linkedPlayers.Add(ulong.Parse(steamId), user.Id);
                            var player = Utilities.GetPlayerFromSteamId(ulong.Parse(steamId));
                            if (player != null)
                            {
                                _ = LoadPlayerData(steamId, user.Id);
                                if (Config.Link.LinkIngameSettings.SendLinkedMessageToPlayer)
                                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AccountLinked", user.DisplayName]}");
                                if (Config.Link.LinkIngameSettings.SendLinkedMessageToAll)
                                    Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AccountLinkedAll", player.PlayerName, user.DisplayName]}");
                                if (!string.IsNullOrEmpty(Config.Link.LinkIngameSettings.LinkedSound))
                                    player.ExecuteClientCommand($"play {Config.Link.LinkIngameSettings.LinkedSound}");
                            }
                        });
                    }
                    return;
                }

                var replaceVariablesBuilder = new ReplaceVariables.Builder
                {
                    CustomVariables = new(){
                        { "{STEAM}", steamId },
                    }
                };

                embed = GetEmbedBuilder(GetEmbedBuilderFromConfig(Config.Link.LinkEmbed.Success, replaceVariablesBuilder));
                content = Config.Link.LinkEmbed.Success.Content.Replace("{STEAM}", steamId);

                await InsertPlayerData(linkCodes[code].ToString(), command.User.Id.ToString(), user.DisplayName);
                await command.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
                if (!user.Roles.Any(id => id == role))
                {
                    await user.AddRoleAsync(role);
                }

                await SendUserLinkedAllMessage(user, steamId);
                await CreateScheduledEventAsync($"removecode;{code};{steamId}");
                return;
            }

            if (!Config.Link.ResponseServer)
                return;

            var replaceVariables = new ReplaceVariables.Builder
            {
                CustomVariables = new(){
                    { "{CODE}", code },
                }
            };

            embed = GetEmbedBuilder(GetEmbedBuilderFromConfig(Config.Link.LinkEmbed.Failed, replaceVariables));
            content = Config.Link.LinkEmbed.Failed.Content.Replace("{CODE}", code);
            await command.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
        }
    }
}