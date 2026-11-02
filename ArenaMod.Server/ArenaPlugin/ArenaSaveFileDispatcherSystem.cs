using Friflo.Engine.ECS;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Relay.Server.Sdk.Players;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Server.Events;

namespace ArenaMod.Server.ArenaPlugin;

[Service]
internal sealed partial class ArenaSaveFileDispatcherSystem(
    ArenaSaveFileDispatcher dispatcher,
    IServerEvents events,
    IPlayers players,
    ILogger logger
)
{
    private void Start()
    {
        logger.LogInformation("Arena plugin Init");

        logger.LogInformation("Registered handler {name}", nameof(OnPlayerConnectedHandler));
        events.OnPlayerConnected += OnPlayerConnectedHandler;
        logger.LogInformation("Registered handler {name}", nameof(OnPlayerDisconnectedHandler));
        events.OnPlayerDisconnected += OnPlayerDisconnectedHandler;

        dispatcher.InitializeLoadouts();
        logger.LogInformation("Arena plugin init completed");

        if (!Directory.Exists(dispatcher.SavesPlayersDir))
            Directory.CreateDirectory(dispatcher.SavesPlayersDir);
    }

    private void Stop()
    {
        events.OnPlayerConnected -= OnPlayerConnectedHandler;
        events.OnPlayerDisconnected -= OnPlayerDisconnectedHandler;
    }

    private void OnPlayerConnectedHandler(Player player, Guid playerGuid)
    {
        var templatePath = dispatcher.GetRandomLoadout();

        if (!string.IsNullOrEmpty(templatePath))
        {
            var playerFile = Path.Combine(dispatcher.SavesPlayersDir, $"player_{playerGuid}.sav");
            var shortLoadoutName = templatePath.Split('\\').Last();
            
            Directory.CreateDirectory(dispatcher.SavesPlayersDir);
            File.Copy(templatePath, playerFile, overwrite: true);
            logger.LogInformation("Prepared loadout {loadout} for player {guid}", shortLoadoutName, playerGuid);

            dispatcher.PlayerLoadouts[playerGuid] = templatePath;

            if (dispatcher.PlayerLoadouts.Count > 1) 
                PrintUsers();
        }
        else
        {
            logger.LogWarning("All loadouts are on lease, no free one for {guid}!", playerGuid);
        }
    }

    private void OnPlayerDisconnectedHandler(Player player, DisconnectReason __)
    {
        var playerGuidRaw = players.GetReadyMId(player.PlayerId);
        if (playerGuidRaw is not {} playerGuid)
            return;
        
        dispatcher.ReleaseLoadout(playerGuid);

        var playerFile = Path.Combine(dispatcher.SavesPlayersDir, $"player_{playerGuid}.sav");
        if (File.Exists(playerFile))
        {
            File.Delete(playerFile);
            logger.LogInformation("Deleted loadout file for player {guid}", playerGuid);
        }

        PrintUsers();
    }

    private void PrintUsers()
    {
        if (dispatcher.PlayerLoadouts.Count == 0) return;

        logger.LogInformation("Taken loadouts:");
        foreach (var kvp in dispatcher.PlayerLoadouts)
        {
            logger.LogInformation($"\t- Player: {kvp.Key} -> Loadout: {Path.GetFileName(kvp.Value)}");
        }
    }
}