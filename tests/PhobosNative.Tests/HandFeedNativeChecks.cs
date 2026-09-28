using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

/// <summary>Round 4 of the vanilla-precedence audit (29 September 2026): the game's ordinary walls are
/// cosmetic overlay variants of one base with their own masses, and its PDA Reload job keeps a container
/// stocked until cancelled. Data and signature checks against the game files; not a game session.</summary>
internal static class HandFeedNativeChecks
{
    // Loose overlay mass = base 24 kg + the variant's condition-loot delta (data/loot/loot.json CNDOLWall*).
    private static readonly Dictionary<string, double> ExpectedKg = new(StringComparer.Ordinal) {
        ["ItmWallAERO01Loose"] = 14, ["ItmWallAERO01bLoose"] = 14, ["ItmWallAERO02Loose"] = 14, ["ItmWallLDPH01Loose"] = 48,
        ["ItmWallMSHG01Loose"] = 25, ["ItmWallMSHG02Loose"] = 25, ["ItmWallMSHG03Loose"] = 25, ["ItmWallMSHG04Loose"] = 25, ["ItmWallMSHG05Loose"] = 25,
        ["ItmWallMSSLFWhiteLoose"] = 20, ["ItmWallRYOB01Loose"] = 28, ["ItmWallRYOB02Loose"] = 28, ["ItmWallTSDO01Loose"] = 25, ["ItmWallTSDO44Loose"] = 25,
        ["ItmWallVHRBLoose"] = 27 };

