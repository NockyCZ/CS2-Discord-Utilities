namespace DiscordUtilitiesAPI.Builders;

public interface Components
{
    public enum ButtonColor
    {
        Blurple = 1,
        Grey = 2,
        Green = 3,
        Red = 4,
    }
    public class InteractiveButtonsBuilder
    {
        public required string CustomId { get; set; }
        public required string Label { get; set; }
        public string Emoji { get; set; } = "";
        public required ButtonColor Color { get; set; }
    }

    public class LinkButtonsBuilder
    {
        public required string Label { get; set; }
        public required string URL { get; set; }
        public string Emoji { get; set; } = "";
    }

    public class InteractiveMenusOptions
    {
        public required string Label { get; set; }
        public required string Value { get; set; }
        public string Description { get; set; } = "";
        public string Emoji { get; set; } = "";
    }
    public class InteractiveMenusBuilder
    {
        public required string CustomId { get; set; }
        public required string Placeholder { get; set; }
        public required int MinValues { get; set; }
        public required int MaxValues { get; set; }
        public required List<InteractiveMenusOptions> Options { get; set; }
    }

    public class Builder
    {
        public List<InteractiveButtonsBuilder>? InteractiveButtons { get; set; }
        public List<LinkButtonsBuilder>? LinkButtons { get; set; }
        public List<InteractiveMenusBuilder>? InteractiveMenus { get; set; }
    }
}

