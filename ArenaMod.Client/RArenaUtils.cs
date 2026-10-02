using OblivionMp.Sdk;
using OblivionMpCSharpMod;
using ReadyM.Api.Idents;
using ReadyM.SDK.Archetypes.Core;

namespace ArenaMod.Client;

internal static class RArenaUtils
{
    public static void DisplayWinner(PlayerId playerId)
    {
        if (!Mod.IsInArena)
            return;

        if (SDK.Entities.TryGetPlayerById(playerId, out Player? player))
        {
            SDK.GameMessage.ShowMessage(RArenaClient.WinnerString.Replace("{arg}", player.Nickname.ToString()), MessagePosition.Center, 4f);
        }
    }

    public static void DisplayArenaParticipantStatus()
    {
        if (!Mod.IsInArena)
            return;

        if (!RArenaClient.IsRegistered)
            SDK.GameMessage.HideInfoMessage();

        SDK.GameMessage.ShowMessage(RArenaClient.ParticipantRegisteredString.Replace("{arg}", RArenaClient.IsRegistered ? "added" : "removed"), MessagePosition.TopLeft);
    }

    public static void DisplayArenaReadyStatus()
    {
        if (!Mod.IsInArena) return;
        SDK.GameMessage.ShowMessage(RArenaClient.ParticipantReadyString.Replace("{arg}", RArenaClient.IsReady ? "ready" : "not ready"),
            MessagePosition.TopLeft);
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
            .Replace("{percent}", Math.Round((float)RArenaClient.RArenaParticipantReadyCount / (float)RArenaClient.RArenaParticipantCount * 100).ToString())
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
}