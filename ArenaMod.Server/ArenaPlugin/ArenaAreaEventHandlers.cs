using ArenaMod.Server.ArenaPlugin.Enums;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Relay.Server.Sdk.Events;
using ReadyM.SDK.Attributes;

namespace ArenaMod.Server.ArenaPlugin;

[Service]
public sealed partial class ArenaAreaEventHandlers(Arena arena, IServerEvents events, RpcHandlers rpc, ILogger logger)
{
    private void Start()
    {
        events.OnPlayerJoinedArea += OnPlayerJoinedArea;
        events.OnPlayerLeftArea += OnPlayerLeftArea;
    }

    private void Stop()
    {
        events.OnPlayerJoinedArea -= OnPlayerJoinedArea;
        events.OnPlayerLeftArea -= OnPlayerLeftArea;
    }

    private void OnPlayerJoinedArea(PlayerId pId, AreaId aId)
    {
        rpc.SendAreaJoined(pId, aId.ToString());
        arena.PlayersAreas[pId] = aId.ToString();
    }

    private void OnPlayerLeftArea(PlayerId pId, AreaId aId)
    {
        if (!aId.ToString().Contains("L_PersistentDungeon-1C60F"))
            return;

        if (arena.Participants.TryGetValue(pId, out _))
        {
            if (arena.ArenaStatus == ArenaStatus.NotStarted)
            {
                logger.LogInformation("{Player} left area, removing as contestant.", pId);
                arena.Participants.Remove(pId);
                arena.ParticipantsStatusTimeouts.Remove(pId);
            }
        }
    }
}