using OblivionMp.ChatMod;
using OblivionMp.Sdk;
using OblivionMpCSharpMod.Values;
using ReadyM.SDK.Attributes;

namespace ArenaMod.Client;

[Service]
public sealed partial class DetectDeadPlayerSystem(IChat chat, IPlayers players)
{
    private float _previousHp = 100f;

    public bool HasAnnounced { get; set; }

    private void Update()
    {
        if (players.LocalMainCharacter is { } me)
        {
            if (me.Hp < 1 && _previousHp >= 1)
            {
                if (!HasAnnounced)
                {
                    HasAnnounced = true;
                    chat.ShowLocal("Press Shift+Control+P to respawn!", new Color(1, 1, 0));
                    chat.ShowLocal("Arena Rules:", new Color(1, 1, 0));
                    chat.ShowLocal("Exit arena and you are eliminated.", new Color(1, 1, 0));
                    chat.ShowLocal("Fall below 30% HP and you are eliminated.", new Color(1, 1, 0));
                    chat.ShowLocal("If over half of players are ready, game begins to count down 30 seconds,", new Color(1, 1, 0));
                    chat.ShowLocal("should you fail to enter blood ring before then, you are eliminated.", new Color(1, 1, 0));
                }
            }

            _previousHp = me.Hp;
        }
    }
}