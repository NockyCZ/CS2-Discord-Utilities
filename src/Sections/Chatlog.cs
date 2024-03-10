using Discord;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        public void PerformChatlog(ulong playerSteamID, string message, int team, bool isTeamMessage)
        {
            var embed = new EmbedBuilder();
            var content = string.Empty;
            string[] data = new string[2];
            data[0] = playerSteamID.ToString();
            data[1] = message;
            if (isTeamMessage)
            {
                embed = GetEmbed(EmbedTypes.Team_Chatlog, data);
                content = GetContent(ContentTypes.Team_Chatlog, data);
                _ = SendDiscordMessage(embed, content, ulong.Parse(Config.Chatlog.TeamChatEmbed.ChannelID), "Team Chatlog");
            }
            else
            {
                embed = GetEmbed(EmbedTypes.All_Chatlog, data);
                content = GetContent(ContentTypes.All_Chatlog, data);
                _ = SendDiscordMessage(embed, content, ulong.Parse(Config.Chatlog.AllChatEmbed.ChannelID), "All Chatlog");
            }
        }
    }
}