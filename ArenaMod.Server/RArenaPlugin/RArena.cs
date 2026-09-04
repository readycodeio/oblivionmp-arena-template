using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;

namespace ArenaMod.Server.RArenaPlugin
{
    public class RArena(ILogger logger)
    {
        public ILogger _logger = logger;

        public Tournament tournament = new(logger);

        public bool PlayersImmortal = true;

        public EArenaStatus ArenaStatus = EArenaStatus.NotStarted;
        public EArenaMode ArenaMode = EArenaMode.ArenaNone;
        public Dictionary<PlayerId, int> PlayersScore = [];
        public Dictionary<PlayerId, EArenaParticipantStatus> Participants = [];
        public Dictionary<PlayerId, float> ParticipantsStatusTimeouts = [];
        public Dictionary<PlayerId, string> PlayersAreas = [];
        public List<IRArenaCondition> ArenaWinningConditions = [];

    }

    public class Tournament(ILogger logger)
    {
        public bool ArenaTournamentConcluded { get; private set; } = false;

        public Dictionary<PlayerId, byte> ArenaTournamentTeams = [];

        public Dictionary<byte, byte> ArenaTournamentScores = [];

        public byte ArenaTournamentWinnerTeam = 255;

        private static byte _BestOfNDefault = 3;
        public byte ArenaTournamentBestOfN
        {
            get;
            set
            {
                if (value % 2 == 0)
                {
                    logger.LogWarning("Can not set BestOfN to even number, must be odd! Defaulting to {Default}", _BestOfNDefault);
                    value = _BestOfNDefault;
                }
            }
        } = _BestOfNDefault;

        public bool AddParticipantToTeam(PlayerId player, byte team)
        {
            if (ArenaTournamentTeams.ContainsKey(player))
            {
                logger.LogWarning("Player {player} already registered for team {}!", player, ArenaTournamentTeams[player]);
                return false;
            }

            if (ArenaTournamentTeams.TryAdd(player, team))
            {
                return true;
            }
            return false;
        }

        public bool RemoveParticipantFromTeam(PlayerId player, byte team)
        {
            if (ArenaTournamentTeams.TryGetValue(player, out _))
            {
                ArenaTournamentTeams.Remove(player);
                return true;
            }
            logger.LogWarning("Trying to remove not existing participant {player} from tournament!", player);
            return false;
        }

        internal void ConcludeTournament()
        {
            // Resolve best of N
            // Set winner team
            // Mark tournament concluded
        }
    }
}
