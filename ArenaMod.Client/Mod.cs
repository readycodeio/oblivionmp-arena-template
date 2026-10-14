using ArenaMod.Common;
using Microsoft.Extensions.Logging;
using OblivionMp.Sdk;
using OblivionMp.Sdk.Archetypes.Extensions;
using ReadyM.Api.DI;
using ReadyM.SDK.Attributes;
using ReadyM.Sdk.Common.Input;

namespace ArenaMod.Client;

// The mod's entry point.
[ModEntry]
public sealed partial class Mod(
    IDependencyContainer services,
    IPlayers players,
    IInput input,
    ILogger logger
)
{
    public static string MyArea = string.Empty;
    public static bool IsInArena = false;

    private RpcHandlers rpc => services.Resolve<RpcHandlers>();

    // Runs once when the mod loads, before Start. Register RPC handlers, services and components.
    private void Init()
    {
        services.RegisterSingleton<ArenaUtils>();
        services.RegisterSingleton<IChat, DummyChat>();

        input.RegisterKeyBind(ModifierKeys.Control | ModifierKeys.Shift, Key.P, () =>
        {
            if (players.LocalMainCharacter is { } me)
            {
                services.Resolve<DetectDeadPlayerSystem>().HasAnnounced = false;
                me.ResurrectLocalPlayer();
            }
        });

#if DEBUG
        input.RegisterKeyBind(Key.NUM_TWO, () =>
        {
            if (players.LocalMainCharacter is { } me)
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
        input.RegisterKeyBind(Key.NUM_ONE, () =>
        {
            if (players.LocalMainCharacter is { } me)
            {
                logger.LogInformation("Sending request to heal {who}", "local player");
                rpc.SendDemandHeal();
            }
        });
        input.RegisterKeyBind(Key.NUM_THREE, () =>
        {
            if (players.LocalMainCharacter is { } me)
            {
                logger.LogInformation("Sending request to heal {who}", "all players");
                rpc.SendDemandHealAllPlayers();
            }
        });
        input.RegisterKeyBind(Key.NUM_SEVEN, () =>
        {
            if (players.LocalMainCharacter is { } me)
            {
                logger.LogInformation("Sending request to switch position");
                rpc.SendDemandPositionChange();
            }
        });
        input.RegisterKeyBind(ModifierKeys.Control, Key.NUM_SEVEN, () =>
        {
            if (players.LocalMainCharacter is { } me)
            {
                logger.LogInformation("Sending request to switch position with local position override");
                me.Position = RArenaCommon.Data.ArenaSpectatorStand;
                rpc.SendDemandPositionChange();
            }
        });
#endif
    }
}