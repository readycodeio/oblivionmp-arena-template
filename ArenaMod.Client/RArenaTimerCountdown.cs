using ReadyM.Modloader.Mods;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaMod.Client
{
    public class RArenaTimerCountdown : ModSystemBase
    {
        private float _elapsed = 0f;
        public bool _running = false;

        protected override void OnUpdate(UpdateTick tick)
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
                _elapsed += tick.deltaTime;
            }
        }

        public double GetRemainingTime()
        {
            double remainingTime = 30.0 - _elapsed;
            if (remainingTime > 0f)
            {
                return Math.Round(remainingTime, 2);
            }
            else
            {
                return 0;
            }
        }
    }
}
