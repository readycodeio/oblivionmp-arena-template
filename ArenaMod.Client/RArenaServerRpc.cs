using Microsoft.Extensions.Logging;
using ArenaMod.Common;
using OblivionMp.Sdk;
using OblivionMpCSharpMod;
using OblivionMpCSharpMod.Values;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.RPC;

namespace ArenaMod.Client;

// Client side of the server-RPC contracts. Implement the [ServerToClient] handlers; the
// [ClientToServer] Send methods are generated. Register in Mod.RegisterServices.
[ServerRpcFor(typeof(RpcContracts))]
public partial class RArenaServerRpc(ILogger logger) : ServerRpcClient
{
    partial void OnAreaJoined(string area)
    {
        Mod.MyArea = area;
        if (area.Contains(RArenaCommon.Data.ArenaCellId))
        {
            Mod.IsInArena = true;
            RArenaUtils.DisplayArenaInfo();
        }
        else
        {
            Mod.IsInArena = false;
        }
    }
    partial void OnArenaParticipantRegistered(bool registered, int allRegistered)
    {
        logger.LogInformation("Your arena status has been changed to {status}.", registered);
        RArenaClient.IsRegistered = registered;
        RArenaClient.RArenaParticipantCount = (short)allRegistered;
        RArenaUtils.DisplayArenaParticipantStatus();
        if (registered)
        {
            RArenaUtils.DisplayArenaInfo();
        }
        else
        {
            SDK.GameMessage.HideInfoMessage();
            RArenaClient.InfoMessageDisplayedString = string.Empty;
        }
    }
    partial void OnUpdateParticipantRegistered(int allRegistered)
    {
        RArenaClient.RArenaParticipantCount = (short)allRegistered;
        RArenaUtils.DisplayArenaInfo();
    }

    partial void OnArenaParticipantReady(bool ready, int allReady)
    {
        logger.LogInformation("Your ready status has been changed to {status}.", ready);
        RArenaClient.IsReady = ready;
        RArenaClient.RArenaParticipantReadyCount = (short)allReady;
        RArenaUtils.DisplayArenaReadyStatus();
        RArenaUtils.DisplayArenaInfo();
    }
    partial void OnUpdateParticipantReady(int allReady)
    {
        RArenaClient.RArenaParticipantReadyCount = (short)allReady;
        RArenaUtils.DisplayArenaInfo();
    }

    partial void OnAnnounceWinner(PlayerId winner)
    {
        logger.LogError("got the winner");
        RArenaClient.RArenaParticipantReadyCount = 0;
        RArenaUtils.DisplayWinner(winner);
    }

    partial void OnAnnounceFightStarted()
    {
        SDK.GameMessage.HideInfoMessage();
        SDK.GameMessage.ShowMessage("Fight started!", MessagePosition.Center, 5);
    }

    partial void OnPlayerEliminated(PlayerId playerId)
    {
        if (playerId.Equals(SDK.Sync.LocalPlayer?.PlayerId))
        {
            SDK.GameMessage.ShowMessage("You have been eliminated!", MessagePosition.Center, 3);
        }
        string nickname = SDK.Sync.AllPlayers.FirstOrDefault(p => p.PlayerId == playerId).Nickname;
        SDK.Chat.ShowLocalMessage($"Player {nickname} has been eliminated!", new Color(1, 0, 0));
    }

    partial void OnArenaCountdown(int timeLeft)
    {
        if (Mod.IsInArena)
        {
            SDK.GameMessage.ShowMessage($"Fight starts in {timeLeft} second(s)!", MessagePosition.Center, 2);
        }
    }
}
