using OblivionMpCSharpMod.Values;

namespace ArenaMod.Client;

public interface IChat
{
    void ShowLocalMessage(string message, Color color);
}