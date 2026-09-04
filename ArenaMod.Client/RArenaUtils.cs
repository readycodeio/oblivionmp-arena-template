using OblivionMp.Sdk;
using OblivionMpCSharpMod;
using ReadyM.Api.Idents;

namespace ArenaMod.Client
{
    internal static class RArenaUtils
    {
        public static void DisplayWinner(PlayerId playerId)
        {
            if (!Mod.IsInArena) return;
            var nickname = SDK.Sync.AllPlayers.First(p => p.PlayerId == playerId).Nickname;
            SDK.GameMessage.ShowMessage(RArenaClient.WinnerString.Replace("{arg}", nickname), 
                                        MessagePosition.Center, 4f);
        }

        public static void DisplayArenaParticipantStatus()
        {
            if (!Mod.IsInArena) return;
            if (!RArenaClient.IsRegistered) { SDK.GameMessage.HideInfoMessage(); }
            SDK.GameMessage.ShowMessage(RArenaClient.ParticipantRegisteredString.Replace("{arg}", RArenaClient.IsRegistered ? "added" : "removed"),
                                        MessagePosition.TopLeft, 3f);
        }
        public static void DisplayArenaReadyStatus()
        {
            if (!Mod.IsInArena) return;
            SDK.GameMessage.ShowMessage(RArenaClient.ParticipantReadyString.Replace("{arg}", RArenaClient.IsReady ? "ready" : "not ready"), 
                                        MessagePosition.TopLeft, 3f);
        }
        public static void DisplayArenaInfo()
        {
            if (!Mod.IsInArena || !RArenaClient.IsRegistered) return;
            if (RArenaClient.RArenaParticipantCount < 2)
            {
                SDK.GameMessage.ShowInfoMessage("At least two players must be present!");
                return;
            }

            string message = RArenaClient.ArenaParticipantInfoMessageString
                .Replace("{ready}", RArenaClient.RArenaParticipantReadyCount.ToString())
                .Replace("{total}", RArenaClient.RArenaParticipantCount.ToString())
                .Replace("{percent}", Math.Round(((float)RArenaClient.RArenaParticipantReadyCount / (float)RArenaClient.RArenaParticipantCount) * 100).ToString())
                .Replace("{status}", RArenaClient.IsReady ? "You are ready" : "Enter blood ring to start")
                .Replace("{majorityTimer}", $"{(Mod.RArenaTimerCountdown._running && RArenaClient.RArenaParticipantReadyCount >= Math.Max(1, RArenaClient.RArenaParticipantCount / 2)
                ? RArenaClient.ArenaMajorityCountdownString.Replace("{arg}", Mod.RArenaTimerCountdown.GetRemainingTime().ToString("0.00"))
                : string.Empty
                )}");

            if (message != RArenaClient.InfoMessageDisplayedString)
            {
                RArenaClient.InfoMessageDisplayedString = message;
                SDK.GameMessage.ShowInfoMessage(message);
            }
        }
        public static string[] CenterDisplayText(string[] strings)
        {
            if (strings == null || strings.Length == 0) return strings;

            int longest = strings.Max(s => s.Length);

            return strings.Select(s =>
            {
                if (s.Length == longest) return s;

                int diff = longest - s.Length;

                int wideSpaceCount = (diff / 4) - 1;
                wideSpaceCount = Math.Max(0, wideSpaceCount);

                int dynamicNormalSpaces = (diff % 4);
                int normalSpaceCount = dynamicNormalSpaces + 1;

                string padding = new string(' ', normalSpaceCount);
                return new string('\u3000', wideSpaceCount) + padding + s;
            }).ToArray();
        }
    }
}
