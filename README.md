# CS2 Discord Utilities
 
Discord Utilities is a server API plugin for communication between the CS2 server and the Discord server. With this plugin you can install/create modules that will run through the main plugin.<br>
Designed for [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) framework

## [Documentation/Wiki](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities)
### Discord Support Server
[<img src="https://discordapp.com/api/guilds/1149315368465211493/widget.png?style=banner2">](https://discord.gg/Tzmq98gwqF)
- [Map Images API](https://nockycz.github.io/CS2-Discord-Utilities/)
  
# Main Features
• These features are already implemented in the main plugin.
- [x] [Linking System](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/main-configuration/linking-system)
- [x] [Custom Bot Status](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/main-configuration/custom-bot-status)

# Addons/Modules
• These modules are separate plugins that have their own configuration and work only with the main plugin.
- [x] [Report System (Calladmin)](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/modules/report-calladmin)
- [x] [Server Status](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/modules/server-status)
- [x] [RCON](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/modules/rcon) (Execute Commands from your Discord)
- [x] [Discord Chat Relay](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/modules/chat-relay)
- [x] [Connected Players Role](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/modules/connected-players-role)
- [x] [Event Notifications (Connect, Disconnect)](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/modules/event-notifications)
- [x] [Manage Roles and Permissions](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/modules/manage-roles-and-permissions)
- [x] ⭐ [Skin Changer](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/modules/skin-changer)
- [x] ⭐ [Player Stats](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/modules/player-stats)
- [x] ⭐ [Server Status Plus](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/modules/server-status-plus)
- [x] ⭐ [Commands Blocker](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/modules/commands-blocker)
- [x] ⭐ [Leaderboard](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/modules/leaderboard)

# Installation
1. Download the latest verison - https://github.com/NockyCZ/CS2-Discord-Utilities/releases/latest
   - `DiscordUtilities.zip` file includes only the main plugin without available modules
   - `DiscordUtilitiesWithModules.zip` file includes all available modules, if you don't want to use some modules, just remove them from `/plugins/` folder
3. Unzip into your servers `csgo/addons/counterstrikesharp/` dir
4. Restart the server
5. Configure the config file
6. After any configuration changes, you must restart the server

# Requirements
- MySQL Database
- [Created Discord Bot](https://docs.sourcefactory.eu/cs2-plugins/discord-utilities/setting-up-a-discord-bot)

# Preview Images/Videos from available modules
<details>
<summary>Link System</summary>
<img src="https://2185268345-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2FrczaiIR8LCIvnID1U1Ty%2Fuploads%2FwXCGXNZxe61TcJ76BWW9%2Flinked.png?alt=media&token=5cee06d8-1dc7-452d-a195-5b019d67bc0c"/>
</details>

<details>
<summary>Custom Bot Status</summary>
<img src="https://2185268345-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2FrczaiIR8LCIvnID1U1Ty%2Fuploads%2FE8kwVpDDcCqta3tVjlM2%2Fbot_status.png?alt=media&token=db215792-a9af-4912-b40d-ca3b559b59c1"/>
</details>

<details>
<summary>Report/Calladmin Module</summary>
<img src="https://docs.sourcefactory.eu/~gitbook/image?url=https%3A%2F%2F799349702-files.gitbook.io%2F%7E%2Ffiles%2Fv0%2Fb%2Fgitbook-x-prod.appspot.com%2Fo%2Fspaces%252FrczaiIR8LCIvnID1U1Ty%252Fuploads%252FJzRuCyaq6LCOBSSQXEey%252FReport_DiscordInfo.png%3Falt%3Dmedia%26token%3Dc97e0540-6e62-45e9-a07e-9899b95842bc&width=768&dpr=1&quality=100&sign=fd175908&sv=1"/>
</details>

<details>
<summary>Skin Changer Module</summary>
 
[![Skin Changer Video](https://img.youtube.com/vi/z4IX8gj4asA/0.jpg)](https://www.youtube.com/watch?v=z4IX8gj4asA)
</details>

<details>
<summary>Server Status Module</summary>
<img src="https://docs.sourcefactory.eu/~gitbook/image?url=https%3A%2F%2F799349702-files.gitbook.io%2F%7E%2Ffiles%2Fv0%2Fb%2Fgitbook-x-prod.appspot.com%2Fo%2Fspaces%252FrczaiIR8LCIvnID1U1Ty%252Fuploads%252F75Tj2CNGj6JnP9IIk3gY%252FServerStatusPlus.png%3Falt%3Dmedia%26token%3Dc542b07a-176b-4873-8f06-dcc6bf4f8d43&width=768&dpr=1&quality=100&sign=e24d3917&sv=1"/>
</details>
<details>
<summary>Chat Relay Module</summary>
<img src="https://2185268345-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2FrczaiIR8LCIvnID1U1Ty%2Fuploads%2Fsofp76XECCqImToUlpPl%2Fchatlog.png?alt=media&token=42ab5c4d-d38b-4fcc-85f9-e7c3b3b9d0b8"/>
<img src="https://2185268345-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2FrczaiIR8LCIvnID1U1Ty%2Fuploads%2FOy5itmmNeM51qvkt1Y52%2Frelay.png?alt=media&token=1ab194e0-2fc8-4b5e-a514-0fa024bd2e8a"/>
</details>
<details>
<summary>Event Notifications Module</summary>
<img src="https://2185268345-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2FrczaiIR8LCIvnID1U1Ty%2Fuploads%2FsUBC7Wwa1D9oBHp0vAgi%2Fevent_notf.png?alt=media&token=3dee848f-f330-44a2-b5f1-2aeddfaac409"/>
</details>
