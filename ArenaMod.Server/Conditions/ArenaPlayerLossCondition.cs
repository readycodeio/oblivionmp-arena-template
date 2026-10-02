using ArenaMod.Server.ArenaPlugin;
using ArenaMod.Server.ArenaPlugin.Enums;
using ReadyM.Relay.Common.Oblivion.ECS.Components;
using ReadyM.Relay.Server.Sdk.Ecs;

namespace ArenaMod.Server.Conditions;

public class ArenaPlayerLossCondition(Arena arena, EcsApi ecsApi, RArenaServerRpc sRpc) : ArenaCondition
{
    private readonly float _playerHpRateLossLimit = 0.3f;
    private readonly float _playerHpLossLimit = 1;
    private readonly ArenaLossCondition _arenaHpLossCondition;

    public ArenaPlayerLossCondition(Arena arena, EcsApi ecsApi, RArenaServerRpc sRpc,
        int priority,
        float targetHpPercentage = 0.3f, float targetHpLimit = 1,
        ArenaLossCondition arenaHpLossCondition = ArenaLossCondition.PlayerDead)
        : this(arena, ecsApi, sRpc)
    {
        Priority = priority;
        _playerHpRateLossLimit = targetHpPercentage;
        _playerHpLossLimit = targetHpLimit;
        _arenaHpLossCondition = arenaHpLossCondition;
    }

    public override void OnUpdate()
    {
        ecsApi.QueryWithEntity<MainCharacterComponent, VitalsComponent>((ref main, ref vital, id) =>
        {
            if (arena.Participants.TryGetValue(main.PlayerId, out var status) && status == ArenaParticipantStatus.Fighting)
            {
                bool playerLost = false;

                float maxHp = vital.HpBase > 0 ? vital.HpBase : 1;
                float currentHpPercentage = vital.Hp / maxHp;

                switch (_arenaHpLossCondition)
                {
                    case ArenaLossCondition.PlayerDead:
                        if (vital.Hp <= _playerHpLossLimit) playerLost = true;
                        break;

                    case ArenaLossCondition.PlayerHpBelowLimit:
                        if (currentHpPercentage <= _playerHpRateLossLimit || vital.Hp <= _playerHpLossLimit) playerLost = true;
                        break;
                }

                if (playerLost)
                {
                    arena.Participants[main.PlayerId] = ArenaParticipantStatus.Loss;
                    foreach (var p in arena.Participants.Keys)
                    {
                        sRpc.SendPlayerEliminated(p, main.PlayerId);
                    }
                    ArenaUtils.Heal(id, ref vital);
                }
            }
        });
    }

    public override bool WasFullfilled()
    {
        return false;
    }

    public override void Reset()
    {

    }
}