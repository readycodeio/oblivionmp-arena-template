using ReadyM.SDK.Attributes;

namespace ArenaMod.Client;

[Service]
public sealed partial class ArenaTimerCountdown
{
    private float _elapsed;
    public bool Running => _elapsed > 0f;

    private void Update()
    {
        if (ArenaClient.RArenaParticipantReadyCount >= ArenaClient.RArenaParticipantCount / 2)
        {
            _elapsed += Time.DeltaTime;
        }
        else
        {
            _elapsed = 0f;
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