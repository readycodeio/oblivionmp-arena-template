using Microsoft.Extensions.Logging;
using OblivionMpCSharpMod.Values;

namespace ArenaMod.Client;

public interface IChat
{
    void ShowLocalMessage(string message, Color color);
}

// TODO: Replace with dependency on ChatMod
public class DummyChat(ILogger logger) : IChat
{
    public void ShowLocalMessage(string message, Color color)
    {
        logger.LogDebug("ShowLocalMessage: {message} with color {color}", message, color);
    }
}