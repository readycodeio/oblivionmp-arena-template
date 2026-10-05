using ArenaMod.Server.ArenaPlugin;
using ArenaMod.Server.ArenaPlugin.Enums;
using ReadyM.Api.Idents;

namespace ArenaMod.Server.Conditions;

public class LastManStandingCondition(Arena arena) : ArenaCondition
{
    private int _playersRemaining;
    private bool _hasFinished;
    private bool _hasInit;

    public PlayerId Winner { get; private set; } = PlayerId.Invalid;

    public LastManStandingCondition(Arena arena, int priority) : this(arena)
    {
        Priority = priority;
    }

    public override void OnUpdate()
    {
        if (!_hasInit)
        {
            _hasInit = true;
            _playersRemaining = arena.Participants
                .Count(p => p.Value == ArenaParticipantStatus.Fighting);
        }

        if (_playersRemaining <= 0) return;

        if (_hasFinished) return;

        _playersRemaining = arena.Participants
            .Count(p => p.Value == ArenaParticipantStatus.Fighting);

        if (_playersRemaining < 2)
        {
            _hasFinished = true;
            Winner = arena.Participants.FirstOrDefault(b => b.Value == ArenaParticipantStatus.Fighting).Key;
            arena.Participants[Winner] = ArenaParticipantStatus.Win;
        }
    }

    public override bool WasFullfilled()
    {
        return _hasFinished;
    }

    public override void Reset()
    {
        _playersRemaining = -1;
        _hasFinished = false;
        _hasInit = false;
        Winner = PlayerId.Invalid;
    }
}