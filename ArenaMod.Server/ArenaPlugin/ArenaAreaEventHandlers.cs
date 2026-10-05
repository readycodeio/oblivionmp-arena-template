using ArenaMod.Server.ArenaPlugin.Enums;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Relay.Server.Sdk.Events;

namespace ArenaMod.Server.ArenaPlugin;

public sealed class ArenaAreaEventHandlers(Arena arena, ServerEventsApi events, RpcHandlers rpc, ILogger logger)
    : ServerEventHandlersBase(events)
{
    protected override void Subscribe(ServerEventsApi events)
    {
        events.OnPlayerJoinedArea += Events_OnPlayerJoinedArea;
        events.OnPlayerLeftArea += Events_OnPlayerLeftArea;
    }

    protected override void Unsubscribe(ServerEventsApi events)
    {
        events.OnPlayerJoinedArea -= Events_OnPlayerJoinedArea;
        events.OnPlayerLeftArea -= Events_OnPlayerLeftArea;
    }

    private void Events_OnPlayerJoinedArea(PlayerId pId, AreaId aId)
    {
        rpc.SendAreaJoined(pId, aId.ToString());
        arena.PlayersAreas[pId] = aId.ToString();
    }

    private void Events_OnPlayerLeftArea(PlayerId pId, AreaId aId)
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