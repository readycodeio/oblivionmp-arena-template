namespace ArenaMod.Client
{
    internal static class RArenaClient
    {
        public static bool IsRegistered = false;
        public static bool IsReady = false;
        public static short RArenaParticipantCount = 0;
        public static short RArenaParticipantReadyCount = 0;
        public static readonly string WinnerString = "{arg} has won the match!";
        // {reg} to swap into 'unregistered' or 'registered'
        public static readonly string ParticipantRegisteredString = "You have been {arg} as an arena contender.";
        // {ready} to swap into 'not ready' or 'ready'
        public static readonly string ParticipantReadyString = "You have been marked as {arg} for arena match";
        public static readonly string ArenaMajorityCountdownString = "\nArena match will start in {arg}s!";
        public static string ArenaParticipantInfoMessageString = "Waiting for all players to be ready!" +
                                        "\n{ready} / {total}" +
                                        " ({percent}%)" +
                                        "\n{status}." +
                                        "{majorityTimer}";
        public static string InfoMessageDisplayedString = string.Empty;
    }
}
