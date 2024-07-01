namespace DiscordUtilitiesAPI.Builders;

public interface Modal
{
    public enum TextInputStyle
    {
        Short = 1,
        Paragraph = 2
    }
    public class ModalInputsBuilder
    {
        public required string Label { get; set; }
        public required string CustomId { get; set; }
        public TextInputStyle InputStyle { get; set; } = TextInputStyle.Short;
        public string Placeholder { get; set; } = "";
        public string Value { get; set; } = "";
        public int MinLength { get; set; } = 0;
        public int MaxLength { get; set; } = 5000;
        public bool Required { get; set; } = false;
    }

    public class Builder
    {
        public required string CustomId { get; set; }
        public required string Title { get; set; }
        public required List<ModalInputsBuilder> ModalInputs { get; set; }
    }
}