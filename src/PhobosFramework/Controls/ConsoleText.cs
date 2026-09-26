namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Shared console messages without a dependency on Unity widget initialization.</summary>
public static class ConsoleText
{
    public static string Get(string key, params object[] args) => Framework.Text.Get("Console." + key, args);
}
