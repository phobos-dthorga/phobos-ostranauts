using System;
using System.Reflection;

namespace Phobos.Ostranauts.Framework.Data;

/// <summary>The fields every data pack file carries. A pack is one schema's authored tables for one mod: shipped as an
/// embedded resource (and as the readable copy under <c>mods/&lt;Mod&gt;/framework/&lt;schema&gt;.json</c>), then
/// overlaid by player files. Schemas derive their typed pack from this class; unknown fields are refused.</summary>
public abstract class DataPack
{
    /// <summary>Format version of this schema; the loader accepts only the schema's current version.</summary>
    public int schemaVersion;
    /// <summary>Schema name, for example <c>economy</c>; must match the schema being loaded.</summary>
    public string schema = "";
    /// <summary>Free text for authors: what the file holds and which record explains its figures.</summary>
    public string? notes;
}

/// <summary>Where a pack comes from: the owning plugin id, the mod folder name (also the player override folder under
/// <c>BepInEx/config</c>), the schema name and the embedded resource that holds the shipped copy.</summary>
public sealed class DataPackSource
{
    public string Owner { get; }
    public string ModFolder { get; }
    public string Schema { get; }
    public Assembly Assembly { get; }
    public string ResourceName { get; }
    public DataPackSource(string owner, string modFolder, string schema, Assembly assembly, string resourceName)
    {
        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(modFolder) || string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(resourceName))
            throw new ArgumentException("A data pack source needs an owner, a mod folder, a schema and a resource name.");
        Owner = owner; ModFolder = modFolder; Schema = schema; Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly)); ResourceName = resourceName;
    }
}

/// <summary>One rejected player file, kept for the console and the log. Shipped packs never produce problems: a
/// shipped pack that fails is a packaging fault and stops its owner's registration.</summary>
public sealed class DataPackProblem
{
    public string Owner { get; }
    public string Schema { get; }
    public string File { get; }
    public string Message { get; }
    public DataPackProblem(string owner, string schema, string file, string message) { Owner = owner; Schema = schema; File = file; Message = message; }
}
