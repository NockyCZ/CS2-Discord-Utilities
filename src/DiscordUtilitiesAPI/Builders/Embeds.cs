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
        private string title = "";
        private string description = "";
        public List<FieldsData> fields { get; set; } = new List<FieldsData>();
        private string thumbnailUrl = "";
        private string imageUrl = "";
        private string color = "#ffffff";
        private string footer = "";
        private bool footerTimestamp = false;

        public string Title
        {
            get => title;
            set => title = value;
        }
        public string Description
        {
            get => description;
            set => description = value;
        }
        public List<FieldsData> Fields
        {
            get => fields!;
            set => fields = value;
        }
        public string ThumbnailUrl
        {
            get => thumbnailUrl;
            set => thumbnailUrl = value;
        }
        public string ImageUrl
        {
            get => imageUrl;
            set => imageUrl = value;
        }
        public string Color
        {
            get => color;
            set => color = value;
        }
        public string Footer
        {
            get => footer;
            set => footer = value;
        }
        public bool FooterTimestamp
        {
            get => footerTimestamp;
            set => footerTimestamp = value;
        }
    }
}
