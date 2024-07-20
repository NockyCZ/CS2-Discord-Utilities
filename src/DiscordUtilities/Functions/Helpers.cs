using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Discord;
using DiscordUtilitiesAPI.Builders;
using DiscordUtilitiesAPI.Helpers;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public MessageBuilders? GetMessageBuilders(IUserMessage? message)
        {
            var messageBuilders = new MessageBuilders();
            if (message != null)
            {
                if (!string.IsNullOrEmpty(message.Content))
                    messageBuilders.Content = message.Content;

                var messageEmbeds = message.Embeds;
                var messageComponents = message.Components;
                if (messageComponents != null && messageComponents.Count() > 0)
                {
                    List<Components.Builder> componentsList = new();
                    foreach (var component in messageComponents)
                    {
                        var ComponentsBuilder = new Components.Builder();
                        if (component is ButtonComponent button)
                        {
                            if (string.IsNullOrEmpty(button.Url))
                            {
                                var interactiveButton = new Components.InteractiveButtonsBuilder()
                                {
                                    CustomId = button.CustomId,
                                    Label = button.Label,
                                    Emoji = button.Emote.Name,
                                    Color = (Components.ButtonColor)button.Style,
                                };
                            }
                            else
                            {
                                var linkButton = new Components.LinkButtonsBuilder()
                                {
                                    Label = button.Label,
                                    URL = button.Url,
                                    Emoji = button.Emote.Name,
                                };
                            }
                        }
                        else if (component is SelectMenuComponent selectMenu)
                        {
                            var interactiveMenu = new Components.InteractiveMenusBuilder()
                            {
                                CustomId = selectMenu.CustomId,
                                Placeholder = selectMenu.Placeholder,
                                MinValues = selectMenu.MinValues,
                                MaxValues = selectMenu.MaxValues,
                                Options = new List<Components.InteractiveMenusOptions>()
                            };
                            interactiveMenu.Options.AddRange(selectMenu.Options.Select(option => new Components.InteractiveMenusOptions
                            {
                                Label = option.Label,
                                Value = option.Value,
                                Description = option.Description,
                                Emoji = option.Emote.Name
                            }));
                        }

                        componentsList.Add(ComponentsBuilder);
                    }
                }
                if (messageEmbeds != null && messageEmbeds.Count() > 0)
                {
                    List<Embeds.Builder> embedsList = new();
                    foreach (var embed in messageEmbeds)
                    {
                        var EmbedBuilder = new Embeds.Builder();

                        if (!string.IsNullOrEmpty(embed.Title))
                            EmbedBuilder.Title = embed.Title;
                        if (!string.IsNullOrEmpty(embed.Description))
                            EmbedBuilder.Description = embed.Description;

                        if (embed.Fields.Count() > 0)
                        {
                            EmbedBuilder.Fields.AddRange(embed.Fields.Select(field => new Embeds.FieldsData
                            {
                                Title = field.Name,
                                Description = field.Value,
                                Inline = field.Inline
                            }));
                        }

                        if (embed.Thumbnail.HasValue)
                            EmbedBuilder.ThumbnailUrl = embed.Thumbnail.Value.Url;

                        if (embed.Image.HasValue)
                            EmbedBuilder.ImageUrl = embed.Image.Value.Url;

                        if (embed.Color.HasValue)
                            EmbedBuilder.Color = embed.Color.Value.ToString();

                        if (embed.Footer.HasValue)
                            EmbedBuilder.Footer = embed.Footer.Value.Text;

                        embedsList.Add(EmbedBuilder);
                    }
                    messageBuilders.Embeds = embedsList;
                }
            }
            return messageBuilders;
        }

        public ModalBuilder GetModalBuilder(DiscordUtilitiesAPI.Builders.Modal.Builder modalBuilder)
        {
            var modal = new ModalBuilder()
                .WithTitle(modalBuilder.Title)
                .WithCustomId(modalBuilder.CustomId);

            foreach (var input in modalBuilder.ModalInputs)
            {
                modal.AddTextInput(label: input.Label, customId: input.CustomId, style: (TextInputStyle)input.InputStyle, placeholder: input.Placeholder, value: string.IsNullOrEmpty(input.Value) ? null : input.Value, minLength: input.MinLength, maxLength: input.MaxLength, required: input.Required);
            }

            return modal;
        }

        public ComponentBuilder GetComponentsBuilder(Components.Builder componentBuilder)
        {
            var component = new ComponentBuilder();
            if (componentBuilder.InteractiveButtons != null)
            {
                foreach (var buttonData in componentBuilder.InteractiveButtons)
                {
                    var buttonBuilder = new ButtonBuilder()
                        .WithCustomId(buttonData.CustomId)
                        .WithLabel(buttonData.Label)
                        .WithStyle((ButtonStyle)buttonData.Color);

                    if (!string.IsNullOrEmpty(buttonData.Emoji))
                    {
                        if (!IsDefaultEmoji(buttonData.Emoji))
                        {
                            if (IsValidCustomEmoji(buttonData.Emoji))
                                buttonBuilder.WithEmote(Emote.Parse(buttonData.Emoji));
                        }
                        else
                            buttonBuilder.WithEmote(Emoji.Parse(buttonData.Emoji));
                    }

                    component.WithButton(buttonBuilder);
                }
            }
            if (componentBuilder.LinkButtons != null)
            {
                foreach (var buttonData in componentBuilder.LinkButtons)
                {
                    var buttonBuilder = new ButtonBuilder()
                        .WithLabel(buttonData.Label)
                        .WithStyle(ButtonStyle.Link)
                        .WithUrl(buttonData.URL);

                    if (!string.IsNullOrEmpty(buttonData.Emoji))
                    {
                        if (!IsDefaultEmoji(buttonData.Emoji))
                        {
                            if (IsValidCustomEmoji(buttonData.Emoji))
                                buttonBuilder.WithEmote(Emote.Parse(buttonData.Emoji));
                        }
                        else
                            buttonBuilder.WithEmote(Emoji.Parse(buttonData.Emoji));
                    }

                    component.WithButton(buttonBuilder);
                }
            }
            if (componentBuilder.InteractiveMenus != null)
            {
                foreach (var menuData in componentBuilder.InteractiveMenus)
                {
                    var menuBuilder = new SelectMenuBuilder()
                        .WithPlaceholder(menuData.Placeholder)
                        .WithCustomId(menuData.CustomId)
                        .WithMinValues(menuData.MinValues)
                        .WithMaxValues(menuData.MaxValues);

                    foreach (var option in menuData.Options)
                    {
                        menuBuilder.AddOption(label: option.Label, value: option.Value, description: string.IsNullOrEmpty(option.Description) ? null : option.Description, emote: string.IsNullOrEmpty(option.Emoji) ? null : !IsDefaultEmoji(option.Emoji) ? (IsValidCustomEmoji(option.Emoji) ? Emote.Parse(option.Emoji) : null) : Emoji.Parse(option.Emoji));
                    }
                    component.WithSelectMenu(menuBuilder);
                }
            }

            return component;
        }

        public EmbedBuilder GetEmbedBuilder(Embeds.Builder embedBuilder)
        {
            var embed = new EmbedBuilder();
            if (!string.IsNullOrEmpty(embedBuilder.Title))
            {
                embed.WithTitle(embedBuilder.Title);
                //Console.WriteLine("WithTitle");
            }
            if (!string.IsNullOrEmpty(embedBuilder.Description))
            {
                embed.WithDescription(embedBuilder.Description);
                //Console.WriteLine("WithDescription");
            }
            if (embedBuilder.ThumbnailUrl.Contains(".jpg") || embedBuilder.ThumbnailUrl.Contains(".png") || embedBuilder.ThumbnailUrl.Contains(".gif"))
            {
                embed.WithThumbnailUrl(embedBuilder.ThumbnailUrl);
                //Console.WriteLine("WithThumbnailUrl");
            }
            if (embedBuilder.ImageUrl.Contains(".jpg") || embedBuilder.ImageUrl.Contains(".png") || embedBuilder.ImageUrl.Contains(".gif"))
            {
                embed.WithImageUrl(embedBuilder.ImageUrl);
                //Console.WriteLine("WithImageUrl");
            }
            if (!string.IsNullOrEmpty(embedBuilder.Footer))
            {
                embed.WithFooter(embedBuilder.Footer);
                //Console.WriteLine("WithFooter");
            }
            if (embedBuilder.FooterTimestamp)
            {
                embed.WithCurrentTimestamp();
                //Console.WriteLine("WithCurrentTimestamp");
            }

            if (embedBuilder.fields.Count() > 0)
            {
                foreach (var field in embedBuilder.fields)
                {
                    embed.AddField(field.Title, field.Description, field.Inline);
                }
            }
            if (!string.IsNullOrEmpty(embedBuilder.Color))
            {
                var color = embedBuilder.Color;
                if (color.StartsWith("#"))
                    color = color.Substring(1);
                embed.WithColor(new Color(Convert.ToUInt32(color, 16)));
                //Console.WriteLine("WithColor");
            }
            return embed;
        }

        private CCSPlayerController? GetTargetBySteamID64(ulong steamid)
        {
            foreach (var p in Utilities.GetPlayers().Where(p => p != null && p.IsValid && p.SteamID.ToString().Length == 17 && p.AuthorizedSteamID != null))
            {
                if (p.AuthorizedSteamID!.SteamId64 == steamid)
                    return p;
            }
            return null;
        }

        private string GetRandomCode(int length, bool onlyNumbers = false)
        {
            string chars;
            if (onlyNumbers)
                chars = "0123456789";
            else
                chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            var random = new Random();
            var keyBuilder = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                keyBuilder.Append(chars[random.Next(chars.Length)]);
            }

            return keyBuilder.ToString();
        }

        public static string ReplacePlayerDataVariables(string replacedString, CCSPlayerController target, bool isTarget = false, bool checkCustomVariables = true)
        {
            PlayerData? selectedPlayer = null;
            if (playerData.ContainsKey(target.Slot))
            {
                selectedPlayer = playerData[target.Slot];
            }
            else
            {
                return replacedString;
            }

            var team = target.TeamNum;
            string player = isTarget ? "Target" : "Player";
            if (selectedPlayer != null)
            {
                var replacedData = new Dictionary<string, string>
                {
                    { $"{{{player}.Name}}", selectedPlayer.Name},
                    { $"{{{player}.NameWithoutEmoji}}", selectedPlayer.Name},
                    { $"{{{player}.UserID}}", target.UserId.ToString()!},
                    { $"{{{player}.SteamID32}}", selectedPlayer.SteamId32},
                    { $"{{{player}.SteamID64}}", selectedPlayer.SteamId64},
                    { $"{{{player}.IpAddress}}", selectedPlayer.IpAddress},
                    { $"{{{player}.CommunityUrl}}", selectedPlayer.CommunityUrl},
                    { $"{{{player}.PlayedTime}}", (selectedPlayer.PlayedTime / 60).ToString()},
                    { $"{{{player}.FirstJoin}}", selectedPlayer.FirstJoin.ToString(DateFormat)},
                    { $"{{{player}.LastSeen}}", selectedPlayer.LastSeen.ToString(DateFormat)},
                    { $"{{{player}.TeamShortName}}", GetTeamShortName(team)},
                    { $"{{{player}.TeamLongName}}", GetTeamLongName(team)},
                    { $"{{{player}.TeamNumber}}", team.ToString()},
                    { $"{{{player}.Kills}}", target.ActionTrackingServices!.MatchStats.Kills.ToString()},
                    { $"{{{player}.Deaths}}", target.ActionTrackingServices.MatchStats.Deaths.ToString()},
                    { $"{{{player}.KD}}", target.ActionTrackingServices!.MatchStats.Deaths != 0 ? (target.ActionTrackingServices.MatchStats.Kills / (double)target.ActionTrackingServices.MatchStats.Deaths).ToString("F2") : target.ActionTrackingServices.MatchStats.Kills.ToString()},
                    { $"{{{player}.Assists}}", target.ActionTrackingServices.MatchStats.Assists.ToString()},
                    { $"{{{player}.Points}}", target.Score.ToString()},
                    { $"{{{player}.CountryShort}}", selectedPlayer.CountryShort},
                    { $"{{{player}.CountryLong}}", selectedPlayer.CountryLong},
                    { $"{{{player}.CountryEmoji}}", selectedPlayer.CountryEmoji},
                    { $"{{{player}.DiscordGlobalname}}", selectedPlayer.DiscordGlobalname},
                    { $"{{{player}.DiscordDisplayName}}", selectedPlayer.DiscordDisplayName},
                    { $"{{{player}.DiscordAvatar}}", selectedPlayer.DiscordAvatar},
                    { $"{{{player}.DiscordPing}}", selectedPlayer.DiscordPing},
                    { $"{{{player}.DiscordID}}", selectedPlayer.DiscordID}
                };

                foreach (var item in replacedData)
                {
                    if (replacedString.Contains(item.Key))
                    {
                        replacedString = replacedString.Replace(item.Key, item.Value);
                    }
                }

                if (UseCustomVariables && checkCustomVariables)
                {
                    foreach (var item in customVariables.Where(x => x.Value == (isTarget ? replaceDataType.Target : replaceDataType.Player)))
                    {
                        if (replacedString.Contains(item.Key))
                        {
                            var replaceData = new replaceData()
                            {
                                Player = target,
                                Target = target,
                            };
                            replacedString = replacedString.Replace(item.Key, ReplaceConditions(item.Key, replaceData, isTarget ? replaceDataType.Target : replaceDataType.Player));
                        }
                    }
                }
            }
            return replacedString;
        }

        public static string ReplaceServerDataVariables(string replacedString, bool checkCustomVariables = true)
        {
            var mapName = mapImagesList.Contains(serverData.MapName) ? serverData.MapName : "notfound";

            var replacedData = new Dictionary<string, string>
            {
                { "{Server.Name}", serverData.Name },
                { "{Server.MaxPlayers}", serverData.MaxPlayers },
                { "{Server.MapName}", serverData.MapName },
                { "{Server.Timeleft}", serverData.Timeleft },
                { "{Server.OnlinePlayers}", serverData.OnlinePlayers },
                { "{Server.OnlinePlayersAndBots}", serverData.OnlinePlayersAndBots },
                { "{Server.OnlineBots}", serverData.OnlineBots },
                { "{Server.IP}", serverData.IP },
                { "{Server.MapImageUrl}", $"https://nockycz.github.io/CS2-Discord-Utilities/MapImages/{mapName}.png" },
                { "{Server.JoinUrl}", $"https://nockycz.github.io/CS2-Discord-Utilities/API/join.html?address={serverData.IP}" },
                { "{Server.TeamScoreCT}", serverData.TeamScoreCT },
                { "{Server.TeamScoreT}", serverData.TeamScoreT }
            };

            foreach (var item in replacedData)
            {
                if (replacedString.Contains(item.Key))
                {
                    replacedString = replacedString.Replace(item.Key, item.Value);
                }
            }

            if (UseCustomVariables && checkCustomVariables)
            {
                foreach (var item in customVariables.Where(x => x.Value == replaceDataType.Server))
                {
                    if (replacedString.Contains(item.Key))
                    {
                        var replaceData = new replaceData()
                        {
                            Server = true,
                        };
                        replacedString = replacedString.Replace(item.Key, ReplaceConditions(item.Key, replaceData, replaceDataType.Server));
                    }
                }
            }
            return replacedString;
        }
        public static string ReplaceDiscordUserVariables(UserData user, string replacedString, bool checkCustomVariables = true)
        {
            var replacedData = new Dictionary<string, string>
            {
                { "{DiscordUser.DisplayName}", user.DisplayName },
                { "{DiscordUser.GlobalName}", user.GlobalName },
                { "{DiscordUser.ID}", user.ID.ToString() }
            };
            foreach (var item in replacedData)
            {
                if (replacedString.Contains(item.Key))
                {
                    replacedString = replacedString.Replace(item.Key, item.Value);
                }
            }
            if (UseCustomVariables && checkCustomVariables)
            {
                foreach (var item in customVariables.Where(x => x.Value == replaceDataType.DiscordUser))
                {
                    if (replacedString.Contains(item.Key))
                    {
                        var replaceData = new replaceData()
                        {
                            DiscordUser = user,
                        };
                        replacedString = replacedString.Replace(item.Key, ReplaceConditions(item.Key, replaceData, replaceDataType.DiscordUser));
                    }
                }
            }
            return replacedString;
        }
        public static string ReplaceDiscordChannelVariables(MessageData channel, string replacedString, bool checkCustomVariables = true)
        {
            var replacedData = new Dictionary<string, string>
            {
                { "{DiscordChannel.Name}", channel.ChannelName },
                { "{DiscordChannel.ID}", channel.ChannelID.ToString() },
                { "{DiscordChannel.Message}", channel.Text }
            };
            foreach (var item in replacedData)
            {
                if (replacedString.Contains(item.Key))
                {
                    replacedString = replacedString.Replace(item.Key, item.Value);
                }
            }
            if (UseCustomVariables && checkCustomVariables)
            {
                foreach (var item in customVariables.Where(x => x.Value == replaceDataType.DiscordChannel))
                {
                    if (replacedString.Contains(item.Key))
                    {
                        var replaceData = new replaceData()
                        {
                            DiscordChannel = channel,
                        };
                        replacedString = replacedString.Replace(item.Key, ReplaceConditions(item.Key, replaceData, replaceDataType.DiscordChannel));
                    }
                }
            }
            return replacedString;
        }

        public static int GetTeamScore(int team)
        {
            return Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager").Where(x => x.TeamNum == team).Select(x => x.Score).FirstOrDefault();
        }

        private bool IsEmbedValid(EmbedBuilder? Embed)
        {
            if (Embed == null)
                return false;

            int embedOptions = 0;
            if (!string.IsNullOrEmpty(Embed.Title))
                embedOptions++;
            if (!string.IsNullOrEmpty(Embed.Description))
                embedOptions++;
            if (Embed.Fields.Count > 0)
                embedOptions++;
            if (Embed.Footer != null)
                embedOptions++;
            if (!string.IsNullOrEmpty(Embed.ImageUrl))
                embedOptions++;
            if (!string.IsNullOrEmpty(Embed.ThumbnailUrl))
                embedOptions++;

            return embedOptions > 0;
        }

        private static string IsValidFlag(string flagEmoji)
        {
            var flagsList = new List<string>
            {
                ":flag_ac:", ":flag_ad:", ":flag_ae:", ":flag_af:", ":flag_ag:",
                ":flag_ai:", ":flag_al:", ":flag_am:", ":flag_ao:", ":flag_aq:",
                ":flag_ar:", ":flag_as:", ":flag_at:", ":flag_au:", ":flag_aw:",
                ":flag_ax:", ":flag_az:", ":flag_ba:", ":flag_bb:", ":flag_bd:",
                ":flag_be:", ":flag_bf:", ":flag_bg:", ":flag_bh:", ":flag_bi:",
                ":flag_bj:", ":flag_bl:", ":flag_bm:", ":flag_bn:", ":flag_bo:",
                ":flag_bq:", ":flag_br:", ":flag_bs:", ":flag_bt:", ":flag_bv:",
                ":flag_bw:", ":flag_by:", ":flag_bz:", ":flag_ca:", ":flag_cc:",
                ":flag_cd:", ":flag_cf:", ":flag_cg:", ":flag_ch:", ":flag_ci:",
                ":flag_ck:", ":flag_cl:", ":flag_cm:", ":flag_cn:", ":flag_co:",
                ":flag_cp:", ":flag_cr:", ":flag_cu:", ":flag_cv:", ":flag_cw:",
                ":flag_cx:", ":flag_cy:", ":flag_cz:", ":flag_de:", ":flag_dg:",
                ":flag_dj:", ":flag_dk:", ":flag_dm:", ":flag_do:", ":flag_dz:",
                ":flag_ea:", ":flag_ec:", ":flag_ee:", ":flag_eg:", ":flag_eh:",
                ":flag_er:", ":flag_es:", ":flag_et:", ":flag_eu:", ":flag_fi:",
                ":flag_fj:", ":flag_fk:", ":flag_fm:", ":flag_fo:", ":flag_fr:",
                ":flag_ga:", ":flag_gb:", ":flag_gd:", ":flag_ge:", ":flag_gf:",
                ":flag_gg:", ":flag_gh:", ":flag_gi:", ":flag_gl:", ":flag_gm:",
                ":flag_gn:", ":flag_gp:", ":flag_gq:", ":flag_gr:", ":flag_gs:",
                ":flag_gt:", ":flag_gu:", ":flag_gw:", ":flag_gy:", ":flag_hk:",
                ":flag_hm:", ":flag_hn:", ":flag_hr:", ":flag_ht:", ":flag_hu:",
                ":flag_ic:", ":flag_id:", ":flag_ie:", ":flag_il:", ":flag_im:",
                ":flag_in:", ":flag_io:", ":flag_iq:", ":flag_ir:", ":flag_is:",
                ":flag_it:", ":flag_je:", ":flag_jm:", ":flag_jo:", ":flag_jp:",
                ":flag_ke:", ":flag_kg:", ":flag_kh:", ":flag_ki:", ":flag_km:",
                ":flag_kn:", ":flag_kp:", ":flag_kr:", ":flag_kw:", ":flag_ky:",
                ":flag_kz:", ":flag_la:", ":flag_lb:", ":flag_lc:", ":flag_li:",
                ":flag_lk:", ":flag_lr:", ":flag_ls:", ":flag_lt:", ":flag_lu:",
                ":flag_lv:", ":flag_ly:", ":flag_ma:", ":flag_mc:", ":flag_md:",
                ":flag_me:", ":flag_mf:", ":flag_mg:", ":flag_mh:", ":flag_mk:",
                ":flag_ml:", ":flag_mm:", ":flag_mn:", ":flag_mo:", ":flag_mp:",
                ":flag_mq:", ":flag_mr:", ":flag_ms:", ":flag_mt:", ":flag_mu:",
                ":flag_mv:", ":flag_mw:", ":flag_mx:", ":flag_my:", ":flag_mz:",
                ":flag_na:", ":flag_nc:", ":flag_ne:", ":flag_nf:", ":flag_ng:",
                ":flag_ni:", ":flag_nl:", ":flag_no:", ":flag_np:", ":flag_nr:",
                ":flag_nu:", ":flag_nz:", ":flag_om:", ":flag_pa:", ":flag_pe:",
                ":flag_pf:", ":flag_pg:", ":flag_ph:", ":flag_pk:", ":flag_pl:",
                ":flag_pm:", ":flag_pn:", ":flag_pr:", ":flag_ps:", ":flag_pt:",
                ":flag_pw:", ":flag_py:", ":flag_qa:", ":flag_re:", ":flag_ro:",
                ":flag_rs:", ":flag_ru:", ":flag_rw:", ":flag_sa:", ":flag_sb:",
                ":flag_sc:", ":flag_sd:", ":flag_se:", ":flag_sg:", ":flag_sh:",
                ":flag_si:", ":flag_sj:", ":flag_sk:", ":flag_sl:", ":flag_sm:",
                ":flag_sn:", ":flag_so:", ":flag_sr:", ":flag_ss:", ":flag_st:",
                ":flag_sv:", ":flag_sx:", ":flag_sy:", ":flag_sz:", ":flag_ta:",
                ":flag_tc:", ":flag_td:", ":flag_tf:", ":flag_tg:", ":flag_th:",
                ":flag_tj:", ":flag_tk:", ":flag_tl:", ":flag_tm:", ":flag_tn:",
                ":flag_to:", ":flag_tr:", ":flag_tt:", ":flag_tv:", ":flag_tw:",
                ":flag_tz:", ":flag_ua:", ":flag_ug:", ":flag_um:", ":flag_us:",
                ":flag_uy:", ":flag_uz:", ":flag_va:", ":flag_vc:", ":flag_ve:",
                ":flag_vg:", ":flag_vi:", ":flag_vn:", ":flag_vu:", ":flag_wf:",
                ":flag_ws:", ":flag_xk:", ":flag_ye:", ":flag_yt:", ":flag_za:",
                ":flag_zm:", ":flag_zw:"
            };

            if (flagsList.Contains(flagEmoji))
                return flagEmoji;

            return ":flag_white:";
        }

        public static string GetTeamShortName(int team)
        {
            switch (team)
            {
                case 1:
                    return "Spec";
                case 2:
                    return "T";
                case 3:
                    return "CT";
                default:
                    return "None";
            }
        }
        public static string GetTeamLongName(int team)
        {
            switch (team)
            {
                case 1:
                    return "Spectator";
                case 2:
                    return "Terrorist";
                case 3:
                    return "Counter-Terrorist";
                default:
                    return "None";
            }
        }

        public static bool IsDefaultEmoji(string input)
        {
            return input.StartsWith(":") && input.EndsWith(":");
        }

        public bool IsValidCustomEmoji(string emoji)
        {
            if (emoji.StartsWith("<:") && emoji.EndsWith(">"))
                return true;

            Perform_SendConsoleMessage($"Invalid Emoji Format '{emoji}'! Correct format: '<:NAME:ID>'", ConsoleColor.Red);
            return false;
        }

        public static string RemoveEmoji(string input)
        {
            Console.WriteLine($"input: {input}");
            string emojiPattern = @"[\p{So}]";

            string result = Regex.Replace(input, emojiPattern, string.Empty);
            Console.WriteLine($"return: {result}");
            return result;
        }

        private int GetPlayersCount()
        {
            return Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV && !p.IsBot && p.Connected == PlayerConnectedState.PlayerConnected && p.SteamID.ToString().Length == 17).Count();
        }
        private int GetPlayersCountWithBots()
        {
            return Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected).Count();
        }
        private int GetBotsCounts()
        {
            return Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV && p.IsBot).Count();
        }
        private static CCSGameRules GameRules()
        {
            return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
        }
    }
}