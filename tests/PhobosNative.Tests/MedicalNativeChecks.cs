using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using Ostranauts.Trading;
using PhobosMedical.Core;

/// <summary>Phobos Medical 0.1.0 against the installed game's own definitions: the Ward-3's four forms and 3 x 5
/// footprint, the game's bed marks only on the installed intact form, the rest chain and drawer, the stand-in art, and
/// the vanilla shapes the bed relies on, so a game update that moves them fails here rather than in play.</summary>
internal static class MedicalNativeChecks
{
    private static bool Passes(string trigger, JsonCondOwner co) => DataHandler.dictCTs[trigger].TriggeredDataCO(new DataCO(co), false);
    private static JsonCondOwner Without(JsonCondOwner co, string condition) =>
        new() { strName = co.strName, aStartingConds = co.aStartingConds.Where(s => !s.StartsWith(condition + "=", StringComparison.Ordinal)).ToArray() };

    internal static void Run(NativeDefinitions medical, Action<bool, string> check)
    {
        string p = MedicalRules.BedPrefix;
        foreach (string form in MedicalRules.Forms)
        {
            check(medical.Objects.ContainsKey(p + form) && medical.Items.ContainsKey(p + form), "Ward-3 form exists: " + form);
            var item = medical.Items[p + form];
            check(item.nCols == MedicalRules.Width && item.aSocketAdds.Length == MedicalRules.Width * MedicalRules.Depth && item.aSocketReqs.Length == (MedicalRules.Width + 2) * (MedicalRules.Depth + 2),
                "Ward-3 is three tiles across and five deep, with the native border ring: " + form);
            check(medical.Objects[p + form].inventoryWidth == MedicalRules.Width && medical.Objects[p + form].inventoryHeight == MedicalRules.Depth, "Ward-3 carried footprint is 3 x 5: " + form);
            check(medical.Objects[p + form].strNameFriendly.StartsWith("Phobos' Halewright Ward-3 Medical Bed", StringComparison.Ordinal), "Ward-3 carries its full brand name: " + form);
        }
        var installed = medical.Objects[MedicalRules.BedInstalled];
        // The game's own triggers, against the bed as it installs (off) and once power has cleared its Off mark.
        var powered = Without(installed, "IsOff");
        check(!Passes("TIsBedMedical", installed), "An unpowered Ward-3 is not a medical bed to the game (installs off, like the Infirmaway)");
        check(Passes("TIsBedMedical", powered) && Passes("TIsBed", powered) && Passes("TIsBedFree", powered) && Passes("TIsBedInstalled", powered),
            "A powered Ward-3 passes the game's own medical-bed, bed, free-bed and installed-bed tests");
        check(Passes("TIsBed", installed), "An unpowered Ward-3 is still an ordinary bed to the game");
        check(Passes(MedicalRules.BedFreeTrigger, powered), "The rest opener's free-bed test accepts an empty intact Ward-3");
        foreach (string form in new[] { "Loose", "InstalledDmg", "LooseDmg" })
        {
            var co = medical.Objects[p + form];
            check(!Passes("TIsBedMedical01Uninstalled", co) && !Passes("TIsBedMedical", co) && !Passes("TIsBed", co),
                "The Infirmaway's install job and the game's bed tests never take a loose or damaged Ward-3: " + form);
        }
        foreach (string ia in installed.aInteractions)
            check(DataHandler.dictInteractions.ContainsKey(ia), "Ward-3 action resolves: " + ia);
        check(installed.aInteractions.Contains(MedicalRules.Sleep) && installed.aInteractions.Contains(MedicalRules.Rest) && installed.aInteractions.Contains(MedicalRules.Lay)
            && installed.aInteractions.Contains(MedicalRules.Controls), "The installed Ward-3 offers Sleep, Rest, Lay patient here and its Control Panel");
        check(installed.mapPoints.Contains("sleep," + MedicalRules.PatientPointOffset) && installed.mapPoints.Contains("use," + MedicalRules.PatientPointOffset),
            "The patient lies in the middle of the mattress, as on the Infirmaway");

        // The rest chain: the gate to the game's own lie-down first, then the loop, then getting up.
        var loop = DataHandler.dictInteractions[MedicalRules.RestLoop];
        check(loop.aInverse.Length == 3 && loop.aInverse[0].StartsWith(MedicalRules.RestSleep + ",") && loop.aInverse[1].StartsWith(MedicalRules.RestLoop + ",") && loop.aInverse[2].StartsWith(MedicalRules.RestEnd + ","),
            "The rest loop tries falling asleep, then resting on, then getting up");
        var sleep = DataHandler.dictInteractions[MedicalRules.RestSleep];
        check(sleep.CTTestUs == "THasDcSleepComfort02" && sleep.aInverse[0].StartsWith("SeekSleepSimpleLieDownMedical,"), "A sleepy rester falls asleep through the game's own medical lie-down");
        check(!DataHandler.dictInteractions[MedicalRules.Rest].bOpener, "Rest is never an idle opener; the player or a later order chooses it");
        check(DataHandler.dictConds[MedicalRules.Recovering].aPer.SequenceEqual(new[] { MedicalRules.RecuperatingEffect }), "Recovering carries exactly the game's own Recuperating figures");
        check(!DataHandler.dictCTs["TIsNotSleeping"].aForbids.Contains(MedicalRules.Recovering), "An awake rester is not counted asleep by the game's social tests");

        // The bedside drawer: medical goods and dressings only.
        var drawer = installed.strContainerCT;
        check(drawer == MedicalRules.DrawerTrigger && EquipmentInventory.Of(installed.strName)?.Role == InventoryRole.ServiceRack, "The drawer is a declared service rack");
        foreach (string accepted in new[] { "ItmScrapClothClean", "ItmScrapClothDirty", "ItmSplint01", "ItmPillAntibiotic01", "ItmPillPainkillerMinor01" })
            check(Passes(drawer, DataHandler.dictCOs[accepted]), "The drawer takes " + accepted);
        foreach (string refused in new[] { "ItmScrapSteel", "ItmBedMedical01Loose" })
            check(!Passes(drawer, DataHandler.dictCOs[refused]), "The drawer refuses " + refused);

        // The Halewright art on every form, drawn as low as the game's own medical bed so a patient lies on top.
        foreach (string form in MedicalRules.Forms)
        {
            var item = medical.Items[p + form];
            check(item.strImg == "phobos/medical/PhobosMedicalBed" && item.strImgNorm == item.strImg + "Normal" && medical.Objects[p + form].strPortraitImg == item.strImg,
                "Ward-3 shows its own Halewright art: " + form);
            check(item.fZScale == DataHandler.dictItemDefs["ItmBedMedical01"].fZScale, "Ward-3 draws as low as the game's medical bed: " + form);
        }

        // Medical 0.2.0: Send injured crew here, and the weightless-care stat Framework reads.
        check(installed.aInteractions.Contains(MedicalRules.Send) && DataHandler.dictInteractions.ContainsKey(MedicalRules.Send), "The installed Ward-3 offers Send injured crew here");
        check(DataHandler.dictConds.ContainsKey(Phobos.Ostranauts.Framework.Health.WoundGravity.FactorStat), "Framework's weightless-healing stat is a registered condition");
        WoundGravityChecks(check);

        // Medical 0.3.0: the Vigil-2 monitor, a 2 x 2 powered cart that stores nothing.
        foreach (string form in MedicalRules.Forms)
        {
            var co = medical.Objects[MedicalRules.MonitorPrefix + form]; var item = medical.Items[MedicalRules.MonitorPrefix + form];
            check(item.nCols == 2 && item.aSocketAdds.Length == 4 && item.aSocketReqs.Length == 16, "Vigil-2 is two by two with the native border ring: " + form);
            check(co.strContainerCT == null && !co.aInteractions.Contains("Inventory") && EquipmentInventory.Of(co.strName)?.Role == InventoryRole.None, "Vigil-2 stores nothing: " + form);
            check(co.strNameFriendly.StartsWith("Phobos' Halewright Vigil-2 Patient Monitor", StringComparison.Ordinal), "Vigil-2 carries its full brand name: " + form);
            check(item.strImg == "phobos/medical/PhobosMedicalMonitor" && co.strPortraitImg == item.strImg, "Vigil-2 shows its own art: " + form);
            check(!(co.aStartingConds ?? Array.Empty<string>()).Any(c => c.StartsWith("IsBedMedical=") || c.StartsWith("IsCushion=")), "Vigil-2 is never a bed to the game: " + form);
        }
        check(medical.Objects[MedicalRules.MonitorInstalled].aInteractions.Contains(MedicalRules.Controls), "The installed Vigil-2 opens its Control Panel");

        // The vanilla shapes the bed relies on.
        check(DataHandler.dictCTs["TIsBedMedical"].aForbids.Contains("IsOff"), "The game's medical-bed test refuses an Off bed (power mirrors into IsOff)");
        check(DataHandler.dictCTs["TIsBedFree"].aForbids.Contains("IsOccupied"), "The game's free-bed test refuses an occupied bed");
        check(DataHandler.dictCTs["TCanSleep"].aForbids.Contains("Unconscious"), "The game's sleep test refuses the unconscious (why Lay patient here exists)");
        check(DataHandler.dictInteractions[MedicalRules.Sleep].aInverse[0].StartsWith("SeekSleepSimpleLieDownMedical,"), "The game's sleep opener tries its medical lie-down first");
        check(DataHandler.dictLoot["CONDSleepingMedicalPer"] != null && DataHandler.dictConds.ContainsKey(MedicalRules.SleepingMedical), "The game's Recuperating condition and figures exist");
        var payloads = DataHandler.dictLoot["ACTFFWDContextPayloads"].aCOs.Select(s => s.Split('=')[0]).ToList();
        check(payloads.IndexOf("Tick1HourIncapacitated") >= 0 && payloads.IndexOf("Tick1HourIncapacitated") < payloads.IndexOf("Tick1HourSleepMedical")
            && DataHandler.dictCTs["TIsValidTick1HourIncapacitated"].aForbids.Contains(MedicalRules.SleepingMedical),
            "Time skip gives an unconscious patient with medical sleep the game's medical hour, not the incapacitated one");
        check(DataHandler.dictConds["IsOccupied"].bResetTimer, "The game's occupied mark resets its timer when refreshed");
    }

