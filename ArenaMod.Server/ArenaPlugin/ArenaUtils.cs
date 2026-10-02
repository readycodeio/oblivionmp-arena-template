using System.Numerics;
using ArenaMod.Common;
using ArenaMod.Server.ArenaPlugin.Enums;
using ReadyM.Relay.Common.Oblivion.ECS.Components;
using ReadyM.Relay.Server.Sdk.Ecs;

namespace ArenaMod.Server.ArenaPlugin;

public static class ArenaUtils
{
    public static Arena Arena;
    public static EcsApi EcsApi;
    public static RArenaServerRpc SRpc;

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

    public static void Teleport(int id, ref TransformComponent t, Vector3 dest)
    {
        t.Position = dest;
        t.PositionNotifyChanged(id);
    }

    public static void BroadcastRegistrationUpdate()
    {
        int allRegistered = Arena.Participants.Count;

        EcsApi.Query<MainCharacterComponent>((ref main) =>
        {
            if (Arena.PlayersAreas.TryGetValue(main.PlayerId, out var area) && area.Contains(RArenaCommon.Data.ArenaCellId))
            {
                SRpc?.SendUpdateParticipantRegistered(main.PlayerId, allRegistered);
            }
        });
    }

    public static void BroadcastReadyUpdate()
    {
        int allReady = Arena.Participants.Count(p => p.Value == ArenaParticipantStatus.Ready);

        EcsApi.Query<MainCharacterComponent>((ref main) =>
        {
            if (Arena.PlayersAreas.TryGetValue(main.PlayerId, out var area) && area.Contains(RArenaCommon.Data.ArenaCellId))
            {
                SRpc?.SendUpdateParticipantReady(main.PlayerId, allReady);
            }
        });
    }
}