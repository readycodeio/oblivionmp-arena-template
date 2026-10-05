using ArenaMod.Common;
using Microsoft.Extensions.Logging;
using OblivionMp.Sdk;
using OblivionMp.Sdk.Common.Archetypes.Mixins;
using OblivionMpCSharpMod;
using OblivionMpCSharpMod.Values;
using ReadyM.Api.Idents;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Client.Entities;

namespace ArenaMod.Client;

// Client side of the server-RPC contracts. Implement the [ServerToClient] handlers; the
// [ClientToServer] Send methods are generated. Register in Mod.RegisterServices.
[RpcHandlersFor(typeof(ArenaRpcContracts))]
public sealed partial class RpcHandlers(IEntities entities, ArenaUtils arenaUtils, IChat chat, ILogger logger)
{
    partial void OnAreaJoined(string area)
    {
        Mod.MyArea = area;
        if (area.Contains(RArenaCommon.Data.ArenaCellId))
        {
            Mod.IsInArena = true;
            arenaUtils.DisplayArenaInfo();
        }
        else
        {
            Mod.IsInArena = false;
        }
    }

    partial void OnArenaParticipantRegistered(bool registered, int allRegistered)
    {
        logger.LogInformation("Your arena status has been changed to {status}.", registered);
        
        ArenaClient.IsRegistered = registered;
        ArenaClient.RArenaParticipantCount = (short)allRegistered;
        arenaUtils.DisplayArenaParticipantStatus();
        if (registered)
        {
            arenaUtils.DisplayArenaInfo();
        }
        else
        {
            SDK.GameMessage.HideInfoMessage();
            ArenaClient.InfoMessageDisplayedString = string.Empty;
        }
    }

    partial void OnUpdateParticipantRegistered(int allRegistered)
    {
        ArenaClient.RArenaParticipantCount = (short)allRegistered;
        arenaUtils.DisplayArenaInfo();
    }

    partial void OnArenaParticipantReady(bool ready, int allReady)
    {
        logger.LogInformation("Your ready status has been changed to {status}.", ready);
        
        ArenaClient.IsReady = ready;
        ArenaClient.RArenaParticipantReadyCount = (short)allReady;
        arenaUtils.DisplayArenaReadyStatus();
        arenaUtils.DisplayArenaInfo();
    }

    partial void OnUpdateParticipantReady(int allReady)
    {
        ArenaClient.RArenaParticipantReadyCount = (short)allReady;
        arenaUtils.DisplayArenaInfo();
    }

    partial void OnAnnounceWinner(PlayerId winner)
    {
        logger.LogError("got the winner");
        
        ArenaClient.RArenaParticipantReadyCount = 0;
        arenaUtils.DisplayWinner(winner);
    }

    partial void OnAnnounceFightStarted()
    {
        SDK.GameMessage.HideInfoMessage();
        SDK.GameMessage.ShowMessage("Fight started!", MessagePosition.Center, 5);
    }

    partial void OnPlayerEliminated(PlayerId playerId)
    {
        if (playerId.Equals(SDK.Entities.LocalPlayer?.PlayerId))
        {
            SDK.GameMessage.ShowMessage("You have been eliminated!");
        }

        if (entities.TryLookup(playerId, out Player player))
        {
            chat.ShowLocalMessage($"Player {player.Nickname.ToString()} has been eliminated!", new Color(1, 0, 0));
        }
    }

    partial void OnArenaCountdown(int timeLeft)
    {
        if (Mod.IsInArena)
        {
            SDK.GameMessage.ShowMessage($"Fight starts in {timeLeft} second(s)!", MessagePosition.Center, 2);
        }
    }
}