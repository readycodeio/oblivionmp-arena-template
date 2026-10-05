using ArenaMod.Common;
using Microsoft.Extensions.Logging;
using OblivionMp.Sdk;
using OblivionMp.Sdk.Archetypes.Extensions;
using OblivionMpCSharpMod;
using ReadyM.Api.DI;
using ReadyM.SDK.Attributes;
using ReadyM.Sdk.Common.Input;

namespace ArenaMod.Client;

// The mod's entry point. The modloader instantiates the single class deriving from ModBase.
[ModEntry]
public sealed partial class Mod(IDependencyContainer services, ILogger logger)
{
    public static string MyArea = string.Empty;
    public static bool IsInArena = false;
    
    private RpcHandlers rpc => services.Resolve<RpcHandlers>();

    // Runs once when the mod loads, before Start. Register RPC handlers, services and components.
    private void Init()
    {
        services.RegisterSingleton<ArenaUtils>();
        services.RegisterSingleton<IChat, DummyChat>();
        
        SDK.Input.RegisterKeyBind(ModifierKeys.Control | ModifierKeys.Shift, Key.P, () =>
        {
            if (SDK.Entities.LocalMainCharacter is { } me)
            {
                services.Resolve<DetectDeadPlayerSystem>().HasAnnounced = false;
                me.RebirthLocalPlayer();
            }
        });

#if DEBUG
        SDK.Input.RegisterKeyBind(Key.NUM_EIGHT, () => { SDK.GameMessage.ShowMessage("Test test test", MessagePosition.Center, 4f); });

        SDK.Input.RegisterKeyBind(Key.NUM_TWO, () =>
        {
            if (SDK.Entities.LocalMainCharacter is { } me)
            {
                logger.LogWarning(me.Position.ToString());

                float dx = me.Position.X;
                float dy = me.Position.Y;

                double distance = Math.Sqrt(dx * dx + dy * dy);
                logger.LogError("To middle : {diff}, Current Area: {area}", distance, MyArea);
                if (distance > RArenaCommon.Data.ArenaRadius + RArenaCommon.Data.MercyGiveUpMargin)
                {
                    logger.LogWarning("This would trigger out of bounds MercyGiveUp Function");
                }
            }
        });
        SDK.Input.RegisterKeyBind(Key.NUM_ONE, () =>
        {
            if (SDK.Entities.LocalMainCharacter is { } me)
            {
                logger.LogInformation("Sending request to heal {who}", "local player");
                rpc.SendDemandHeal();
            }
        });
        SDK.Input.RegisterKeyBind(Key.NUM_THREE, () =>
        {
            if (SDK.Entities.LocalMainCharacter is { } me)
            {
                logger.LogInformation("Sending request to heal {who}", "all players");
                rpc.SendDemandHealAllPlayers();
            }
        });
        SDK.Input.RegisterKeyBind(Key.NUM_SEVEN, () =>
        {
            if (SDK.Entities.LocalMainCharacter is { } me)
            {
                logger.LogInformation("Sending request to switch position");
                rpc.SendDemandPositionChange();
            }
        });
        SDK.Input.RegisterKeyBind(ModifierKeys.Control, Key.NUM_SEVEN, () =>
        {
            if (SDK.Entities.LocalMainCharacter is { } me)
            {
                logger.LogInformation("Sending request to switch position with local position override");
                me.Position = RArenaCommon.Data.ArenaSpectatorStand;
                rpc.SendDemandPositionChange();
            }
        });
#endif
    }
}