    internal static void Run(NativeDefinitions prepared, Action<bool, string> check)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var loose = DataHandler.dictCOOverlays.Values.Where(o => o.strCOBase == ProcessRules.Wall).ToDictionary(o => o.strName, StringComparer.Ordinal);
        check(loose.Count == ExpectedKg.Count && ExpectedKg.Keys.All(loose.ContainsKey), "The game ships fifteen loose ordinary-wall variants on the plain loose wall");
        var baseWall = DataHandler.dictCOs[ProcessRules.Wall];
        double baseKg = Stat(baseWall.aStartingConds, "StatMass");
        check(baseKg == ProcessRules.StandardWallKg, "The plain loose wall weighs 24 kg");
        var feedTrigger = DataHandler.dictCTs[prepared.Objects[Content.InputBin].strContainerCT];
        check(feedTrigger.TriggeredDataCO(new DataCO(baseWall), false), "The feed's game-level rule admits the loose base every variant resolves to");
        foreach (var overlay in loose.Values)
        {
            check(WallIdentity.IsLooseOrdinary(overlay.strName) && !WallIdentity.IsInstalledOrdinary(overlay.strName), "Variant identity resolves to the loose base: " + overlay.strName);
            double kg = baseKg + Delta(DataHandler.dictLoot[overlay.strCondLoot].aCOs, "StatMass");
            check(kg == ExpectedKg[overlay.strName] && ProcessRules.AcceptedWallKg(kg), "Variant mass is a whole kilogram inside the accepted range: " + overlay.strName + " " + kg);
            // The game's overlay constructor rolls its loot with UnityEngine.Random, so read the loot terms as data.
            var lootNames = DataHandler.dictLoot[overlay.strCondLoot].aCOs.Select(c => c.Split('=')[0]).ToArray();
            check(!lootNames.Any(n => n == "IsInstalled" || n == "IsOversized" || n == "-IsWall1x1" || n == "-IsCumbersome"),
                "The variant's condition loot keeps the feed rule's terms (IsWall1x1, cumbersome, not installed): " + overlay.strName);
            check(ProcessRecipes.ForWallMass(kg).Current.Products.Any(p => p.Id == ReclaimerRules.Feedstock), "A recipe exists for the variant's mass: " + overlay.strName);
        }
        foreach (var installed in DataHandler.dictCOOverlays.Values.Where(o => o.strCOBase == ProcessRules.InstalledWall))
        {
            check(WallIdentity.IsInstalledOrdinary(installed.strName), "Installed variant identity resolves to the ordinary wall: " + installed.strName);
            var map = installed.mapModeSwitches ?? Array.Empty<string>();
            int at = Array.IndexOf(map, ProcessRules.Wall);
            check(at >= 0 && at + 1 < map.Length && loose.ContainsKey(map[at + 1]), "Uninstalling the variant keeps the variant on the loose replacement: " + installed.strName);
        }
        check(!WallIdentity.IsLooseOrdinary("ItmWallPlastic1x1Loose") && new DataCO(DataHandler.dictCOs["ItmWallPlastic1x1Loose"]).HasCond("IsWall1x1"),
            "DuraWal carries IsWall1x1 too, so identity is the base definition, not the condition");
        check(WallIdentity.Base("NoSuchDefinition") == "NoSuchDefinition" && WallIdentity.Base(null) == null, "An unknown or missing definition is its own base");
        var heavy = ProcessingService.OutputSizes(ProcessRecipes.ForWallMass(ProcessRules.MaximumWallKg).Current);
        check(heavy != null && BatchPlacement.Plan(new bool[ProcessRules.OutputSize, ProcessRules.OutputSize], heavy) != null,
            "The heaviest wall's products fit an empty 8 x 8 product tray");
        // Hand loading of the F6 relies on the game's own right-click placement of one unit off a stack.
        check(typeof(CondOwner).GetMethod("PopHeadFromStack", flags)?.ReturnType == typeof(CondOwner), "The game pops one unit off a stack for right-click placement");
        // The game's own persistent loading job: PDA LOAD paints ACTReloadItem<rule>, searched ship-wide, never removed on completion.
        check(DataHandler.dictInteractions.TryGetValue("ACTReloadItem", out var reload) && reload.strDuty == "Haul", "The game's Reload job exists as a Haul duty");
        var reloadFactory = typeof(WorkManager).GetMethod("GetReloadInteraction", flags);
        check(reloadFactory != null && reloadFactory.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(CondTrigger) }),
            "The game builds a Reload action per container rule");
        check(typeof(Ship).GetMethod("GetCOs", new[] { typeof(CondTrigger), typeof(bool), typeof(bool), typeof(bool) }) != null,
            "The ship-wide search the Reload job uses exists (rule, sub-objects, docked, locked)");
        var complete = typeof(WorkManager).GetMethod("CompleteTask", flags);
        check(complete != null && Strings(complete).Contains("ACTReloadItem"), "Completing a step keeps a Reload task listed: it persists until cancelled");
        check(typeof(CondOwner).GetMethod("RootParent", flags) != null && typeof(CondOwner).GetMethod("RemoveFromCurrentHome", flags) != null &&
            typeof(Ship).GetMethod("AddCO", new[] { typeof(CondOwner), typeof(bool) }) != null, "Deck items can be taken and put back through the game's own home handling");
        // The right-click toggle sits on the three intact installed machines only.
        var toggle = prepared.Interactions[IndustrialRules.FeedOrder];
        check(toggle.strRaiseUI == null && toggle.strThemType == DataHandler.dictInteractions["Inventory"].strThemType, "The loading toggle raises no window and targets the machine like Inventory");
        var carriers = prepared.Objects.Values.Where(o => o.aInteractions != null && o.aInteractions.Contains(IndustrialRules.FeedOrder)).Select(o => o.strName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        check(carriers.SequenceEqual(new[] { FurnaceRules.Prefix + "Installed", "PhobosScrapReclaimerInstalled", "PhobosShipbreakerInstalled" }), "Only the intact installed D4, R4 and F6 offer Load feed by crew: " + string.Join(",", carriers));
        check(carriers.All(n => prepared.Objects[n].aInteractions.Count(i => i == IndustrialRules.FeedOrder) == 1), "The toggle appears once per machine");
    }

    private static double Stat(IEnumerable<string> conditions, string name) =>
        conditions.Where(c => c.StartsWith(name + "=", StringComparison.Ordinal)).Select(c => Value(c)).FirstOrDefault();
    /// <summary>Signed value of a condition-loot entry such as "-StatMass=1.0x10.0" or "StatMass=1.0x24.0".</summary>
    private static double Delta(IEnumerable<string> loot, string name) => loot
        .Where(c => c.TrimStart('-').StartsWith(name + "=", StringComparison.Ordinal))
        .Sum(c => (c.StartsWith("-", StringComparison.Ordinal) ? -1 : 1) * Value(c.TrimStart('-')));
    private static double Value(string entry) => double.Parse(entry.Split('=')[1].Split('x')[1], CultureInfo.InvariantCulture);
    private static List<string> Strings(MethodInfo method)
    {
        var codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!).ToDictionary(c => unchecked((ushort)c.Value));
        byte[] il = method.GetMethodBody()!.GetILAsByteArray()!;
        var strings = new List<string>();
        for (int offset = 0; offset < il.Length;)
        {
            ushort code = il[offset++];
            if (code == 0xfe) code = (ushort)(0xfe00 | il[offset++]);
            var op = codes[code];
            if (op == OpCodes.Ldstr) strings.Add(method.Module.ResolveString(BitConverter.ToInt32(il, offset)));
            offset += op.OperandType switch {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, offset),
                _ => 4
            };
        }
        return strings;
    }
}
