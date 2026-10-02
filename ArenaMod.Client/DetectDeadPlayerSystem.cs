using OblivionMp.Sdk;
using OblivionMpCSharpMod.Values;
using ReadyM.SDK.Attributes;

namespace ArenaMod.Client;

[Service]
public sealed partial class DetectDeadPlayerSystem(IChat chat)
{
    private float _previousHp = 100f;

    public bool HasAnnounced { get; set; }

    private void Update()
    {
        if (SDK.Entities.LocalMainCharacter is { } me)
        {
            if (me.Hp < 1 && _previousHp >= 1)
            {
                if (!HasAnnounced)
                {
                    HasAnnounced = true;
                    chat.ShowLocalMessage("Press Shift+Control+P to respawn!", new Color(1, 1, 0));
                    chat.ShowLocalMessage("Arena Rules:", new Color(1, 1, 0));
                    chat.ShowLocalMessage("Exit arena and you are eliminated.", new Color(1, 1, 0));
                    chat.ShowLocalMessage("Fall below 30% HP and you are eliminated.", new Color(1, 1, 0));
                    chat.ShowLocalMessage("If over half of players are ready, game begins to count down 30 seconds,", new Color(1, 1, 0));
                    chat.ShowLocalMessage("should you fail to enter blood ring before then, you are eliminated.", new Color(1, 1, 0));
                }
            }

            _previousHp = me.Hp;
        }
    }
}