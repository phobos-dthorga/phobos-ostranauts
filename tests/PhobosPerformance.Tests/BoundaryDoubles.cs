using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

public class ConsoleResolver { public void ResolveString() { } }
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string method) { } }
}
namespace Phobos.Ostranauts.Framework
{
    public static class FrameworkInfo { public const string Version = "test"; }
    internal static class Text
    {
        private static readonly Dictionary<string, string> Catalog = JsonSerializer.Deserialize<Dictionary<string, string>>(
            typeof(Text).Assembly.GetManifestResourceStream("PhobosFramework.en.json")!)!;
        internal static string Get(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, Catalog[key], args);
    }
}
namespace Phobos.Ostranauts.Framework.Construction
{ public static class ConstructionRegistry { public static string Describe(bool recipes) => recipes ? "recipes" : "status"; } }
namespace Phobos.Ostranauts.Framework.Diagnostics
{
    internal static class NativePerformance
    {
        internal static bool Command(string[] words, out string response)
        {
            if (Performance.Session != null) return Performance.Session.Command(words, out response);
            response = Text.Get("Performance.unavailable"); return false;
        }
    }
}
