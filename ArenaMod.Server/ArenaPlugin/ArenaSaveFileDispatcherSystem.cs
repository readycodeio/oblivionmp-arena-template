using Microsoft.Extensions.Logging;
using ReadyM.Relay.Server.Sdk.Players;
using ReadyM.SDK.Attributes;

namespace ArenaMod.Server.ArenaPlugin;

[Service]
internal sealed partial class ArenaSaveFileDispatcherSystem(PlayerApi playerApi, ILogger logger)
{
    private void Start()
    {
        logger.LogInformation("Arena plugin Init");

        logger.LogInformation("Registered handler {name}", nameof(OnPlayerConnectedHandler));
        playerApi.OnPlayerConnected += OnPlayerConnectedHandler;
        logger.LogInformation("Registered handler {name}", nameof(OnPlayerDisconnectedHandler));
        playerApi.OnPlayerDisconnected += OnPlayerDisconnectedHandler;

        ArenaSaveFileDispatcher.InitializeLoadouts();
        logger.LogInformation("Arena plugin init completed");

        if (!Directory.Exists(ArenaSaveFileDispatcher.SavesPlayersDir))
            Directory.CreateDirectory(ArenaSaveFileDispatcher.SavesPlayersDir);
    }

    private void Stop()
    {
        playerApi.OnPlayerConnected -= OnPlayerConnectedHandler;
        playerApi.OnPlayerDisconnected -= OnPlayerDisconnectedHandler;
    }

    private void OnPlayerConnectedHandler(PlayerConnectedEvent obj)
    {
        Guid rawPlayerGuid = obj.ReadyMId;
        string playerGuid = rawPlayerGuid.ToString().Replace("-", "");
        string templatePath = ArenaSaveFileDispatcher.GetRandomLoadout();

        if (!string.IsNullOrEmpty(templatePath))
        {
            string playerFile = Path.Combine(ArenaSaveFileDispatcher.SavesPlayersDir, $"player_{playerGuid}.sav");
            string shortLoadoutName = templatePath.Split('\\').Last();

            File.Copy(templatePath, playerFile, overwrite: true);
            logger.LogInformation("Prepared loadout {loadout} for player {guid}", shortLoadoutName, playerGuid);

            ArenaSaveFileDispatcher.PlayerLoadouts[rawPlayerGuid] = templatePath;

            if (ArenaSaveFileDispatcher.PlayerLoadouts.Count > 1) PrintUsers();
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


        ArenaSaveFileDispatcher.ReleaseLoadout(rawPlayerGuid);

        string playerFile = Path.Combine(ArenaSaveFileDispatcher.SavesPlayersDir, $"player_{playerGuid}.sav");
        if (File.Exists(playerFile))
        {
            File.Delete(playerFile);
            logger.LogInformation("Deleted loadout file for player {guid}", playerGuid);
        }

        PrintUsers();
    }

    private void PrintUsers()
    {
        if (ArenaSaveFileDispatcher.PlayerLoadouts.Count == 0) return;

        logger.LogInformation("Taken loadouts:");
        foreach (var kvp in ArenaSaveFileDispatcher.PlayerLoadouts)
        {
            logger.LogInformation($"\t- Player: {kvp.Key} -> Loadout: {Path.GetFileName(kvp.Value)}");
        }
    }
}