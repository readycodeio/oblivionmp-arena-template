using System.Numerics;
using Microsoft.Extensions.Logging;
using ArenaMod.Common;
using ReadyM.Relay.Common.Oblivion.ECS.Components;
using ReadyM.Relay.Server.Sdk.Ecs;

namespace ArenaMod.Server.RArenaPlugin;

public static class RArenaUtils
{
    public static RArena rArena;
    public static EcsApi ecsApi;
    public static RArenaServerRpc sRpc;
    public static ILogger? logger;
    public static void Heal(int id, ref VitalsComponent vitals)
    {
        //logger?.LogInformation("Healing invoked");
        vitals.Hp = 10_000;
        vitals.Magicka = 1_000;
        vitals.Fatigue = 1_000;
        vitals.HpNotifyChanged(id);
        vitals.MagickaNotifyChanged(id);
        vitals.FatigueNotifyChanged(id);
    }

    public static void Teleport(int id,ref TransformComponent t, Vector3 dest)
    {
        t.Position = dest;
        t.PositionNotifyChanged(id);
    }

    public static void BroadcastRegistrationUpdate()
    {
        int allRegistered = rArena.Participants.Count;

        ecsApi.Query<MainCharacterComponent>((ref main) =>
        {
            if (rArena.PlayersAreas.TryGetValue(main.PlayerId, out var area) && area.Contains(RArenaCommon.Data.ArenaCellId))
            {
                sRpc?.SendUpdateParticipantRegistered(main.PlayerId, allRegistered);
            }
        });
    }

    public static void BroadcastReadyUpdate()
    {
        int allReady = rArena.Participants.Count(p => p.Value == EArenaParticipantStatus.Ready);

        ecsApi.Query<MainCharacterComponent>((ref main) =>
        {
            if (rArena.PlayersAreas.TryGetValue(main.PlayerId, out var area) && area.Contains(RArenaCommon.Data.ArenaCellId))
            {
                sRpc?.SendUpdateParticipantReady(main.PlayerId, allReady);
            }
        });
    }
}
