using DiscordUtilitiesAPI.Helpers;

namespace DiscordUtilitiesAPI.Builders;
public class Commands
{
    public class SlashCommandData
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
    }
    public class SlashCommandOptionChoices
    {
        public required string Name { get; set; }
        public required string Value { get; set; }
    }
    public class SlashCommandOptionsData
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required SlashCommandOptionsType Type;
        public bool Required = false;
        public List<SlashCommandOptionChoices>? Choices { get; set; } = null;
        /*public List<SlashCommandOptionChoices> Choices
        {
            get => choices;
            set => choices = value;
        }*/
    }

    public class Builder
    {
        public required SlashCommandData commandData { get; set; }
        public required List<SlashCommandOptionsData> commandOptions { get; set; }
    }
}