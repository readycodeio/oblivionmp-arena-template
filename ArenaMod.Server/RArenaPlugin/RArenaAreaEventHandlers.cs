using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Relay.Server.Sdk.Events;

namespace ArenaMod.Server.RArenaPlugin
{
    public sealed class RArenaAreaEventHandlers(RArena rArena, ServerEventsApi events, RArenaServerRpc sRpc, ILogger logger)
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
            if (rArena == null) return;

            sRpc.SendAreaJoined(pId, aId.ToString());
            rArena.PlayersAreas[pId] = aId.ToString();

            //if (!aId.ToString().Contains("L_PersistentDungeon-1C60F"))
            //    return;

            //logger.LogInformation("{player} entered arena. Trying to register", pId);
            //if (rArena == null)
            //{
            //    sRpc.SendRegisterForArena(pId, false);
            //    return;
            //}

            //if (rArena.ArenaStatus != EArenaStatus.NotStarted)
            //{
            //    sRpc.SendRegisterForArena(pId, false);
            //    return;
            //}
            
            //if (rArena.Participants.TryAdd(pId, EArenaParticipantStatus.NotReady))
            //{
            //    logger.LogInformation("Successfully registered {player}", pId);
            //    sRpc.SendRegisterForArena(pId, true);
            //}
            //else
            //{
            //    sRpc.SendRegisterForArena(pId, false);
            //}
        }

        private void Events_OnPlayerLeftArea(PlayerId pId, AreaId aId)
        {
            if (!aId.ToString().Contains("L_PersistentDungeon-1C60F"))
                return;

            if (rArena.Participants.TryGetValue(pId, out var _))
            {
                if (rArena.ArenaStatus == EArenaStatus.NotStarted)
                {
                    logger.LogInformation("{Player} left area, removing as contestant.", pId);
                    rArena.Participants.Remove(pId);
                    rArena.ParticipantsStatusTimeouts.Remove(pId);
                }
            }
        }
    }
}
