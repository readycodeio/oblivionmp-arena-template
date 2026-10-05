using Microsoft.Extensions.Logging;

namespace ArenaMod.Server.ArenaPlugin;

internal class ArenaSaveFileDispatcher(ILogger logger)
{
    private static readonly string SavesDir = Path.Combine(Directory.GetCurrentDirectory(), "saves");
    
    public readonly string SavesPlayersDir = Path.Combine(SavesDir, "players");
    public readonly string SavesLoadoutDir = Path.Combine(SavesDir, "arena");

    public readonly Dictionary<string, bool> SaveFileSlots = new();
    public readonly Dictionary<Guid, string> PlayerLoadouts = new();

    private static readonly Random Rand = new(DateTime.Now.Millisecond);

    public void InitializeLoadouts()
    {
        if (!Directory.Exists(SavesLoadoutDir))
        {
            Directory.CreateDirectory(SavesLoadoutDir);
        }

        var files = Directory.GetFiles(SavesLoadoutDir);
        if (files.Length <= 0)
        {
            logger.LogWarning("No custom save files to load, place them at {path}", SavesLoadoutDir);
            return;
        }

        foreach (var file in files)
        {
            if (!SaveFileSlots.ContainsKey(file))
            {
                Console.WriteLine("Loaded save file: {0}", file.Split("\\").Last());
                SaveFileSlots[file] = false;
                    
            }
        }
    }

    public string GetRandomLoadout()
    {
        var availableLoadouts = SaveFileSlots.Where(x => !x.Value).Select(x => x.Key).ToList();

        if (availableLoadouts.Count == 0)
        {
            return string.Empty;
        }

        var chosenLoadout = availableLoadouts[Rand.Next(availableLoadouts.Count)];
        SaveFileSlots[chosenLoadout] = true;

        return chosenLoadout;
    }

    public void ReleaseLoadout(Guid playerGuid)
    {
        if (PlayerLoadouts.TryGetValue(playerGuid, out var usedLoadout))
        {
            if (SaveFileSlots.ContainsKey(usedLoadout))
            {
                SaveFileSlots[usedLoadout] = false;
            }
            PlayerLoadouts.Remove(playerGuid);
        }
    }
}