    /// <summary>Framework 0.84.0's wound-gravity patch against the installed game's own <c>Wound.Run</c>: exactly one
    /// <c>ldc.r8 0.05</c> follows the <c>DcGrav01</c> test in its IL (read as bytes, without Harmony's runtime), and the
    /// pure rewrite replaces exactly one constant in a synthetic copy of that shape and leaves any other shape alone.</summary>
    private static void WoundGravityChecks(Action<bool, string> check)
    {
        var run = typeof(Wound).GetMethod("Run", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        check(run != null && run.GetParameters().Length == 1 && run.GetParameters()[0].ParameterType == typeof(double), "The game's Wound.Run(double) exists");
        var il = run!.GetMethodBody()!.GetILAsByteArray()!;
        int matches = 0;
        for (int i = 0; i + 5 <= il.Length; i++)
        {
            if (il[i] != 0x72) continue; // ldstr
            string? text = null;
            try { text = run.Module.ResolveString(BitConverter.ToInt32(il, i + 1)); } catch (ArgumentException) { }
            if (text != Phobos.Ostranauts.Framework.Health.WoundGravity.GravityCondition) continue;
            for (int j = i + 5; j + 9 <= il.Length && j < i + 40; j++)
                if (il[j] == 0x23 && Math.Abs(BitConverter.ToDouble(il, j + 1) - Phobos.Ostranauts.Framework.Health.WoundGravity.NativeFactor) < 1e-12) { matches++; break; }
        }
        check(matches == 1, "The game's Wound.Run multiplies by 0.05 exactly once after its DcGrav01 test (" + matches + ")");
        HarmonyLib.CodeInstruction I(System.Reflection.Emit.OpCode op, object? operand = null) => new(op, operand);
        // The rewrite changes the constant's own instruction in place, as transpilers do, so each case builds a fresh shape.
        System.Collections.Generic.List<HarmonyLib.CodeInstruction> Shape() => new()
        {
            I(System.Reflection.Emit.OpCodes.Ldloc_1), I(System.Reflection.Emit.OpCodes.Ldstr, "DcGrav01"), I(System.Reflection.Emit.OpCodes.Callvirt, typeof(CondOwner).GetMethod("HasCond", new[] { typeof(string) })),
            I(System.Reflection.Emit.OpCodes.Brfalse_S), I(System.Reflection.Emit.OpCodes.Ldloc_2), I(System.Reflection.Emit.OpCodes.Ldc_R8, 0.05), I(System.Reflection.Emit.OpCodes.Mul), I(System.Reflection.Emit.OpCodes.Stloc_2)
        };
        var shape = Shape();
        var rewritten = Phobos.Ostranauts.Framework.Health.WoundGravity.Rewrite(Shape(), out int count);
        check(count == 1 && rewritten.Count == shape.Count + 2 && rewritten[5].opcode == System.Reflection.Emit.OpCodes.Ldloc_1 && rewritten[6].opcode == System.Reflection.Emit.OpCodes.Ldc_R8
            && rewritten[7].opcode == System.Reflection.Emit.OpCodes.Call && rewritten[8].opcode == System.Reflection.Emit.OpCodes.Mul,
            "The rewrite loads the patient and asks Factor in place of the constant");
        var twice = Shape(); twice.AddRange(Shape());
        var untouched = Phobos.Ostranauts.Framework.Health.WoundGravity.Rewrite(twice, out int doubled);
        check(doubled == 2 && untouched.Count == twice.Count && untouched.Count(c => c.opcode == System.Reflection.Emit.OpCodes.Ldc_R8) == 2, "Two matches leave the method unchanged");
        var none = Phobos.Ostranauts.Framework.Health.WoundGravity.Rewrite(Shape().Take(4).ToList(), out int missing);
        check(missing == 0 && none.Count == 4, "No match leaves the method unchanged");
        check(Phobos.Ostranauts.Framework.Health.WoundGravity.Factor(null!, 0.05) == 0.05, "Without a patient the game's own factor stands");
    }
}
