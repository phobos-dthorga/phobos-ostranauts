using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace Phobos.Ostranauts.Framework.Controls;

internal static class NativeAssemblyAudit
{
    internal static bool Matches(Assembly assembly, string managedDirectory, string expectedHash)
    {
        // BepInEx can load patched game assemblies from bytes, leaving Location empty.
        // Audit the installed donor file in that case; never bypass the pinned hash.
        string path = assembly.Location;
        if (string.IsNullOrEmpty(path))
        {
            if (string.IsNullOrWhiteSpace(managedDirectory) || !Path.IsPathRooted(managedDirectory))
                return false;
            path = Path.Combine(managedDirectory, assembly.GetName().Name + ".dll");
        }
        using var source = File.OpenRead(path);
        using var hash = SHA256.Create();
        return string.Equals(BitConverter.ToString(hash.ComputeHash(source)).Replace("-", ""),
            expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}
