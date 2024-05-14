namespace DiscordUtilitiesAPI.Builders;

public interface Embeds
{
    public class FieldsData
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public bool Inline = false;

    }
    public class Builder
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public List<FieldsData>? Fields { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string Color { get; set; } = "#ffffff";
        public string? Footer { get; set; }
        public bool FooterTimestamp = false;
    }
}
