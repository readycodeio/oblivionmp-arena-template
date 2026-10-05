namespace ArenaMod.Server.ArenaPlugin;

internal static class ArenaTimerCountdown
{
    private static float _elapsed;
    private static float _maxTime;
    private static bool _hasFinished;
    private static readonly List<float> TimerStamps = [];
    private static readonly List<float> TimerDefaultStamps = [30.0f, 20.0f, 10.0f, 5.0f, 3.0f, 2.0f, 1.0f];
    private static List<float> _customStamps = [];
    private static Action<float>? OnCountdown { get; set; }
    private static Action? OnCountdownFinished { get; set; }

    public static void InitializeTimerStamps(float maxTime, List<float>? customStamps = null, Action<float>? onCountdown = null, Action? onCountdownFinished = null)
    {
        _maxTime = maxTime;

        TimerStamps.Clear();
        if (customStamps is { Count: > 0 })
        {
            _customStamps = customStamps;
            TimerStamps.AddRange(customStamps);
        }
        else
        {
            TimerStamps.AddRange(TimerDefaultStamps);
        }

        OnCountdown = onCountdown;
        OnCountdownFinished = onCountdownFinished;
    }

    public static void Update(float deltaTime)
    {
        if (_hasFinished || TimerStamps.Count <= 0)
        {
            return;
        }

        _elapsed += deltaTime;
        if (_maxTime - _elapsed <= TimerStamps.First())
        {
            OnCountdown?.Invoke(TimerStamps.First());
            TimerStamps.RemoveAt(0);
        }

        if (TimerStamps.Count <= 0)
        {
            OnCountdownFinished?.Invoke();
            _hasFinished = true;
        }
    }

    internal static void ResetCountdown()
    {
        _elapsed = 0f;
        _hasFinished = false;
        TimerStamps.Clear();
        TimerStamps.AddRange(_customStamps.Count > 0 ? _customStamps : TimerDefaultStamps);
    }
}