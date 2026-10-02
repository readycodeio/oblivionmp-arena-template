using ArenaMod.Common;
using OblivionMp.Sdk.Common.Archetypes;
using ReadyM.Relay.Server.Sdk.Rpc;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Server.Entities;

namespace ArenaMod.Server;

// Server side of the server-RPC contracts. Implement the [ClientToServer] handlers; the
// [ServerToClient] Send methods are generated. Each handler gets an RpcContext with the sender.
// Register in Mod.Init.
[RpcHandlersFor(typeof(ArenaRpcContracts))]
public sealed partial class RArenaServerRpc(IEntities entities)
{
    partial void OnDemandHeal(RpcContext context)
    {
        if (entities.TryLookup(context.Sender, out MainCharacter main))
        {
            main.Hp = main.HpBase; // TODO: Check, was 9999
        }
    }

    partial void OnDemandHealAllPlayers(RpcContext context)
    {
        foreach (var main in entities.Query<MainCharacter>())
        {
            main.Hp = main.HpBase; // TODO: Check, was 9999
        }
    }

    partial void OnDemandPositionChange(RpcContext context)
    {
        if (entities.TryLookup(context.Sender, out MainCharacter main))
        {
            main.Position = RArenaCommon.Data.ArenaSpectatorStand;
        }
    }
}