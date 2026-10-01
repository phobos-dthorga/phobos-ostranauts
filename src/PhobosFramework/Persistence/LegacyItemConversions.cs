using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Construction;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Persistence;

/// <summary>Live conversion of retired loose parts that a saved object cannot simply be renamed into (Framework 0.67.0;
/// owner decisions of 1 October 2026: machines are bought or found whole, and saved assembly sections convert
/// automatically). <see cref="DefinitionMigrations"/> renames one saved object into one new one; a retired part
/// instead becomes either a share of one whole item or its own bill of materials, so the conversion runs on the live
/// objects once a ship has loaded:
/// <list type="number">
/// <item>Saved construction sites for retired sections that can never finish (their bill is incomplete) are cancelled
/// through the game's own cancellation, which returns their delivered parts. Complete sites are left to the crew.</item>
/// <item>Free parts on the ship are grouped by kind in ID order. Each complete set becomes one whole item and each
/// leftover part its exact materials, dropped through the game's own deck placement where the part (or whatever held
/// it) lay. Every rule balances mass exactly; masses are checked again on the spawned objects.</item>
/// </list>
/// Only the player's own ships are swept, so merchant stock and derelict loot stay untouched until they come aboard.
/// Parts reserved by a live construction site, inside one, stacked or installed are never touched. The sweep is
/// idempotent and never edits a save file: an interrupted conversion simply repeats from the old save.</summary>
public static class LegacyItemConversions
{
    public sealed class Material
    {
        public string Id { get; }
        public int Count { get; }
        public double UnitKg { get; }
        public Material(string id, int count, double unitKg)
        {
            if (string.IsNullOrEmpty(id) || count < 1 || count > RecipeRules.MaxUnits || !(unitKg > 0) || double.IsInfinity(unitKg))
                throw new ArgumentException("Invalid conversion material: " + id);
            Id = id; Count = count; UnitKg = unitKg;
        }
    }

    public sealed class Rule
    {
        public string RetiredId { get; }
        public double UnitKg { get; }
        public int SetSize { get; }
        public string WholeId { get; }
        public double WholeKg { get; }
        public IReadOnlyList<Material> Materials { get; }
        internal Rule(string retiredId, double unitKg, int setSize, string wholeId, double wholeKg, IReadOnlyList<Material> materials)
        { RetiredId = retiredId; UnitKg = unitKg; SetSize = setSize; WholeId = wholeId; WholeKg = wholeKg; Materials = materials; }
    }

    /// <summary>What one sweep of one ship did.</summary>
    public sealed class Report
    {
        public int SitesCancelled, WholeItems, PartsBrokenDown;
        public bool Any => SitesCancelled + WholeItems + PartsBrokenDown > 0;
    }

    public const double SweepSeconds = 15;
    private static readonly Dictionary<string, Rule> rules = new(StringComparer.Ordinal);
    private static readonly Cadence cadence = new(SweepSeconds);
    public static IReadOnlyCollection<Rule> Rules => rules.Values;
    /// <summary>A new data load: content registers its rules again.</summary>
    internal static void Reset() { rules.Clear(); cadence.Invalidate(); }

    /// <summary>Registers a retired part: <paramref name="setSize"/> of them make one <paramref name="wholeId"/>, and one
    /// left over becomes <paramref name="materials"/>. Both must weigh exactly what the parts weigh.</summary>
    public static void Register(string retiredId, double unitKg, int setSize, string wholeId, double wholeKg, params Material[] materials)
    {
        if (string.IsNullOrEmpty(retiredId) || string.IsNullOrEmpty(wholeId) || retiredId == wholeId || setSize < 1 || setSize > RecipeRules.MaxUnits ||
            materials == null || materials.Length == 0 || materials.Any(m => m == null || m.Id == retiredId))
            throw new ArgumentException("Invalid legacy conversion: " + retiredId);
        if (!RecipeRules.MassMatches(unitKg * setSize, wholeKg) || !RecipeRules.MassMatches(materials.Sum(m => m.Count * m.UnitKg), unitKg))
            throw new ArgumentException("Legacy conversion does not balance mass: " + retiredId);
        rules[retiredId] = new Rule(retiredId, unitKg, setSize, wholeId, wholeKg, materials.ToArray());
    }

    /// <summary>Splits ordered parts into complete sets and leftovers (pure, for tests and the sweep).</summary>
    public static (List<List<T>> Sets, List<T> Leftovers) Group<T>(IReadOnlyList<T> parts, int setSize)
    {
        var sets = new List<List<T>>();
        int whole = setSize < 1 ? 0 : parts.Count / setSize;
        for (int i = 0; i < whole; i++) sets.Add(parts.Skip(i * setSize).Take(setSize).ToList());
        return (sets, parts.Skip(whole * setSize).ToList());
    }

    internal static void Poll()
    {
        if (rules.Count == 0 || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || CrewSim.system?.dictShips == null || !cadence.Due()) return;
        foreach (var ship in CrewSim.system.dictShips.Values.ToArray())
        {
            if (ship == null || ship.bDestroyed || (int)ship.LoadState < 2 || CrewSim.system.GetShipOwner(ship.strRegID) != CrewSim.coPlayer?.strID) continue;
            try
            {
                var report = Sweep(ship);
                if (!report.Any) continue;
                string text = Text.Get("LegacyItemConversions.notice", report.WholeItems, report.PartsBrokenDown) +
                    (report.SitesCancelled > 0 ? " " + Text.Get("LegacyItemConversions.notice_sites", report.SitesCancelled) : "");
                Notices.PlayerNotices.Post(ship, "LegacyItemConversions", Notices.NoticeLevel.Info, text);
            }
            catch (Exception e) { FrameworkLifecycle.Log(Text.Get("LegacyItemConversions.failed", ship.strRegID, e.Message)); }
        }
    }

