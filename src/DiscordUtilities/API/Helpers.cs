using System.Dynamic;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Builders;
using static DiscordUtilitiesAPI.Builders.Components;

namespace DiscordUtilities;
public partial class DiscordUtilities : IDiscordUtilitiesAPI
{
    public bool IsValidEmoji(string emoji)
    {
        if (string.IsNullOrEmpty(emoji))
            return false;

        if (emoji.StartsWith("<:") && emoji.EndsWith(">"))
            return true;

        Perform_SendConsoleMessage($"[Discord Utilities] Invalid Emoji Format '{emoji}'! Correct format: <:NAME:ID>", ConsoleColor.Red);
        return false;
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
                        if (builderValue.Contains(".jpg") || builderValue.Contains(".png") || builderValue.Contains(".gif"))
                            Builder.ThumbnailUrl = ReplaceVariables(builderValue, replacedVariables);
                        break;
                    case "Image":
                        if (builderValue.Contains(".jpg") || builderValue.Contains(".png") || builderValue.Contains(".gif"))
                            Builder.ImageUrl = ReplaceVariables(builderValue, replacedVariables);
                        break;
                    case "Color":
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
                                Builder.Fields!.Add(fieldBuilder);
                            }
                            else
                            {
                                Perform_SendConsoleMessage($"[Discord Utilities] Invalid Fields Format! ({builderValue})", ConsoleColor.DarkRed);
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