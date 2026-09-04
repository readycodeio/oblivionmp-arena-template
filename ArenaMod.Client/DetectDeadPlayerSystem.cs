using OblivionMp.Sdk;
using ReadyM.Modloader.Mods;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaMod.Client
{
    public class DetectDeadPlayerSystem : ModSystemBase
    {
        public bool _hasAnnounced;
        private float _previousHp = 100f;
        protected override void OnUpdate(UpdateTick tick)
        {
            if (SDK.Sync.LocalPlayer is { } me)
            {
                if (me.Hp < 1 && _previousHp >= 1)
                {
                    if (!_hasAnnounced)
                    {
                        _hasAnnounced = true;
                        SDK.Chat.ShowLocalMessage("Press Shift+Control+P to respawn!", new OblivionMpCSharpMod.Values.Color(1, 1, 0));
                        SDK.Chat.ShowLocalMessage("Arena Rules:", new OblivionMpCSharpMod.Values.Color(1, 1, 0));
                        SDK.Chat.ShowLocalMessage("Exit arena and you are eliminated.", new OblivionMpCSharpMod.Values.Color(1, 1, 0));
                        SDK.Chat.ShowLocalMessage("Fall below 30% HP and you are eliminated.", new OblivionMpCSharpMod.Values.Color(1, 1, 0));
                        SDK.Chat.ShowLocalMessage("If over half of players are ready, game begins to count down 30 seconds,", new OblivionMpCSharpMod.Values.Color(1, 1, 0));
                        SDK.Chat.ShowLocalMessage("should you fail to enter blood ring before then, you are eliminated.", new OblivionMpCSharpMod.Values.Color(1, 1, 0));
                    }
                }
                _previousHp = me.Hp;
            }
        }
    }
}
