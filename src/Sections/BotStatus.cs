using Discord;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public async Task UpdateBotStatus()
        {
            string ActivityFormat = ReplaceServerDataVariables(Config.BotStatus.ActivityFormat);
            await BotClient!.SetGameAsync(ActivityFormat, null, (ActivityType)Config.BotStatus.ActivityType);
            await BotClient.SetStatusAsync((UserStatus)Config.BotStatus.Status);
        }
    }
}