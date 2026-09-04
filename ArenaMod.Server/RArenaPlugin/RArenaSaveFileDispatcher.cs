using Microsoft.Extensions.Logging;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.Relay.Server.Sdk.Players;

namespace ArenaMod.Server.RArenaPlugin
{
    internal static class RArenaSaveFileDispatcher
    {
        public static ILogger? logger;

        public static readonly string SavesDir = Path.Combine(Directory.GetCurrentDirectory(), "saves");
        public static readonly string SavesPlayersDir = Path.Combine(SavesDir, "players");
        public static readonly string SavesLoadoutDir = Path.Combine(SavesDir, "arena");

        public static Dictionary<string, bool> SaveFileSlots = new();
        public static Dictionary<Guid, string> PlayerLoadouts = new();

        private static readonly Random _rand = new(System.DateTime.Now.Millisecond);

        public static void InitializeLoadouts()
        {
            if (!Directory.Exists(SavesLoadoutDir))
            {
                Directory.CreateDirectory(SavesLoadoutDir);
            }

            string[] files = Directory.GetFiles(SavesLoadoutDir);
            if (files.Length <= 0)
            {
                logger?.LogWarning("No custom save files to load, place them at {path}", SavesLoadoutDir);
                return;
            }

            foreach (string file in files)
            {
                if (!SaveFileSlots.ContainsKey(file))
                {
                    Console.WriteLine("Loaded save file: {0}", file.Split("\\").Last());
                    SaveFileSlots[file] = false;
                    
                }
            }
        }

        public static string GetRandomLoadout()
        {
            List<string> availableLoadouts = SaveFileSlots.Where(x => !x.Value).Select(x => x.Key).ToList();

            if (availableLoadouts.Count == 0)
            {
                return string.Empty;
            }

            string chosenLoadout = availableLoadouts[_rand.Next(availableLoadouts.Count)];
            SaveFileSlots[chosenLoadout] = true;

            return chosenLoadout;
        }

        public static void ReleaseLoadout(Guid playerGuid)
        {
            if (PlayerLoadouts.TryGetValue(playerGuid, out string? usedLoadout))
            {
                if (SaveFileSlots.ContainsKey(usedLoadout))
                {
                    SaveFileSlots[usedLoadout] = false;
                }
                PlayerLoadouts.Remove(playerGuid);
            }
        }
    }

    internal class RArenaSaveFileDispatcherSystem(PlayerApi playerApi, ILogger logger) : ModSystemBase
    {
        private bool _init = false;

        protected override void OnUpdate(UpdateTick tick)
        {
            if (!_init)
            {
                init();
                _init = true;
            }
        }

        private void init()
        {
            logger.LogInformation("Arena plugin Init");

            logger.LogInformation("Registered handler {name}", nameof(OnPlayerConnectedHandler));
            playerApi.OnPlayerConnected += OnPlayerConnectedHandler;
            logger.LogInformation("Registered handler {name}", nameof(OnPlayerDisconnectedHandler));
            playerApi.OnPlayerDisconnected += OnPlayerDisconnectedHandler;

            RArenaSaveFileDispatcher.InitializeLoadouts();
            logger.LogInformation("Arena plugin init completed");

            if (!Directory.Exists(RArenaSaveFileDispatcher.SavesPlayersDir)) Directory.CreateDirectory(RArenaSaveFileDispatcher.SavesPlayersDir);
        }

        private void OnPlayerConnectedHandler(PlayerConnectedEvent obj)
        {
            Guid rawPlayerGuid = obj.ReadyMId;
            string playerGuid = rawPlayerGuid.ToString().Replace("-", "");
            string templatePath = RArenaSaveFileDispatcher.GetRandomLoadout();

            if (!string.IsNullOrEmpty(templatePath))
            {
                string playerFile = Path.Combine(RArenaSaveFileDispatcher.SavesPlayersDir, $"player_{playerGuid}.sav");
                string shortLoadoutName = templatePath.Split('\\').Last();

                File.Copy(templatePath, playerFile, overwrite: true);
                logger.LogInformation("Prepared loadout {loadout} for player {guid}", shortLoadoutName, playerGuid);

                RArenaSaveFileDispatcher.PlayerLoadouts[rawPlayerGuid] = templatePath;

                if (RArenaSaveFileDispatcher.PlayerLoadouts.Count > 1) PrintUsers();
            }
            else
            {
                logger.LogWarning("All loadouts are on lease, no free one for {guid}!", playerGuid);
            }
        }

        private void OnPlayerDisconnectedHandler(PlayerDisconnectedEvent obj)
        {
            Guid rawPlayerGuid = obj.ReadyMId;
            string playerGuid = rawPlayerGuid.ToString().Replace("-", "");


            RArenaSaveFileDispatcher.ReleaseLoadout(rawPlayerGuid);

            string playerFile = Path.Combine(RArenaSaveFileDispatcher.SavesPlayersDir, $"player_{playerGuid}.sav");
            if (File.Exists(playerFile))
            {
                File.Delete(playerFile);
                logger.LogInformation("Deleted loadout file for player {guid}", playerGuid);
            }

            PrintUsers();
        }

        private void PrintUsers()
        {
            if (RArenaSaveFileDispatcher.PlayerLoadouts.Count == 0) return;

            logger.LogInformation("Taken loadouts:");
            foreach (var kvp in RArenaSaveFileDispatcher.PlayerLoadouts)
            {
                logger.LogInformation($"\t- Player: {kvp.Key} -> Loadout: {Path.GetFileName(kvp.Value)}");
            }
        }
    }
}