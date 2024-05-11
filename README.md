# CS2 Discord Utilities
 
Discord Utilities is a server API plugin for communication between the CS2 server and the Discord server. With this plugin you can install/create modules that will run through the main plugin.<br>
Designed for [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) framework | Minimum CSS API Version: 202

## [Documentation/Wiki](https://docs.sourcefactory.eu/cs2-free-plugins/discord-utilities)
## [Discord Support Server](https://discord.gg/Tzmq98gwqF)

# Main Features
• These features are already implemented in the main plugin.
- [x] [Linking System](https://docs.sourcefactory.eu/cs2-free-plugins/discord-utilities/configuration/linking-system)
- [x] [Custom Bot Status](https://docs.sourcefactory.eu/cs2-free-plugins/discord-utilities/configuration/custom-bot-status)

# Addons/Modules
• These modules are separate plugins that have their own configuration and work only with the main plugin.
- [x] [Report System (Calladmin)](https://docs.sourcefactory.eu/cs2-free-plugins/discord-utilities/configuration/report-calladmin)
- [x] [Server Status](https://docs.sourcefactory.eu/cs2-free-plugins/discord-utilities/configuration/server-status)
- [x] [Server Chatlog](https://docs.sourcefactory.eu/cs2-free-plugins/discord-utilities/configuration/server-chatlog)
- [x] [Discord Chat Relay](https://docs.sourcefactory.eu/cs2-free-plugins/discord-utilities/configuration/discord-chat-relay)
- [x] [Connected Players Role](https://docs.sourcefactory.eu/cs2-free-plugins/discord-utilities/configuration/connected-players-role)
- [x] [Event Notifications (Connect, Disconnect)](https://docs.sourcefactory.eu/cs2-free-plugins/discord-utilities/configuration/event-notifications)
- [x] [Manage Roles and Permissions](https://docs.sourcefactory.eu/cs2-free-plugins/discord-utilities/configuration/manage-roles-and-permissions)

# Installation
1. Download the latest verison - https://github.com/NockyCZ/CS2-Discord-Utilities/releases/latest
   - `DiscordUtilities.zip` file includes only the main plugin without available modules
   - `DiscordUtilitiesWithModules.zip` file includes all available modules, if you don't want to use some modules, just remove them from `/plugins/` folder
3. Unzip into your servers `csgo/addons/counterstrikesharp/plugins/` dir
4. Restart the server
5. Configure the config file
6. After any configuration changes, you must restart the server

# Requirements
- MySQL Database (Only if you want to use the Link System)
- [Created Discord Bot](https://docs.sourcefactory.eu/cs2-free-plugins/discord-utilities/setting-up-a-discord-bot)

# Preview Images from available modules
**• Link System** <br>
<img src="https://2185268345-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2FrczaiIR8LCIvnID1U1Ty%2Fuploads%2FwXCGXNZxe61TcJ76BWW9%2Flinked.png?alt=media&token=5cee06d8-1dc7-452d-a195-5b019d67bc0c"/> <br>
**• Custom Bot Status** <br>
<img src="https://2185268345-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2FrczaiIR8LCIvnID1U1Ty%2Fuploads%2FE8kwVpDDcCqta3tVjlM2%2Fbot_status.png?alt=media&token=db215792-a9af-4912-b40d-ca3b559b59c1"/> <br>
**• Report Module** <br>
<img src="https://files.gitbook.com/v0/b/gitbook-x-prod.appspot.com/o/spaces%2FrczaiIR8LCIvnID1U1Ty%2Fuploads%2Frs58xMZ27NkcszdOoySc%2Freport.png?alt=media&token=db91e686-514d-44ff-9cf6-6329200703f2"/> <br>
**• Server Status Module** <br>
<img src="https://2185268345-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2FrczaiIR8LCIvnID1U1Ty%2Fuploads%2F7TUg9qjJ0bQ4PjBvl3Zh%2Fserverstatus.png?alt=media&token=b71a92ff-e1ba-48eb-82b1-f14912197cc7"/><br>
**• Chat Relay Module** <br>
<img src="https://2185268345-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2FrczaiIR8LCIvnID1U1Ty%2Fuploads%2Fsofp76XECCqImToUlpPl%2Fchatlog.png?alt=media&token=42ab5c4d-d38b-4fcc-85f9-e7c3b3b9d0b8"/> <br>
<img src="https://2185268345-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2FrczaiIR8LCIvnID1U1Ty%2Fuploads%2FOy5itmmNeM51qvkt1Y52%2Frelay.png?alt=media&token=1ab194e0-2fc8-4b5e-a514-0fa024bd2e8a"/> <br>
**• Event Notifications Module** <br>
<img src="https://2185268345-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2FrczaiIR8LCIvnID1U1Ty%2Fuploads%2FsUBC7Wwa1D9oBHp0vAgi%2Fevent_notf.png?alt=media&token=3dee848f-f330-44a2-b5f1-2aeddfaac409"/> <br>
