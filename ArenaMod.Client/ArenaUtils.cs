using OblivionMp.Sdk;
using OblivionMp.Sdk.Common.Archetypes.Mixins;
using OblivionMpCSharpMod;
using ReadyM.Api.Idents;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Client.Entities;

namespace ArenaMod.Client;

public class ArenaUtils(
    ArenaTimerCountdown timer,
    IEntities entities,
    IUserInterface ui
)
{
    public void DisplayWinner(PlayerId playerId)
    {
        if (!Mod.IsInArena)
            return;

        if (entities.TryLookup(playerId, out Player player))
        {
            ui.ShowMessage(ArenaClient.WinnerString.Replace("{arg}", player.Nickname.ToString()), MessagePosition.Center, 4f);
        }
    }

    public void DisplayArenaParticipantStatus()
    {
        if (!Mod.IsInArena)
            return;

        if (!ArenaClient.IsRegistered)
            ui.HideInfoMessage();

        ui.ShowMessage(ArenaClient.ParticipantRegisteredString.Replace("{arg}", ArenaClient.IsRegistered ? "added" : "removed"), MessagePosition.TopLeft);
    }

    public void DisplayArenaReadyStatus()
    {
        if (!Mod.IsInArena) return;
        ui.ShowMessage(ArenaClient.ParticipantReadyString.Replace("{arg}", ArenaClient.IsReady ? "ready" : "not ready"),
            MessagePosition.TopLeft);
    }

    public void DisplayArenaInfo()
    {
        if (!Mod.IsInArena || !ArenaClient.IsRegistered) return;
        if (ArenaClient.RArenaParticipantCount < 2)
        {
            ui.ShowInfoMessage("At least two players must be present!");
            return;
        }

        string message = ArenaClient.ArenaParticipantInfoMessageString
            .Replace("{ready}", ArenaClient.RArenaParticipantReadyCount.ToString())
            .Replace("{total}", ArenaClient.RArenaParticipantCount.ToString())
            .Replace("{percent}", Math.Round((float)ArenaClient.RArenaParticipantReadyCount / ArenaClient.RArenaParticipantCount * 100).ToString())
            .Replace("{status}", ArenaClient.IsReady ? "You are ready" : "Enter blood ring to start")
            .Replace("{majorityTimer}", $"{(timer.Running && ArenaClient.RArenaParticipantReadyCount >= Math.Max(1, ArenaClient.RArenaParticipantCount / 2)
                    ? ArenaClient.ArenaMajorityCountdownString.Replace("{arg}", timer.GetRemainingTime().ToString("0.00"))
                    : string.Empty
                )}");

        if (message != ArenaClient.InfoMessageDisplayedString)
        {
            ArenaClient.InfoMessageDisplayedString = message;
            ui.ShowInfoMessage(message);
        }
    }
}