namespace ArenaMod.Server.ArenaPlugin;

internal static class ArenaTimerCountdown
{
    private static float _elapsed;
    private static float _maxTime;
    private static bool _hasFinished;
    private static readonly List<float> _timerStamps = [];
    private static readonly List<float> _timerDefaultStamps = [30.0f, 20.0f, 10.0f, 5.0f, 3.0f, 2.0f, 1.0f];
    private static List<float> _customStamps = [];
    public static Action<float>? OnCountdown { get; set; }
    public static Action? OnCountdownFinished { get; set; }

    public static void InitializeTimerStamps(float maxTime, List<float>? customStamps = null, Action<float>? onCountdown = null, Action? onCountdownFinished = null)
    {
        _maxTime = maxTime;

        _timerStamps.Clear();
        if (customStamps is { Count: > 0 })
        {
            _customStamps = customStamps;
            _timerStamps.AddRange(customStamps);
        }
        else
        {
            _timerStamps.AddRange(_timerDefaultStamps);
        }

        OnCountdown = onCountdown;
        OnCountdownFinished = onCountdownFinished;
    }

    public static void Update(float deltaTime)
    {
        if (_hasFinished || _timerStamps.Count <= 0)
        {
            return;
        }

        _elapsed += deltaTime;
        if (_maxTime - _elapsed <= _timerStamps.First())
        {
            OnCountdown?.Invoke(_timerStamps.First());
            _timerStamps.RemoveAt(0);
        }

        if (_timerStamps.Count <= 0)
        {
            OnCountdownFinished?.Invoke();
            _hasFinished = true;
        }
    }

    internal static void ResetCountdown()
    {
        _elapsed = 0f;
        _hasFinished = false;
        _timerStamps.Clear();
        _timerStamps.AddRange(_customStamps.Count > 0 ? _customStamps : _timerDefaultStamps);
    }
}