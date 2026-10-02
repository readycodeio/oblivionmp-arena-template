using ArenaMod.Server.ArenaPlugin;
using ArenaMod.Server.ArenaPlugin.Enums;
using ReadyM.Api.Idents;

namespace ArenaMod.Server.Conditions;

public class ArenaLmsCondition(Arena arena) : ArenaCondition
{
    private int _playersRemaining;
    private bool _hasFinished;
    private bool _hasInit;
    private PlayerId _winner = PlayerId.Invalid;

    public ArenaLmsCondition(Arena arena, int priority) : this(arena)
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
            _winner = arena.Participants.FirstOrDefault(b => b.Value == ArenaParticipantStatus.Fighting).Key;
            arena.Participants[_winner] = ArenaParticipantStatus.Win;
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
        _winner = PlayerId.Invalid;
    }

    public PlayerId Winner => _winner;
}