    /// <summary>Converts every free retired part aboard one loaded ship.</summary>
    internal static Report Sweep(Ship ship)
    {
        var report = new Report();
        if (rules.Count == 0 || ship == null) return report;
        var all = ship.GetCOs(null, true, false, true);
        foreach (var co in all.ToArray())
        {
            if (co == null || !SectionAssembly.IsSectionSite(co, out bool complete) || complete) continue;
            var marker = co.GetComponent<Placeholder>();
            if (marker == null) continue;
            marker.Cancel(null);
            report.SitesCancelled++;
            FrameworkLifecycle.Log(Text.Get("LegacyItemConversions.site_cancelled", co.strID, ship.strRegID));
        }
        if (report.SitesCancelled > 0) all = ship.GetCOs(null, true, false, true);
        var reserved = new HashSet<CondOwner>();
        foreach (var co in all)
            if (co != null && !co.bDestroyed && co.Item != null && co.Item.bPlaceholder)
                foreach (var part in co.GetLotCOs(false)) if (part != null) reserved.Add(part);
        foreach (var rule in rules.Values.OrderBy(r => r.RetiredId, StringComparer.Ordinal))
        {
            var parts = all.Where(c => c != null && c.strCODef == rule.RetiredId && Free(c, ship, reserved))
                .Distinct().OrderBy(c => c.strID, StringComparer.Ordinal).ToList();
            if (parts.Count == 0) continue;
            var (sets, leftovers) = Group(parts, rule.SetSize);
            foreach (var set in sets)
            {
                Replace(ship, set, new[] { (rule.WholeId, rule.WholeKg) }, rule.UnitKg);
                report.WholeItems++;
                FrameworkLifecycle.Log(Text.Get("LegacyItemConversions.whole", rule.SetSize, rule.RetiredId, rule.WholeId, ship.strRegID));
            }
            foreach (var part in leftovers)
            {
                Replace(ship, new[] { part }, rule.Materials.SelectMany(m => Enumerable.Repeat((m.Id, m.UnitKg), m.Count)).ToArray(), rule.UnitKg);
                report.PartsBrokenDown++;
                FrameworkLifecycle.Log(Text.Get("LegacyItemConversions.materials", part.strID, rule.RetiredId, ship.strRegID));
            }
        }
        return report;
    }

    private static bool Free(CondOwner co, Ship ship, HashSet<CondOwner> reserved)
    {
        if (co.bDestroyed || co.ship != ship || co.HasCond("IsInstalled") || reserved.Contains(co) ||
            co.coStackHead != null || co.aStack.Count != 0 || co.GetLotCOs(true).Count != 0) return false;
        for (var parent = co.objCOParent; parent != null; parent = parent.objCOParent)
            if (parent.bDestroyed || parent.Item != null && parent.Item.bPlaceholder) return false;
        return true;
    }

    internal static Vector2 Anchor(CondOwner co)
    {
        var root = co;
        while (root.objCOParent != null) root = root.objCOParent;
        var p = root.tf.position;
        return new Vector2(p.x, p.y);
    }

    // Everything new is created and checked before any part leaves its place; the parts are destroyed only after
    // every output is aboard, and a failed placement puts the parts back where they were and removes the outputs.
    private static void Replace(Ship ship, IReadOnlyList<CondOwner> parts, IReadOnlyList<(string Id, double Kg)> outputs, double unitKg)
    {
        var anchor = Anchor(parts[0]);
        var made = new List<CondOwner>();
        try
        {
            foreach (var (id, kg) in outputs)
            {
                var co = DataHandler.GetCondOwner(id) ?? throw new InvalidOperationException(Text.Get("LegacyItemConversions.missing", id));
                made.Add(co);
                if (!RecipeRules.MassMatches(co.GetCondAmount("StatMass"), kg))
                    throw new InvalidOperationException(Text.Get("LegacyItemConversions.mass_changed", id));
            }
            if (!RecipeRules.MassMatches(parts.Sum(p => p.GetCondAmount("StatMass")), unitKg * parts.Count))
                throw new InvalidOperationException(Text.Get("LegacyItemConversions.mass_changed", parts[0].strCODef));
        }
        catch
        {
            foreach (var co in made) if (!co.bDestroyed) co.Destroy();
            throw;
        }
        var origins = parts.Select(p => p.tf.position).ToArray();
        foreach (var part in parts) part.RemoveFromCurrentHome(true);
        var placed = new List<CondOwner>();
        try
        {
            foreach (var co in made) { Drop(ship, co, anchor); placed.Add(co); }
        }
        catch
        {
            foreach (var co in made)
            {
                if (co.bDestroyed) continue;
                if (placed.Contains(co)) co.RemoveFromCurrentHome(true);
                co.Destroy();
            }
            for (int i = 0; i < parts.Count; i++) { parts[i].tf.position = origins[i]; ship.AddCO(parts[i], true); }
            throw;
        }
        foreach (var part in parts) part.Destroy();
    }

    // The game's own deck drop (as its construction cancellation uses): nearby free tiles, stacking where it can, and
    // whatever does not fit is added where it stands.
    internal static void Drop(Ship ship, CondOwner co, Vector2 anchor)
    {
        co.tf.position = new Vector3(anchor.x, anchor.y, co.tf.position.z);
        var rest = ship.DropCO(co, anchor);
        if (rest != null) ship.AddCO(rest, true);
    }
}
