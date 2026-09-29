using System;

namespace Phobos.Ostranauts.Framework.Localization;

/// <summary>A status line kept as a translation key plus arguments and formatted only when something reads it. A
/// service that updates its status every power step pays nothing while the key and arguments stay the same, and
/// panels that refresh a few times a second are the only formatters. Presentation only: the key and arguments are
/// never read back to drive gameplay, and the resolved text follows the selected language.</summary>
public sealed class DeferredMessage
{
    private static readonly object[] None = Array.Empty<object>();
    private readonly Func<string, object[], string> format;
    private string key; private object[] args;
    private string? resolved, resolvedLanguage;
    public DeferredMessage(Func<string, object[], string> format, string key, params object[] args)
    {
        this.format = format ?? throw new ArgumentNullException(nameof(format));
        this.key = key ?? throw new ArgumentNullException(nameof(key));
        this.args = args ?? None;
    }
    public string Key => key;
    /// <summary>Whether the message already carries this key and these arguments (element-wise equality).</summary>
    public bool Same(string key, params object[] args)
    {
        if (!string.Equals(this.key, key, StringComparison.Ordinal)) return false;
        args ??= None;
        if (this.args.Length != args.Length) return false;
        for (int i = 0; i < args.Length; i++) if (!Equals(this.args[i], args[i])) return false;
        return true;
    }
    /// <summary>Replaces the message unless it is the same; returns whether it changed.</summary>
    public bool Set(string key, params object[] args)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (Same(key, args)) return false;
        this.key = key; this.args = args ?? None; resolved = null; return true;
    }
    /// <summary>Allocation-free replacement for a message without arguments.</summary>
    public bool Set(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (args.Length == 0 && string.Equals(this.key, key, StringComparison.Ordinal)) return false;
        this.key = key; args = None; resolved = null; return true;
    }
    /// <summary>The formatted text in the selected language, formatted once per change or language switch.</summary>
    public string Resolve()
    {
        string language = Translations.Language;
        if (resolved == null || !string.Equals(resolvedLanguage, language, StringComparison.Ordinal))
        { resolved = format(key, args); resolvedLanguage = language; }
        return resolved;
    }
    public override string ToString() => Resolve();
}
