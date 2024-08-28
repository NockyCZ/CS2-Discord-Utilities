
using Discord.WebSocket;
using Newtonsoft.Json;

namespace DiscordUtilities
{
    public partial class DiscordUtilities
    {
        private Dictionary<string, Dictionary<string, DateTime>> GetTimedRoles()
        {
            try
            {
                string filePath = $"{ModuleDirectory}/TimedRoles.json";
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, DateTime>>>(json) ?? new Dictionary<string, Dictionary<string, DateTime>>();
                }
                return new Dictionary<string, Dictionary<string, DateTime>>();
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while loading Timed Roles: '{ex.Message}'", ConsoleColor.Red);
                throw new Exception($"An error occurred while loading Timed Roles: {ex.Message}");
            }
        }

        public void SetupAddNewTimedRole(SocketGuildUser user, SocketRole role, DateTime endTime)
        {
            try
            {
                var timedRoles = GetTimedRoles();
                var userId = user.Id.ToString();
                if (timedRoles.ContainsKey(userId))
                {
                    if (timedRoles[userId].ContainsKey(role.Id.ToString()))
                        timedRoles[userId][role.Id.ToString()] = endTime;
                    else
                        timedRoles[userId].Add(role.Id.ToString(), endTime);
                }
                else
                {
                    timedRoles.Add(userId, new Dictionary<string, DateTime>() { { role.Id.ToString(), endTime } });
                }

                string filePath = $"{ModuleDirectory}/TimedRoles.json";
                string json = JsonConvert.SerializeObject(timedRoles, Formatting.Indented);
                File.WriteAllText(filePath, json);
                Perform_SendConsoleMessage($"User '{user.DisplayName}' ({user.Id}) has been added to role '{role.Name}' ({role.Id}) (Ends: '{endTime}')", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while adding new Timed Role: '{ex.Message}'", ConsoleColor.Red);
                throw new Exception($"An error occurred while adding new Timed Role: {ex.Message}");
            }
        }

        public void UpdateTimedRolesFile(Dictionary<string, Dictionary<string, DateTime>> timedRoles)
        {
            try
            {
                string filePath = $"{ModuleDirectory}/TimedRoles.json";
                string json = JsonConvert.SerializeObject(timedRoles, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Perform_SendConsoleMessage($"An error occurred while updating Timed Roles: '{ex.Message}'", ConsoleColor.Red);
                throw new Exception($"An error occurred while updating Timed Roles: {ex.Message}");
            }
        }

        public void CheckExpiredTimedRoles()
        {
            var timedRoles = GetTimedRoles();
            bool updateFile = false;
            foreach (var x in timedRoles)
            {
                var rolesToRemove = x.Value.Where(x => x.Value < DateTime.Now).Select(x => x.Key).ToList();
                if (rolesToRemove == null || rolesToRemove.Count == 0)
                    continue;

                updateFile = true;
                RemoveRolesFromUser(ulong.Parse(x.Key), rolesToRemove);

                foreach (var role in rolesToRemove)
                {
                    if (timedRoles[x.Key].ContainsKey(role))
                        timedRoles[x.Key].Remove(role);
                }

                if (timedRoles[x.Key].Count == 0)
                    timedRoles.Remove(x.Key);
            }

            if (updateFile)
                UpdateTimedRolesFile(timedRoles);
        }
    }
}