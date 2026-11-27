using System.Dynamic;
using Discord;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Builders;
using DiscordUtilitiesAPI.Helpers;
using static DiscordUtilitiesAPI.Builders.Components;

namespace DiscordUtilities;
public partial class DiscordUtilities : IDiscordUtilitiesAPI
{
    public void CheckVersion(string moduleName, string moduleVersion)
    {
        if (string.IsNullOrEmpty(moduleName) || string.IsNullOrEmpty(moduleVersion))
            return;

        // Modules only register here, all outdated versions are reported together in 'ReportOutdatedVersions'
        installedModules[moduleName] = moduleVersion;

        // A module that was loaded after the report (e.g. reloaded) is reported on its own
        if (versionsReported && moduleVersions.TryGetValue(moduleName, out var latestVersion) && IsVersionOutdated(moduleVersion, latestVersion))
            Perform_SendConsoleMessage($"Module '{moduleName.Replace(ModulePrefix, "")}' is outdated! ({moduleVersion} -> {latestVersion})", ConsoleColor.DarkRed);
    }

    private const string ModulePrefix = "[Discord Utilities] ";
    private const string DownloadUrl = "https://github.com/NockyCZ/CS2-Discord-Utilities/releases";
    private static volatile bool versionsReported;

    // Only an older version is outdated, a newer one than the published list is not
    private static bool IsVersionOutdated(string installedVersion, string latestVersion)
    {
        installedVersion = installedVersion.Trim();
        latestVersion = latestVersion.Trim();

        if (Version.TryParse(installedVersion, out var installed) && Version.TryParse(latestVersion, out var latest))
            return installed < latest;

        return !installedVersion.Equals(latestVersion);
    }

    private void ReportOutdatedVersions()
    {
        var latestVersions = moduleVersions;

        string? outdatedMain = null;
        if (latestVersions.TryGetValue("Main", out var latestMain) && IsVersionOutdated(ModuleVersion, latestMain))
            outdatedMain = latestMain;

        var outdatedModules = new List<string>();
        foreach (var module in installedModules.OrderBy(m => m.Key))
        {
            if (latestVersions.TryGetValue(module.Key, out var latestVersion) && IsVersionOutdated(module.Value, latestVersion))
                outdatedModules.Add($"'{module.Key.Replace(ModulePrefix, "")}' ({module.Value} -> {latestVersion})");
        }

        versionsReported = true;

        var loadedModules = installedModules.OrderBy(m => m.Key).Select(m => $"'{m.Key.Replace(ModulePrefix, "")}' ({m.Value})").ToList();
        if (loadedModules.Count > 0)
            Perform_SendConsoleMessage($"Loaded modules ({loadedModules.Count}): {string.Join(", ", loadedModules)}", ConsoleColor.Green);

        if (outdatedMain == null && outdatedModules.Count == 0)
        {
            Perform_SendConsoleMessage("The plugin and all modules are up to date", ConsoleColor.Green);
            return;
        }

        // One block for the plugin and all modules
        Console.WriteLine("====================================================================================");
        Console.WriteLine("");
        if (outdatedMain != null)
            Perform_SendConsoleMessage($"Plugin is outdated! (Installed Version: {ModuleVersion}, Latest Version: {outdatedMain})", ConsoleColor.DarkRed);
        if (outdatedModules.Count > 0)
        {
            Perform_SendConsoleMessage(outdatedModules.Count == 1 ? "Module is outdated!" : $"Modules are outdated! ({outdatedModules.Count})", ConsoleColor.DarkRed);
            foreach (var line in outdatedModules)
                Perform_SendConsoleMessage($" - {line}", ConsoleColor.DarkRed);
        }
        Perform_SendConsoleMessage($"Check: {DownloadUrl}", ConsoleColor.DarkRed);
        Console.WriteLine("");
        Console.WriteLine("====================================================================================");
    }

    public bool IsCustomMessageSaved(ulong messageId)
    {
        return savedMessages.ContainsKey(messageId);
    }

    public MessageData? GetMessageDataFromCustomMessage(ulong messageId)
    {
        if (!savedMessages.ContainsKey(messageId))
            return null;

        var message = savedMessages[messageId];
        if (message == null)
        {
            Perform_SendConsoleMessage($"Message with ID '{messageId}' was not found! (GetMessageData)", ConsoleColor.Red);
            return null;
        }

        var messageData = new MessageData
        {
            ChannelName = message.Channel.Name,
            ChannelID = message.Channel.Id,
            MessageID = message.Id,
            Text = message.Content,
            GuildId = null,
            Builders = GetMessageBuilders(message)
        };
        return messageData;
    }

