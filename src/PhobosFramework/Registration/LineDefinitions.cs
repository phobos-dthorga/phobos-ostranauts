using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Draw depths of the line families in hundredths of the game's item depth scale (<c>fZScale</c>): each
/// family its own, so lines of different families can share a tile and draw in a fixed order without flicker
/// (Framework 0.56.0; the game's own power conduit uses 1.02 and cargo pods 1.1). Belts lie lowest, under every pipe.</summary>
public static class LineLayers
{
    public const float Belt = 1.03f, ProcessWater = 1.04f, Gas = 1.05f, Acid = 1.06f, Irrigation = 1.07f, Coolant = 1.08f;
    /// <summary>The ethanol line (Framework 0.80.0). The 16-pixel tile has room for five pipe lanes, all taken, so its art
    /// shares the coolant lane's pixels; it draws above the coolant conduit and the two should not share a tile.</summary>
    public const float Ethanol = 1.09f;
    public static readonly IReadOnlyList<float> All = new[] { Belt, ProcessWater, Gas, Acid, Irrigation, Coolant, Ethanol };
}

/// <summary>One family of 1 x 1 line segments (pipe or belt) on the shared pattern: a presence condition on the
/// tile, an intact condition while it works (or none, when the family uses presence alone), a joint sprite sheet,
/// a fixture-port loot that draws the joint on equipment, floor-only placement that forbids only its own family,
/// and pocketable loose stacks. Families never add an obstruction or a power-conduit condition, so different
/// families (and the game's own power conduit) share tiles.</summary>
public sealed class LineSegmentSpec
{
    public string Prefix { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>The damaged forms' name, or null to keep <see cref="Name"/>.</summary>
    public string? DamagedName { get; set; }
    public string Description { get; set; } = "";
    /// <summary>The loose icon path; the installed forms use <c>Art + "Sheet"</c>.</summary>
    public string Art { get; set; } = "";
    public string Present { get; set; } = "";
    /// <summary>The condition an intact segment adds beside <see cref="Present"/>, or null (presence alone).</summary>
    public string? Intact { get; set; }
    public double Kg { get; set; } = 1;
    public double Price { get; set; }
    public string InstallTab { get; set; } = InstallMenu.Hvac;
    public string Controls { get; set; } = "Inventory";
    public int LooseStack { get; set; } = 10;
    public float Layer { get; set; }
    /// <summary>The loot name that draws this family's joint on equipment; default <c>Prefix + "FixturePort"</c>.</summary>
    public string? FixtureLoot { get; set; }
    public string Fixture => FixtureLoot ?? Prefix + "FixturePort";
    /// <summary>The network family id whose ports <see cref="LineDefinitions.AddPort"/> records (Framework 0.57.0), or
    /// null for a family without participants (irrigation and coolant circuits pair their own endpoints).</summary>
    public string? Family { get; set; }
    public string Sprite => Prefix + "Sprite";
}

public static class LineDefinitions
{
    public static readonly string[] Forms = { "Installed", "Loose", "InstalledDmg", "LooseDmg" };
    // Fixture loots by name, so a port one mod adds can combine with a line family another mod published.
    private static readonly Dictionary<string, Loot> fixtures = new(StringComparer.Ordinal);
    /// <summary>Publishes the family's conditions, sprite trigger, loot and four forms. Content adds its own economy,
    /// repair inputs, work rates and remainders afterwards.</summary>
    public static void Add(NativeDefinitions d, LineSegmentSpec s)
    {
        if (string.IsNullOrEmpty(s.Prefix) || string.IsNullOrEmpty(s.Present) || !LineLayers.All.Contains(s.Layer))
            throw new ArgumentException("A line family needs a prefix, a presence condition and one of the shared layers.");
        foreach (string key in s.Intact == null ? new[] { s.Present } : new[] { s.Present, s.Intact })
            d.Conditions[key] = new JsonCond { strName = key, strNameFriendly = s.Name, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        d.Triggers[s.Sprite] = new CondTrigger { strName = s.Sprite, fChance = 1, fCount = 1, bAND = true, aReqs = new[] { s.Present }, aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
        d.Loot[s.Prefix + "Adds"] = new Loot { strName = s.Prefix + "Adds", strType = "condition",
            aCOs = s.Intact == null ? new[] { s.Present + "=1x1" } : new[] { s.Present + "=1x1", s.Intact + "=1x1" }, aLoots = Array.Empty<string>() };
        if (s.Intact != null) d.Loot[s.Prefix + "Off"] = new Loot { strName = s.Prefix + "Off", strType = "condition", aCOs = new[] { s.Present + "=1x1" }, aLoots = Array.Empty<string>() };
        // The presence loot a segment's own centre forbids: one segment of a family per tile, any other family welcome.
        string presence = s.Intact == null ? s.Prefix + "Adds" : s.Prefix + "Off";
        d.Loot[s.Fixture] = new Loot { strName = s.Fixture, strType = "condition", aCOs = new[] { s.Present + "=1x1" }, aLoots = new[] { "TILFixtureAdds=1x1" } };
        fixtures[s.Fixture] = d.Loot[s.Fixture];
        ApplianceDefinitions.Add(d, s.Prefix, s.Name, s.Description, 1, s.Kg, s.Price, s.Art, s.Controls, 0, s.InstallTab);
        d.Power.Remove(s.Prefix + "Power"); d.Interactions.Remove(s.Prefix + "PowerChange");
        LineJobFilter.RegisterFamily(s.Prefix);
        foreach (string form in Forms)
        {
            bool installed = form.StartsWith("Installed", StringComparison.Ordinal), damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            var co = d.Objects[s.Prefix + form]; var item = d.Items[co.strItemDef];
            if (damaged && s.DamagedName != null) co.strNameFriendly = co.strNameShort = s.DamagedName;
            co.nStackLimit = installed ? 1 : s.LooseStack;
            co.jsonPI = null; co.aTickers = Array.Empty<string>(); co.aInteractions = Array.Empty<string>();
            co.mapPoints = new[] { "use,0,-16" };
            co.aStartingConds = co.aStartingConds.Where(x => !x.StartsWith("IsContainer=", StringComparison.Ordinal) && !x.StartsWith("IsCumbersome=", StringComparison.Ordinal))
                .Concat(installed ? Array.Empty<string>() : new[] { "IsPocketable=1x1" }).ToArray();
            // A laid segment is a fixture like the game's power conduit (Framework 0.73.0): out of the ground inventory,
            // never pocketable; loose sections stay pocketable supplies.
            if (installed) ItemHandling.Fixture(d, s.Prefix + form);
            co.nContainerWidth = co.nContainerHeight = 0; co.mapGUIPropMaps = Array.Empty<string>(); co.strContainerCT = null;
            item.fZScale = s.Layer;
            if (!installed) continue;
            item.strImg = s.Art + "Sheet"; item.strImgNorm = item.strImg + "Normal";
            item.bHasSpriteSheet = true; item.ctSpriteSheet = s.Sprite;
            item.aSocketAdds = new[] { s.Intact == null || !damaged ? s.Prefix + "Adds" : s.Prefix + "Off" };
            item.aSocketForbids = Enumerable.Range(0, 9).Select(i => i == 4 ? presence : "Blank").ToArray();
            item.aSocketReqs = Enumerable.Range(0, 9).Select(i => i == 4 ? "TILFloor" : "Blank").ToArray();
        }
    }
    /// <summary>A line port on equipment: the named point on every form and the family's joint drawn on the
    /// installed forms' footprint socket beside it. The first family on an item drives the item's own sprite-sheet
    /// refresh; further families are redrawn by <see cref="LineJoints"/>.</summary>
    public static void AddPort(NativeDefinitions d, string equipmentPrefix, LineSegmentSpec line, string point, int x, int y, int socket)
    {
        foreach (string form in Forms)
        {
            if (!d.Objects.TryGetValue(equipmentPrefix + form, out var co)) continue;
            co.mapPoints = (co.mapPoints ?? Array.Empty<string>()).Where(p => !p.StartsWith(point + ",", StringComparison.Ordinal)).Concat(new[] { point + "," + x + "," + y }).ToArray();
            if (line.Family != null) Liquids.LinePorts.Register(line.Family, equipmentPrefix + form, point);
            if (!form.StartsWith("Installed", StringComparison.Ordinal) || !d.Items.TryGetValue(equipmentPrefix + form, out var item)) continue;
            if (socket < 0 || item.aSocketAdds == null || socket >= item.aSocketAdds.Length) throw new ArgumentException("Line port socket outside the footprint: " + equipmentPrefix + form);
            item.aSocketAdds[socket] = item.aSocketAdds[socket] == "TILFixtureAdds" || item.aSocketAdds[socket] == line.Fixture ? line.Fixture : Combined(d, item.aSocketAdds[socket], line);
            if (string.IsNullOrEmpty(item.ctSpriteSheet) || item.ctSpriteSheet == line.Sprite) item.ctSpriteSheet = line.Sprite;
            else LineJoints.Register(equipmentPrefix + form, line.Sprite);
        }
    }
    /// <summary>A line port on a definition another mod published (Framework 0.59.0), amended in place and never
    /// republished: the named point, the family's joint on the footprint socket when that tile carries only the game's
    /// plain fixture add, and the joint redraw through <see cref="LineJoints"/>. The item's own sprite-sheet trigger is
    /// left alone. Returns false, changing nothing, when the footprint is not the expected one or the socket already
    /// adds something else. Idempotent.</summary>
    public static bool AmendPort(JsonCondOwner co, JsonItemDef item, string definition, int footprint, LineSegmentSpec line, string point, int x, int y, int socket)
    {
        if (co == null || item == null || line?.Family == null || string.IsNullOrEmpty(definition) || item.nCols != footprint ||
            item.aSocketAdds == null || item.aSocketAdds.Length != footprint * footprint || socket < 0 || socket >= item.aSocketAdds.Length) return false;
        string current = item.aSocketAdds[socket];
        if (current != "TILFixtureAdds" && current != line.Fixture) return false;
        item.aSocketAdds[socket] = line.Fixture;
        co.mapPoints = (co.mapPoints ?? Array.Empty<string>()).Where(p => !p.StartsWith(point + ",", StringComparison.Ordinal)).Concat(new[] { point + "," + x + "," + y }).ToArray();
        Liquids.LinePorts.Register(line.Family, definition, point);
        LineJoints.Register(definition, line.Sprite);
        return true;
    }
    // A footprint tile that already draws another family's joint gets one loot adding both presences.
    private static string Combined(NativeDefinitions d, string existing, LineSegmentSpec line)
    {
        if ((!d.Loot.TryGetValue(existing, out var first) && !fixtures.TryGetValue(existing, out first)) || first.strType != "condition")
            throw new ArgumentException("A line port tile already carries " + existing + ".");
        string name = existing + "+" + line.Prefix;
        d.Loot[name] = new Loot { strName = name, strType = "condition", aCOs = first.aCOs.Concat(new[] { line.Present + "=1x1" }).Distinct().ToArray(),
            aLoots = (first.aLoots ?? Array.Empty<string>()).Concat(new[] { "TILFixtureAdds=1x1" }).Distinct().ToArray() };
        fixtures[name] = d.Loot[name];
        return name;
    }
}
