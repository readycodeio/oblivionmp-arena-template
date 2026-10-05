using ArenaMod.Server.ArenaPlugin;
using ArenaMod.Server.ArenaPlugin.Enums;
using OblivionMp.Sdk.Common.Archetypes;
using ReadyM.SDK.Server.Entities;

namespace ArenaMod.Server.Conditions;

public class PlayerLossCondition(Arena arena, IEntities entities, RpcHandlers rpc) : ArenaCondition
{
    private readonly float _playerHpRateLossLimit = 0.3f;
    private readonly float _playerHpLossLimit = 1;
    private readonly ArenaLossCondition _arenaHpLossCondition;

    public PlayerLossCondition(Arena arena, IEntities entities, RpcHandlers rpc,
        int priority,
        float targetHpPercentage = 0.3f,
        float targetHpLimit = 1,
        ArenaLossCondition arenaHpLossCondition = ArenaLossCondition.PlayerDead
    ) : this(arena, entities, rpc)
    {
        Priority = priority;
        _playerHpRateLossLimit = targetHpPercentage;
        _playerHpLossLimit = targetHpLimit;
        _arenaHpLossCondition = arenaHpLossCondition;
    }

    public override void OnUpdate()
    {
        foreach (var main in entities.Query<MainCharacter>())
        {
            if (arena.Participants.TryGetValue(main.PlayerId, out var status) && status == ArenaParticipantStatus.Fighting)
            {
                var playerLost = false;

                var maxHp = main.HpBase > 0 ? main.HpBase : 1;
                var currentHpPercentage = main.Hp / maxHp;

                switch (_arenaHpLossCondition)
                {
                    case ArenaLossCondition.PlayerDead:
                        if (main.Hp <= _playerHpLossLimit) playerLost = true;
                        break;

                    case ArenaLossCondition.PlayerHpBelowLimit:
                        if (currentHpPercentage <= _playerHpRateLossLimit || main.Hp <= _playerHpLossLimit) playerLost = true;
                        break;
                }

                if (playerLost)
                {
                    arena.Participants[main.PlayerId] = ArenaParticipantStatus.Loss;
                    foreach (var p in arena.Participants.Keys)
                    {
                        rpc.SendPlayerEliminated(p, main.PlayerId);
                    }

                    ArenaUtils.Heal(main);
                }
            }
        }
    }

    public override bool WasFullfilled() => false;

    public override void Reset() { }
}