    public object ConvertConfigEmbedToObject<T>(T obj)
    {
        var properties = typeof(T).GetProperties();
        var embedObject = new ExpandoObject() as IDictionary<string, object>;
        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj);
            embedObject.Add(prop.Name, value!);
        }
        return embedObject;
    }

    public Embeds.Builder GetEmbedBuilderFromConfig<T>(T obj, ReplaceVariables.Builder? replacedVariables = null)
    {
        var embedObject = ConvertConfigEmbedToObject(obj);
        var Builder = new Embeds.Builder();

        if (embedObject is not IDictionary<string, object> embedDictionary)
            return Builder;

        foreach (var prop in embedDictionary)
        {
            var value = prop.Value;
            if (value != null && value.GetType() == typeof(bool))
            {
                if (prop.Key == "FooterTimestamp")
                    Builder.FooterTimestamp = (bool)value;
            }
            if (value != null && value.GetType() == typeof(string))
            {
                var builderValue = (string)value;
                if (string.IsNullOrEmpty(builderValue))
                    continue;

                switch (prop.Key)
                {
                    case "Title":
                        Builder.Title = ReplaceVariables(builderValue, replacedVariables);
                        break;
                    case "Description":
                        Builder.Description = ReplaceVariables(builderValue, replacedVariables);
                        break;
                    case "Thumbnail":
                        if (builderValue.Contains("{Server.MapImageUrl}") || builderValue.Contains(".jpg") || builderValue.Contains(".png") || builderValue.Contains(".gif"))
                            Builder.ThumbnailUrl = ReplaceVariables(builderValue, replacedVariables);
                        break;
                    case "Image":
                        if (builderValue.Contains("{Server.MapImageUrl}") || builderValue.Contains(".jpg") || builderValue.Contains(".png") || builderValue.Contains(".gif"))
                            Builder.ImageUrl = ReplaceVariables(builderValue, replacedVariables);
                        break;
                    case "Color":
                        Builder.Color = builderValue;
                        break;
                    case "HEXColor":
                        Builder.Color = builderValue;
                        break;
                    case "Footer":
                        Builder.Footer = ReplaceVariables(builderValue, replacedVariables);
                        break;
                    case "Fields":
                        string[] fields = builderValue.Split('|');
                        foreach (var field in fields)
                        {
                            string[] fieldData = field.Split(';');
                            if (fieldData.Length == 3)
                            {
                                var fieldBuilder = new Embeds.FieldsData
                                {
                                    Title = ReplaceVariables(fieldData[0], replacedVariables),
                                    Description = ReplaceVariables(fieldData[1], replacedVariables),
                                    Inline = bool.Parse(fieldData[2])
                                };
                                Builder.Fields.Add(fieldBuilder);
                            }
                            else
                            {
                                Perform_SendConsoleMessage($"Invalid Fields Format! ('{builderValue}')", ConsoleColor.Red);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        return Builder;
    }

    public string ReplaceVariables(string input, ReplaceVariables.Builder? replacedVariables)
    {
        if (replacedVariables == null)
            return input;

        var player = replacedVariables.PlayerData;
        if (player != null && player.IsValid)
            input = ReplacePlayerDataVariables(input, player);

        var target = replacedVariables.TargetData;
        if (target != null && target.IsValid)
            input = ReplacePlayerDataVariables(input, target, true);

        var serverData = replacedVariables.ServerData;
        if (serverData != null)
            input = ReplaceServerDataVariables(input);

        var channelData = replacedVariables.DiscordChannel;
        if (channelData != null)
            input = ReplaceDiscordChannelVariables(channelData, input);

        var userData = replacedVariables.DiscordUser;
        if (userData != null)
            input = ReplaceDiscordUserVariables(userData, input);

        var variables = replacedVariables.CustomVariables;
        if (variables != null && variables.Count() > 0)
        {
            foreach (var (key, value) in variables)
            {
                input = input.Replace(key, value);
            }
        }
        return input;
    }

    public bool IsBotLoaded()
    {
        return IsBotConnected;
    }
    public bool IsDatabaseLoaded()
    {
        return IsDbConnected;
    }
    public bool Debug()
    {
        return IsDebug;
    }
}