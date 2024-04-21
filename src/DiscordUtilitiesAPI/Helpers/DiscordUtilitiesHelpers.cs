using DiscordUtilitiesAPI.Builders;

namespace DiscordUtilitiesAPI.Helpers;

public enum MessageType
{
    Error = 12,
    Success = 10,
    Failed = 6,
    Debug = 11,
    Other = 13,
}

public enum SlashCommandOptionsType
{
    SubCommand = 1,
    SubCommandGroup = 2,
    String = 3,
    Integer = 4,
    Boolean = 5,
    User = 6,
    Channel = 7,
    Role = 8,
    Mentionable = 9,
    Number = 10,
    Attachment = 11
}
public class CommandOptionsData
{
    public required string Name { get; set; }
    public required string Value { get; set; }
    public required SlashCommandOptionsType Type { get; set; }
}
public class CommandData
{
    public required ulong GuildId { get; set; }
    public required int InteractionId { get; set; }
    public required string CommandName { get; set; }
    public required List<CommandOptionsData> OptionsData { get; set; }
}

public class UserData
{
    public required string GlobalName { get; set; }
    public required string DisplayName { get; set; }
    public required ulong ID { get; set; }
    public required List<ulong> RolesIds { get; set; }
}

public class MessageData
{
    public required string ChannelName { get; set; }
    public required ulong ChannelID { get; set; }
    public required ulong MessageID { get; set; }
    public required string Text { get; set; }
    public required ulong GuildId { get; set; }
    public required MessageBuilders? Builders { get; set; }
}

public class InteractionData
{
    public required string ChannelName { get; set; }
    public required ulong ChannelID { get; set; }
    public required ulong GuildId { get; set; }
    public required string CustomId { get; set; }
    public required int InteractionId { get; set; }
    public required IReadOnlyCollection<string> SelectedValues { get; set; }
    public required MessageBuilders? Builders { get; set; }
}

public class MessageBuilders
{
    public string? Content { get; set; }
    public List<Embeds.Builder>? Embeds { get; set; }
    public List<Components.Builder>? Componenets { get; set; }
}