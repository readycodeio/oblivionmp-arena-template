using Microsoft.Extensions.Logging;
using ArenaMod.Common;
using ArenaMod.Server.RArenaPlugin;
using OblivionMp.Sdk.Serverside;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Relay.Server.Sdk;
using ReadyM.Relay.Server.Sdk.Ecs.Components;

namespace ArenaMod.Server;

// The server mod's entry point. The plugin host instantiates the single ServerModBase-derived class.
public class Mod : ServerModBase
{
    public static RArena rArena;
    internal static RArenaSaveFileDispatcherSystem dispatcherSystem;


    protected override void Init()
    {
        //Services.RegisterSingleton<ServerRpc>();
        //Services.RegisterSingleton<ModSystemBase, PassiveActivityIncomeSystem>();

        Services.RegisterSingleton<RArena>();
        Services.RegisterSingleton<RArenaServerRpc>();
        Services.RegisterSystem<RArenaSystem>();
        Services.RegisterSystem<RArenaSaveFileDispatcherSystem>();
        Services.RegisterSingleton<RArenaAreaEventHandlers>();
        rArena = Services.Resolve<RArena>();

        var registry = Services.Resolve<IArchetypeRegistry>();
        var archetypes = Services.Resolve<OblivionArchetypes>();


        registry.ModifyArchetype(archetypes.GlobalPlayerArchetype, archetype =>
        {
            //archetype.Add<WalletComponent>();
        });

        var logger = Services.Resolve<ILogger>();
        RArenaSaveFileDispatcher.logger = logger;
        logger.LogInformation("Arena server mod initialized");
    }
}


