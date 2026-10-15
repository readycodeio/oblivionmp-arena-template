using ArenaMod.Server.ArenaPlugin.Enums;
using ArenaMod.Server.Conditions;
using ReadyM.Api.Idents;

namespace ArenaMod.Server.ArenaPlugin;

public class Arena
{
    public bool PlayersImmortal = true;

    public ArenaStatus ArenaStatus = ArenaStatus.NotStarted;
    public ArenaMode ArenaMode = ArenaMode.ArenaNone;
    public readonly Dictionary<PlayerId, ArenaParticipantStatus> Participants = [];
    public readonly Dictionary<PlayerId, float> ParticipantsStatusTimeouts = [];
    public readonly Dictionary<PlayerId, string> PlayersAreas = [];
    public readonly List<ArenaCondition> ArenaWinningConditions = [];
}