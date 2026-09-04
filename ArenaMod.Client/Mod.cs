using Microsoft.Extensions.Logging;
using ArenaMod.Common;
using OblivionMp.Sdk;
using OblivionMp.Sdk.Entities.Extensions;
using OblivionMpCSharpMod;
using ReadyM.Api.DI;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Idents;
using ReadyM.Modloader.Mods;
using ReadyM.Sdk.Common;
using ReadyM.Sdk.Common.Input;

namespace ArenaMod.Client;

// The mod's entry point. The modloader instantiates the single class deriving from ModBase.
public class Mod : ModBase
{
    public override string Name => "Arena Mod";

    public static bool IsReady = false;
    public static bool IsReadyLocked = false;

    public static string MyArea = string.Empty;
    public static bool IsInArena = false;
    private RArenaServerRpc _serverRpc = null!;
    public static RArenaTimerCountdown RArenaTimerCountdown { get; private set; } = null!;
    public static DetectDeadPlayerSystem detect = null!;

    // Runs once when the mod loads, before Start. Register RPC handlers, services and components.
    protected override void RegisterServices(IDependencyContainer services)
    {
        // RPC handler classes must be registered for their generated handlers to be wired up.
        services.RegisterSingleton<RArenaServerRpc>();

        // To add replicated state, register a networked component here, then attach it to an archetype below.
        //services.Resolve<IComponentApi>().RegisterComponent<WalletComponent>();
        // Attaches networked components to archetypes while the ECS schema is built. Archetype
        // membership must match the server mod.
        RegisterArchetypes(registry =>
        {
            //registry.ModifyArchetype(SDK.Archetypes.GlobalPlayerArchetype, b => b.Add<WalletComponent>());
        });

        services.RegisterSingleton<ModSystemBase, RArenaTimerCountdown>();
        services.RegisterSingleton<ModSystemBase, DetectDeadPlayerSystem>();
        _serverRpc = services.Resolve<RArenaServerRpc>();
        RArenaTimerCountdown = services.Resolve<RArenaTimerCountdown>();
        detect = services.Resolve<DetectDeadPlayerSystem>();
    }

    // Runs once after all mods have loaded. Bind input and do one-off setup here.
    public override void Start()
    {
        SDK.Input.RegisterKeyBind(ModifierKeys.Control | ModifierKeys.Shift, Key.P, () =>
        {
            if (SDK.Sync.LocalPlayer is { } me)
            {
                detect._hasAnnounced = false;
                me.RebirthLocalPlayer();
            }
        });

#if DEBUG
        SDK.Input.RegisterKeyBind(Key.NUM_EIGHT, () =>
        {
            SDK.GameMessage.ShowMessage("Test test test", MessagePosition.Center, 4f);
        });

        SDK.Input.RegisterKeyBind(Key.NUM_TWO, () =>
        {
            if (SDK.Sync.LocalPlayer is { } me)
            {
                Logger.LogWarning(me.Location.ToString());

                float dx = me.Location.X;
                float dy = me.Location.Y;

                double distance = Math.Sqrt(dx * dx + dy * dy);
                Logger.LogError("To middle : {diff}, Current Area: {area}", distance, Mod.MyArea);
                if (distance > RArenaCommon.Data.ArenaRadius + RArenaCommon.Data.MercyGiveUpMargin)
                {
                    Logger.LogWarning("This would trigger out of bounds MercyGiveUp Function");
                }
            }
        });
        SDK.Input.RegisterKeyBind(Key.NUM_ONE, () =>
        {
            if (SDK.Sync.LocalPlayer is { } me)
            {
                Logger.LogInformation("Sending request to heal {who}", "local player");
                _serverRpc.SendDemandHeal();
            }
        });
        SDK.Input.RegisterKeyBind(Key.NUM_THREE, () =>
        {
            if (SDK.Sync.LocalPlayer is { } me)
            {
                Logger.LogInformation("Sending request to heal {who}", "all players");
                _serverRpc.SendDemandHealAllPlayers();
            }
        });
        SDK.Input.RegisterKeyBind(Key.NUM_SEVEN, () =>
        {
            if (SDK.Sync.LocalPlayer is { } me)
            {
                Logger.LogInformation("Sending request to switch position");
                _serverRpc.SendDemandPositionChange();
            }
        });
        SDK.Input.RegisterKeyBind(ModifierKeys.Control, Key.NUM_SEVEN, () =>
        {
            if (SDK.Sync.LocalPlayer is { } me)
            {
                Logger.LogInformation("Sending request to switch position with local position override");
                me.Location = RArenaCommon.Data.ArenaSpectatorStand;
                _serverRpc.SendDemandPositionChange();
            }
        });
#endif
    }
}
