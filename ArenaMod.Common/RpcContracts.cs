using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer;

namespace ArenaMod.Common;

// Server-RPC contracts shared by both mods. [ClientToServer] generates a Send on the client and a
// handler on the server; [ServerToClient] the reverse. Same name, opposite directions = request/response.
[ServerRpcContracts]
public static partial class RpcContracts
{
    [ServerToClient] public static partial void AnnounceWinner(PlayerId winner);
    [ServerToClient] public static partial void AnnounceWinnerTeam(byte team);
    [ServerToClient] public static partial void AnnounceFightStarted();
    [ServerToClient] public static partial void AnnounceFightEnded();
    [ServerToClient] public static partial void AreaJoined(string area);
    [ServerToClient] public static partial void AreaLeft(string area);
    [ServerToClient] public static partial void ArenaParticipantRegistered(bool registered, int allRegistered);
    [ServerToClient] public static partial void ArenaParticipantReady(bool ready, int allReady);
    [ServerToClient] public static partial void UpdateParticipantRegistered(int allRegistered);
    [ServerToClient] public static partial void UpdateParticipantReady(int allReady);
    [ServerToClient] public static partial void PlayerEliminated(PlayerId playerId);
    [ServerToClient] public static partial void ArenaCountdown(int timeLeft);
    [ClientToServer] public static partial void DemandHeal();
    [ClientToServer] public static partial void DemandHealAllPlayers();
    [ClientToServer] public static partial void DemandPositionChange();

}
