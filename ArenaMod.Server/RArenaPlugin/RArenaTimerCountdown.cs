using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaMod.Server.RArenaPlugin
{
    internal static class RArenaTimerCountdown
    {
        private static float _elapsed = 0f;
        private static float _maxTime = 0f;
        private static bool _hasFinished = false;
        private static List<float> _timerStamps = new List<float>();
        private static List<float> _timerDefaultStamps = [30.0f, 20.0f, 10.0f, 5.0f, 3.0f, 2.0f, 1.0f];
        private static List<float> _customStamps = new List<float>();
        public static Action<float>? OnCountdown { get; set; } = null;
        public static Action? OnCountdownFinished { get; set; } = null;
        public static void InitializeTimerStamps(float maxTime, List<float>? customStamps = null, Action<float>? onCountdown = null, Action? onCountdownFinished = null)
        {
            _maxTime = maxTime;

            _timerStamps.Clear();
            if (customStamps != null && customStamps.Count > 0)
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
}
