using ArenaMod.Common;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.Oblivion.ECS.Components;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Rpc;

namespace ArenaMod.Server;

// Server side of the server-RPC contracts. Implement the [ClientToServer] handlers; the
// [ServerToClient] Send methods are generated. Each handler gets an RpcContext with the sender.
// Register in Mod.Init.
[ServerRpcFor(typeof(RpcContracts))]
public partial class RArenaServerRpc(EcsApi ecsApi) : ServerRpcHandlersBase
{
    partial void OnDemandHeal(RpcContext context)
    {
        ecsApi.Query<MainCharacterComponent, VitalsComponent>((ref main, ref vital) => 
        { 
            if (main.PlayerId == context.Sender)
            {
                vital.Hp = 9999;
            }
        });
    }

    partial void OnDemandHealAllPlayers(RpcContext context)
    {
        ecsApi.Query<VitalsComponent>((ref vital) => 
        {
            vital.Hp = 9999;
        });
    }

    partial void OnDemandPositionChange(RpcContext context)
    {
        ecsApi.QueryWithEntity<MainCharacterComponent, TransformComponent>((ref main, ref t, id) => { 
            if (main.PlayerId == context.Sender)
            {
                t.Position = RArenaCommon.Data.ArenaSpectatorStand;
                t.PositionNotifyChanged(id);
            }
        });
    }
}
