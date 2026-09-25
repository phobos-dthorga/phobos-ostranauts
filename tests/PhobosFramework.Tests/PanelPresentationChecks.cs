using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Phobos.Ostranauts.Framework.Controls;

internal static class PanelPresentationChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var disk = typeof(PanelPresentationChecks).Assembly;
        byte[] bytes = File.ReadAllBytes(disk.Location);
        var memory = Assembly.Load(bytes);
        string expected = Convert.ToHexString(SHA256.HashData(bytes));
        string directory = Path.Combine(Path.GetTempPath(), "phobos-widget-audit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string donor = Path.Combine(directory, memory.GetName().Name + ".dll");
        try
        {
            check(memory.Location.Length == 0, "Regression fixture reproduces a byte-loaded assembly with no Location");
            File.WriteAllBytes(donor, bytes);
            check(NativeAssemblyAudit.Matches(memory, directory, expected), "Byte-loaded native donor resolves through the loader's managed directory");
            check(!NativeAssemblyAudit.Matches(memory, "", expected), "Missing managed directory cannot fall back to the current directory");
            File.AppendAllText(donor, "changed");
            check(!NativeAssemblyAudit.Matches(memory, directory, expected), "Changed fallback donor remains rejected by the original hash audit");
            check(NativeAssemblyAudit.Matches(disk, directory, expected), "File-backed assemblies retain their own location instead of an unrelated fallback");
            File.Delete(donor);
            bool missing = false;
            try { NativeAssemblyAudit.Matches(memory, directory, expected); }
            catch (FileNotFoundException) { missing = true; }
            check(missing, "Missing donor cannot silently enable unaudited controls");
        }
        finally { if (File.Exists(donor)) File.Delete(donor); Directory.Delete(directory); }

        // Local Noto Sans SC face audit: 32-point face, 45.88-unit native line height.
        // Three native 26-point lines exceed the hub's 92-unit inset metrics field.
        check(3 * 26 * 45.88 / 32 > 92, "Native font metrics reproduce the telemetry overflow missed by the browser preview");
        foreach (float size in new[] { 24f, 26f })
        foreach (var face in new[] { (32f, 1f, 45.88f), (48f, 1f, 59.765625f), (32f, .5f, 45.88f) })
        {
            float spacing = FixedTextMetrics.LineSpacing(face.Item1, face.Item2, face.Item3);
            float baseline = size * face.Item3 * face.Item2 / face.Item1 + size * .01f * spacing;
            check(Math.Abs(baseline - size * FixedTextMetrics.LineHeightEm) < .0001,
                "Fixed fields use the same baseline spacing across native font metrics without reducing font size");
            check(3 * baseline < 92, "Three telemetry lines fit the designated inset field");
        }
        check(FixedTextMetrics.LineSpacing(0, 1, 40) == 0 && FixedTextMetrics.LineSpacing(32, float.NaN, 40) == 0,
            "Unavailable font metrics do not inject nonfinite layout values");
    }
}
