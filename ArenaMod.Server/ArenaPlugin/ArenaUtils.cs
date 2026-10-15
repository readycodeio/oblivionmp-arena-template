using ArenaMod.Common;
using ArenaMod.Server.ArenaPlugin.Enums;
using OblivionMp.Sdk.Common.Archetypes;
using ReadyM.SDK.Server.Entities;

namespace ArenaMod.Server.ArenaPlugin;

public class ArenaUtils(IEntities entities, Arena arena, RpcHandlers rpc)
{
    public static void Heal(MainCharacter main)
    {
        main.Hp = 10_000;
        main.Magicka = 1_000;
        main.Fatigue = 1_000;
    }

    public void BroadcastRegistrationUpdate()
    {
        var allRegistered = arena.Participants.Count;

        foreach (var main in entities.Query<MainCharacter>())
        {
            if (arena.PlayersAreas.TryGetValue(main.PlayerId, out var area) && area.Contains(RArenaCommon.Data.ArenaCellId))
            {
                rpc.SendUpdateParticipantRegistered(main.PlayerId, allRegistered);
            }
        }
    }

    public void BroadcastReadyUpdate()
    {
        var allReady = arena.Participants.Count(p => p.Value == ArenaParticipantStatus.Ready);

        foreach (var main in entities.Query<MainCharacter>())
        {
            if (arena.PlayersAreas.TryGetValue(main.PlayerId, out var area) && area.Contains(RArenaCommon.Data.ArenaCellId))
            {
                rpc.SendUpdateParticipantReady(main.PlayerId, allReady);
            }
        }
    }
}