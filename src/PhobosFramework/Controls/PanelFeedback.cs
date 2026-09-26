using System;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Presentation only: retain command feedback unless successful text is
/// already a complete line or paragraph in the freshly read live status.</summary>
public static class PanelFeedback
{
    public static string Additional(bool success, string message, string liveStatus)
    {
        if (!success || message.Length == 0) return message;
        string status = "\n" + liveStatus.Replace("\r\n", "\n") + "\n";
        string response = "\n" + message.Replace("\r\n", "\n") + "\n";
        return status.IndexOf(response, StringComparison.Ordinal) >= 0 ? "" : message;
    }
}
