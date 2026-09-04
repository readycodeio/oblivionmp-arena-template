namespace ArenaMod.Server.RArenaPlugin
{
    public enum EArenaMode //TODO
    {
        ArenaNone,
        ArenaFFA,
        ArenaTournament
    }

    public enum EArenaStatus
    {
        NotStarted,
        Ongoing,
        Finished,
    }

    public enum EArenaWinCondition //TODO
    {
        LastManStanding,
        KillCount,
        BestOfN
    }

    public enum EArenaLossCondition //TODO
    {
        PlayerDead,
        PlayerHpBelowLimit,
        PlayerMercy,
        PlayerDisconnect
    }


    public enum EArenaTournamentMode //TODO
    {
        Tournament2v2,
        Tournament3v3,
        Tournament5v5,
        TournamentBrackets
    }

    public enum EArenaParticipantStatus //TODO
    {
        NotReady,
        Ready,
        Fighting,
        Win,
        Loss,
    }
}
