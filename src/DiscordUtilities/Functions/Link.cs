using CounterStrikeSharp.API.Modules.Admin;
using Discord;
using Discord.WebSocket;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public void PerformLinkPermission(ulong steamid)
        {
            var Permission = Config.Link.LinkPermissions;
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
                Perform_SendConsoleMessage($"[Discord Utilities] Invalid permission '{Permission}'!", ConsoleColor.Red);
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
                    Perform_SendConsoleMessage($"[Discord Utilities] Guild with id '{ServerId}' was not found!", ConsoleColor.Red);
                    return;
                }
                var user = guild.GetUser(discordid);
                if (user == null)
                    return;


                var role = guild.GetRole(ulong.Parse(Config.Link.LinkRole));
                if (role == null)
                {
                    Perform_SendConsoleMessage($"[Discord Utilities] Role with id '{Config.Link.LinkRole}' was not found (Link Section)!", ConsoleColor.Red);
                    return;
                }
                if (user.Roles.Any(id => id == role))
                {
                    await user.RemoveRoleAsync(role);
                }
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] An error occurred while removing Link role: {ex.Message}", ConsoleColor.Red);
            }
        }
        public async Task PerformLinkRole(string discordid)
        {
            try
            {
                var guild = BotClient!.GetGuild(ulong.Parse(ServerId));
                if (guild == null)
                {
                    Perform_SendConsoleMessage($"[Discord Utilities] Guild with id '{ServerId}' was not found!", ConsoleColor.Red);
                    return;
                }
                var user = guild.GetUser(ulong.Parse(discordid));
                if (user == null)
                    return;


                var role = guild.GetRole(ulong.Parse(Config.Link.LinkRole));
                if (role == null)
                {
                    Perform_SendConsoleMessage($"[Discord Utilities] Role with id '{Config.Link.LinkRole}' was not found (Link Section)!", ConsoleColor.Red);
                    return;
                }
                if (!user.Roles.Any(id => id == role))
                {
                    await user.AddRoleAsync(role);
                }
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] An error occurred while adding Link role: {ex.Message}", ConsoleColor.Red);
            }
        }

        private async Task DiscordLink_CMD(SocketSlashCommand command)
        {
            if (!Config.Link.ResponseServer)
                return;

            if (IsDebug)
                Perform_SendConsoleMessage($"[Discord Utilities] DEBUG: Slash command '{command.CommandName}' has been successfully logged", ConsoleColor.Cyan);

            ulong guildId = command.GuildId!.Value;
            var guild = BotClient!.GetGuild(guildId);

            if (guild == null)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] LINK Slash Command Error: Guild has not been found ('{guildId}')", ConsoleColor.Red);
                return;
            }

            var user = guild.GetUser(command.User.Id);
            if (user == null)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] LINK Slash Command Error: User was not found!", ConsoleColor.Red);
                return;
            }

            var role = guild.GetRole(ulong.Parse(Config.Link.LinkRole));
            if (role == null)
            {
                Perform_SendConsoleMessage($"[Discord Utilities] LINK Slash Command Error: Role with id '{Config.Link.LinkRole}' was not found!", ConsoleColor.Red);
                return;
            }

            string content = string.Empty;
            var embed = new EmbedBuilder();
            if (linkedPlayers.ContainsValue(user.Id))
            {
                var findSteamIdByUserId = linkedPlayers.FirstOrDefault(x => x.Value == user.Id).Key;
                content = Config.Link.LinkEmbed.AlreadyLinked.Content.Replace("{STEAM}", findSteamIdByUserId.ToString());

                embed = new EmbedBuilder();
                if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.AlreadyLinked.Title))
                {
                    var Title = Config.Link.LinkEmbed.AlreadyLinked.Title.Replace("{STEAM}", findSteamIdByUserId.ToString());
                    embed.WithTitle(Title);
                }
                if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.AlreadyLinked.Description))
                {
                    var Description = Config.Link.LinkEmbed.AlreadyLinked.Description.Replace("{STEAM}", findSteamIdByUserId.ToString());
                    embed.WithDescription(Description);
                }
                if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.AlreadyLinked.Fields))
                {
                    string[] fields = Config.Link.LinkEmbed.AlreadyLinked.Fields.Split('|');
                    foreach (var field in fields)
                    {
                        var replacedField = field.Replace("{STEAM}", findSteamIdByUserId.ToString());
                        string[] fieldData = replacedField.Split(';');
                        if (fieldData.Length == 3)
                            embed.AddField(fieldData[0], fieldData[1], bool.Parse(fieldData[2]));
                        else
                        {
                            Perform_SendConsoleMessage($"[Discord Utilities] Invalid Fields Format! ({Config.Link.LinkEmbed.AlreadyLinked.Fields})", ConsoleColor.DarkRed);
                            return;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.AlreadyLinked.Thumbnail))
                {
                    var value = Config.Link.LinkEmbed.AlreadyLinked.Thumbnail;
                    if (value.Contains(".jpg") || value.Contains(".png") || value.Contains(".gif"))
                        embed.WithThumbnailUrl(value);
                }
                if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.AlreadyLinked.Image))
                {
                    var value = Config.Link.LinkEmbed.AlreadyLinked.Image;
                    if (value.Contains(".jpg") || value.Contains(".png") || value.Contains(".gif"))
                        embed.WithImageUrl(value);
                }
                if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.AlreadyLinked.Color))
                {
                    var value = Config.Link.LinkEmbed.AlreadyLinked.Color;
                    if (value.StartsWith("#"))
                        value = value.Substring(1);
                    embed.WithColor(new Color(Convert.ToUInt32(value, 16)));
                }
                if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.AlreadyLinked.Footer))
                {
                    var Footer = Config.Link.LinkEmbed.AlreadyLinked.Footer.Replace("{STEAM}", findSteamIdByUserId.ToString());
                    embed.WithFooter(Footer);
                }
                if (Config.Link.LinkEmbed.AlreadyLinked.FooterTimestamp)
                    embed.WithCurrentTimestamp();

                await command.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
                return;
            }

            var code = command.Data.Options.First().Value.ToString();
            code = code!.Replace(" ", "");

            if (!string.IsNullOrEmpty(code) && linkCodes.ContainsKey(code))
            {
                var steamId = linkCodes[code];
                content = Config.Link.LinkEmbed.Success.Content.Replace("{STEAM}", steamId);

                embed = new EmbedBuilder();
                if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.Success.Title))
                {
                    var Title = Config.Link.LinkEmbed.Success.Title.Replace("{STEAM}", steamId);
                    embed.WithTitle(Title);
                }
                if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.Success.Description))
                {
                    var Description = Config.Link.LinkEmbed.Success.Description.Replace("{STEAM}", steamId);
                    embed.WithDescription(Description);
                }
                if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.Success.Fields))
                {
                    string[] fields = Config.Link.LinkEmbed.Success.Fields.Split('|');
                    foreach (var field in fields)
                    {
                        var replacedField = field.Replace("{STEAM}", steamId);
                        string[] fieldData = replacedField.Split(';');
                        if (fieldData.Length == 3)
                            embed.AddField(fieldData[0], fieldData[1], bool.Parse(fieldData[2]));
                        else
                        {
                            Perform_SendConsoleMessage($"[Discord Utilities] Invalid Fields Format! ({Config.Link.LinkEmbed.Success.Fields})", ConsoleColor.DarkRed);
                            return;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.Success.Thumbnail))
                {
                    var value = Config.Link.LinkEmbed.Success.Thumbnail;
                    if (value.Contains(".jpg") || value.Contains(".png") || value.Contains(".gif"))
                        embed.WithThumbnailUrl(value);
                }
                if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.Success.Image))
                {
                    var value = Config.Link.LinkEmbed.Success.Image;
                    if (value.Contains(".jpg") || value.Contains(".png") || value.Contains(".gif"))
                        embed.WithImageUrl(value);
                }
                if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.Success.Color))
                {
                    var value = Config.Link.LinkEmbed.Success.Color;
                    if (value.StartsWith("#"))
                        value = value.Substring(1);
                    embed.WithColor(new Color(Convert.ToUInt32(value, 16)));
                }
                if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.Success.Footer))
                {
                    var Footer = Config.Link.LinkEmbed.Success.Footer.Replace("{STEAM}", steamId);
                    embed.WithFooter(Footer);
                }
                if (Config.Link.LinkEmbed.Success.FooterTimestamp)
                    embed.WithCurrentTimestamp();

                await InsertPlayerData(linkCodes[code].ToString(), command.User.Id.ToString());
                await RemoveCode(code);
                await command.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
                if (!user.Roles.Any(id => id == role))
                {
                    await user.AddRoleAsync(role);
                }
                return;
            }

            content = Config.Link.LinkEmbed.Failed.Content.Replace("{CODE}", code);
            embed = new EmbedBuilder();
            if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.Failed.Title))
            {
                var Title = Config.Link.LinkEmbed.Failed.Title.Replace("{CODE}", code);
                embed.WithTitle(Title);
            }
            if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.Failed.Description))
            {
                var Description = Config.Link.LinkEmbed.Failed.Description.Replace("{CODE}", code);
                embed.WithDescription(Description);
            }
            if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.Failed.Fields))
            {
                string[] fields = Config.Link.LinkEmbed.Failed.Fields.Split('|');
                foreach (var field in fields)
                {
                    var replacedField = field.Replace("{CODE}", code);
                    string[] fieldData = replacedField.Split(';');
                    if (fieldData.Length == 3)
                        embed.AddField(fieldData[0], fieldData[1], bool.Parse(fieldData[2]));
                    else
                    {
                        Perform_SendConsoleMessage($"[Discord Utilities] Invalid Fields Format! ({Config.Link.LinkEmbed.Failed.Fields})", ConsoleColor.DarkRed);
                        return;
                    }
                }
            }
            if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.Failed.Thumbnail))
            {
                var value = Config.Link.LinkEmbed.Failed.Thumbnail;
                if (value.Contains(".jpg") || value.Contains(".png") || value.Contains(".gif"))
                    embed.WithThumbnailUrl(value);
            }
            if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.Failed.Image))
            {
                var value = Config.Link.LinkEmbed.Failed.Image;
                if (value.Contains(".jpg") || value.Contains(".png") || value.Contains(".gif"))
                    embed.WithImageUrl(value);
            }
            if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.Failed.Color))
            {
                var value = Config.Link.LinkEmbed.Failed.Color;
                if (value.StartsWith("#"))
                    value = value.Substring(1);
                embed.WithColor(new Color(Convert.ToUInt32(value, 16)));
            }
            if (!string.IsNullOrEmpty(Config.Link.LinkEmbed.Failed.Footer))
            {
                var Footer = Config.Link.LinkEmbed.Failed.Footer.Replace("{CODE}", code);
                embed.WithFooter(Footer);
            }
            if (Config.Link.LinkEmbed.Failed.FooterTimestamp)
                embed.WithCurrentTimestamp();

            await command.RespondAsync(text: string.IsNullOrEmpty(content) ? null : content, embed: IsEmbedValid(embed) ? embed.Build() : null, ephemeral: true);
        }
    }
}