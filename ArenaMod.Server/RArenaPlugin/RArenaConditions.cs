using ReadyM.Api.Idents;
using ReadyM.Relay.Common.Oblivion.ECS.Components;
using ReadyM.Relay.Server.Sdk.Ecs;
using Timer = System.Timers.Timer;

namespace ArenaMod.Server.RArenaPlugin
{
    public class RArenaCondition : IRArenaCondition
    {
        public int Priority { get; set; }

        public virtual void OnUpdate(float tick)
        {
            throw new NotImplementedException();
        }

        public virtual bool WasFullfilled()
        {
            throw new NotImplementedException();
        }

        public virtual void Reset()
        {
            throw new NotImplementedException();
        }
    }

    public class RArenaPlayerLossCondition(RArena rArena, EcsApi ecsApi, RArenaServerRpc sRpc) : RArenaCondition
    {
        private float _playerHpRateLossLimit = 0.3f;
        private float _playerHpLossLimit = 1;
        private EArenaLossCondition _arenaHpLossCondition;

        public RArenaPlayerLossCondition(RArena rArena, EcsApi ecsApi, RArenaServerRpc sRpc,
            int priority, 
            float targetHpPercentage = 0.3f, float targetHpLimit = 1, 
            EArenaLossCondition arenaHpLossCondition = EArenaLossCondition.PlayerDead)
            : this(rArena, ecsApi, sRpc)
        {
            this.Priority = priority;
            this._playerHpRateLossLimit = targetHpPercentage;
            this._playerHpLossLimit = targetHpLimit;
            this._arenaHpLossCondition = arenaHpLossCondition;
        }

        public override void OnUpdate(float tick)
        {
            ecsApi.QueryWithEntity<MainCharacterComponent, VitalsComponent>((ref main, ref vital, id) =>
            {
                if (rArena.Participants.TryGetValue(main.PlayerId, out var status) && status == EArenaParticipantStatus.Fighting)
                {
                    bool playerLost = false;

                    float maxHp = vital.HpBase > 0 ? vital.HpBase : 1;
                    float currentHpPercentage = vital.Hp / maxHp;

                    switch (_arenaHpLossCondition)
                    {
                        case EArenaLossCondition.PlayerDead:
                            if (vital.Hp <= _playerHpLossLimit) playerLost = true;
                            break;

                        case EArenaLossCondition.PlayerHpBelowLimit:
                            if (currentHpPercentage <= _playerHpRateLossLimit || vital.Hp <= _playerHpLossLimit) playerLost = true;
                            break;
                    }

                    if (playerLost)
                    {
                        rArena.Participants[main.PlayerId] = EArenaParticipantStatus.Loss;
                        foreach (var p in rArena.Participants.Keys)
                        {
                            sRpc.SendPlayerEleminated(p, main.PlayerId);
                        }
                        RArenaUtils.Heal(id, ref vital);
                    }
                }
            });
        }

        public override bool WasFullfilled()
        {
            return false;
        }

        public override void Reset()
        {
            
        }
    }

    public class RArenaLMSCondition(RArena rArena) : RArenaCondition
    {
        private int _playersRemaining = 0;
        private bool _hasFinished = false;
        private bool _hasInit = false;
        private PlayerId _winner = PlayerId.Invalid;

        public RArenaLMSCondition(RArena rArena, int priority) : this(rArena)
        {
            this.Priority = priority;
        }

        public override void OnUpdate(float tick)
        {
            if (!_hasInit)
            {
                _hasInit = true;
                _playersRemaining = rArena.Participants
                    .Count(p => p.Value == EArenaParticipantStatus.Fighting);
            }

            if (_playersRemaining <= 0) return;

            if (_hasFinished) return;

            _playersRemaining = rArena.Participants
                .Count(p => p.Value == EArenaParticipantStatus.Fighting);

            if (_playersRemaining < 2)
            {
                _hasFinished = true;
                _winner = rArena.Participants.FirstOrDefault(b => b.Value == EArenaParticipantStatus.Fighting).Key;
                rArena.Participants[_winner] = EArenaParticipantStatus.Win;
            }
        }

        public override bool WasFullfilled()
        {
            return _hasFinished;
        }

        public override void Reset()
        {
            _playersRemaining = -1;
            _hasFinished = false;
            _hasInit = false;
            _winner = PlayerId.Invalid;
        }

        public PlayerId Winner => _winner;
    }

    public class RArenaTimerContition : RArenaCondition
    {
        private Timer _timer = new();
        private bool _hasFinished = false;

        public RArenaTimerContition(float TimeLimit)
        {
            _timer.Interval = TimeLimit * 1000;
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

        public override void OnUpdate(float tick)
        {
            //if (!_timer.Enabled) return;
        }

        public override bool WasFullfilled() => _hasFinished;
    }
}
