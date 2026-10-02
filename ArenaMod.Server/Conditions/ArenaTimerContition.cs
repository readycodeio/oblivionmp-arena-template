using Timer = System.Timers.Timer;

namespace ArenaMod.Server.Conditions;

public class ArenaTimerContition : ArenaCondition
{
    private readonly Timer _timer = new();
    private bool _hasFinished;

    public ArenaTimerContition(float timeLimit)
    {
        _timer.Interval = timeLimit * 1000;
        _timer.AutoReset = false;
        _timer.Enabled = false;

        _timer.Elapsed += (_, _) => { _hasFinished = true; };
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Reset(bool enable = false)
    {
        // xd
        // "If the interval is set after the Timer has started, the count is reset."
        _timer.Stop();
        _timer.Interval = _timer.Interval;
        if (enable) _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public override void OnUpdate()
    {
        //if (!_timer.Enabled) return;
    }

    public override bool WasFullfilled() => _hasFinished;

    public override void Reset()
    {
        throw new NotImplementedException();
    }
}