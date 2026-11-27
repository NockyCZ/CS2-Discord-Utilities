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
                if (!TryGetGuild(out var guild))
                    return;
                var user = guild.GetUser(discordid);
                if (user == null)
                    return;


                if (!ulong.TryParse(Config.Link.LinkDiscordSettings.LinkRole, out var roleId))
                    return;

                var role = guild.GetRole(roleId);
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
                Perform_SendConsoleMessage($"An error occurred while removing Link role: '{ex.Message}'{GetMissingPermissionHint(ex, "'Manage Roles', and move the role of the Discord BOT above the Link role")}", ConsoleColor.Red);
            }
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, byte> reportedRoleProblems = new();

        // Discord only answers "Missing Permissions" (50013), so the exact reason is worked out before the request
        private static string? GetRoleAssignProblem(SocketRole role)
        {
            var bot = role.Guild.CurrentUser;
            if (bot == null)
                return null;

            if (role.IsManaged)
                return "it is managed by an integration (bot or booster role) and cannot be given to users - use a different role in 'Link Role ID'";
            if (role.IsEveryone)
                return "'Link Role ID' is the ID of the '@everyone' role (same as the server ID) - use the ID of a real role";
            if (!bot.GuildPermissions.Administrator && !bot.GuildPermissions.ManageRoles)
                return "the Discord BOT is missing the permission 'Manage Roles'";
            if (bot.Hierarchy <= role.Position)
                return $"the highest role of the Discord BOT is not above it - in Discord Server Settings -> Roles, drag the role of the Discord BOT above '{role.Name}'";

            return null;
        }

        public async Task PerformLinkRole(SocketGuildUser user, SocketRole? role)
        {
            try
            {
                if (role == null)
                    return;

                if (!user.Roles.Any(id => id == role))
                {
                    var problem = GetRoleAssignProblem(role);
                    if (problem != null)
                    {
                        // Reported once per role, not on every player connect
                        if (reportedRoleProblems.TryAdd(role.Id, 0))
                            Perform_SendConsoleMessage($"Link role '{role.Name}' cannot be added: {problem}", ConsoleColor.Red);
                        return;
                    }

                    await user.AddRoleAsync(role);
                }
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while adding Link role: '{ex.Message}'{GetMissingPermissionHint(ex, "'Manage Roles', and move the role of the Discord BOT above the Link role")}", ConsoleColor.Red);
            }
        }

        public async Task SendUserLinkedAllMessage(SocketGuildUser user, string steamId)
        {
            if (string.IsNullOrEmpty(Config.Link.LinkDiscordSettings.SendLinkedMessageToAll) || !ulong.TryParse(Config.Link.LinkDiscordSettings.SendLinkedMessageToAll, out var channelId))
                return;

            if (BotClient?.GetChannel(channelId) is not IMessageChannel channel)
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
            try
            {
                await HandleDiscordLinkCommand(command);
            }
            catch (Exception ex)
            {
                // Without this the exception is swallowed by Discord.Net and the user only sees "The application did not respond"
                Perform_SendConsoleMessage($"An error occurred while processing the Link Slash Command: '{ex.Message}'", ConsoleColor.Red);
            }
        }

        private async Task HandleDiscordLinkCommand(SocketSlashCommand command)
        {
            if (IsDebug && Config.Link.ResponseServer)
                Perform_SendConsoleMessage($"Slash command '{command.CommandName}' has been successfully logged", ConsoleColor.Cyan);

            if (command.GuildId == null)
            {
                await command.RespondAsync(text: "This command cannot be used in a private message!");
                return;
            }

            ulong guildId = command.GuildId.Value;
            var guild = BotClient?.GetGuild(guildId);

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

            // The link role is optional, a missing or invalid role must not block the linking itself
            SocketRole? role = null;
            var linkRole = Config.Link.LinkDiscordSettings.LinkRole;
            if (!string.IsNullOrWhiteSpace(linkRole))
            {
                if (ulong.TryParse(linkRole.Trim(), out var linkRoleId))
                    role = guild.GetRole(linkRoleId);

                if (role == null && Config.Link.ResponseServer)
                    Perform_SendConsoleMessage($"Role with id '{linkRole}' was not found, the Link role will not be added! (DiscordLink_CMD)", ConsoleColor.Red);
            }

            string content = string.Empty;
            var embed = new EmbedBuilder();
            if (TryGetSteamIdByDiscordId(user.Id, out var findSteamIdByUserId))
            {
                if (!Config.Link.ResponseServer)
                    return;

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

            if (!string.IsNullOrEmpty(code) && linkCodes.TryGetValue(code, out var steamId))
            {
                if (!ulong.TryParse(steamId, out var steamId64))
                {
                    Perform_SendConsoleMessage($"Link code belongs to an invalid SteamID '{steamId}'! (DiscordLink_CMD)", ConsoleColor.Red);
                    return;
                }

                if (!Config.Link.ResponseServer)
                {
                    if (linkedPlayers.TryAdd(steamId64, user.Id))
                    {
                        Server.NextFrame(() =>
                        {
                            var player = Utilities.GetPlayerFromSteamId(steamId64);
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

                // Deferred first, the database insert can take longer than the 3 seconds Discord allows for a response
                await command.DeferAsync(ephemeral: true);
                await InsertPlayerData(steamId, command.User.Id.ToString(), user.DisplayName);
                await command.FollowupAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
                await PerformLinkRole(user, role);

                await SendUserLinkedAllMessage(user, steamId);
                linkCodes.TryRemove(code, out _);
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