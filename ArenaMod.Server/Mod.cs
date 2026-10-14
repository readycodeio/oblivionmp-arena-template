using ArenaMod.Server.ArenaPlugin;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.SDK.Attributes;

namespace ArenaMod.Server;

// The server mod's entry point.
[ModEntry]
public sealed partial class Mod(IDependencyContainer services, ILogger logger)
{
    private void Init()
    {
        services.RegisterSingleton<Arena>();
        services.RegisterSingleton<ArenaUtils>();
        services.RegisterSingleton<ArenaSaveFileDispatcher>();

        logger.LogInformation("Arena server mod initialized");
    }
}