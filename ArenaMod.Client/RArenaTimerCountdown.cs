using ReadyM.SDK.Attributes;

namespace ArenaMod.Client;

[Service]
public sealed partial class RArenaTimerCountdown
{
    private float _elapsed;
    public bool _running = false;

    private void Update()
    {
        //RArenaUtils.DisplayArenaInfo();
        if (!_running)
        {
            if (_elapsed > 0f)
            {
                _elapsed = 0f;
            }

            return;
        }

        if (RArenaClient.RArenaParticipantReadyCount >= (RArenaClient.RArenaParticipantCount / 2))
        {
            _elapsed += Time.DeltaTime;
        }
    }

    public double GetRemainingTime()
    {
        var remainingTime = 30.0 - _elapsed;

        if (remainingTime > 0f)
        {
            return Math.Round(remainingTime, 2);
        }

        return 0;
    }
}