using Microsoft.Extensions.Logging;

namespace ArenaMod.Server.ArenaPlugin;

internal static class ArenaSaveFileDispatcher
{
    public static ILogger? Logger;

    public static readonly string SavesDir = Path.Combine(Directory.GetCurrentDirectory(), "saves");
    public static readonly string SavesPlayersDir = Path.Combine(SavesDir, "players");
    public static readonly string SavesLoadoutDir = Path.Combine(SavesDir, "arena");

    public static readonly Dictionary<string, bool> SaveFileSlots = new();
    public static readonly Dictionary<Guid, string> PlayerLoadouts = new();

    private static readonly Random Rand = new(DateTime.Now.Millisecond);

    public static void InitializeLoadouts()
    {
        if (!Directory.Exists(SavesLoadoutDir))
        {
            Directory.CreateDirectory(SavesLoadoutDir);
        }

        string[] files = Directory.GetFiles(SavesLoadoutDir);
        if (files.Length <= 0)
        {
            Logger?.LogWarning("No custom save files to load, place them at {path}", SavesLoadoutDir);
            return;
        }

        foreach (string file in files)
        {
            if (!SaveFileSlots.ContainsKey(file))
            {
                Console.WriteLine("Loaded save file: {0}", file.Split("\\").Last());
                SaveFileSlots[file] = false;
                    
            }
        }
    }

    public static string GetRandomLoadout()
    {
        var availableLoadouts = SaveFileSlots.Where(x => !x.Value).Select(x => x.Key).ToList();

        if (availableLoadouts.Count == 0)
        {
            return string.Empty;
        }

        string chosenLoadout = availableLoadouts[Rand.Next(availableLoadouts.Count)];
        SaveFileSlots[chosenLoadout] = true;

        return chosenLoadout;
    }

    public static void ReleaseLoadout(Guid playerGuid)
    {
        if (PlayerLoadouts.TryGetValue(playerGuid, out string? usedLoadout))
        {
            if (SaveFileSlots.ContainsKey(usedLoadout))
            {
                SaveFileSlots[usedLoadout] = false;
            }
            PlayerLoadouts.Remove(playerGuid);
        }
    }
}