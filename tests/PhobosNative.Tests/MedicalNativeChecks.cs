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

        // Stand-in art: the Infirmaway's own images by name, drawn as low as the game's beds.
        foreach (string form in MedicalRules.Forms)
        {
            var donor = DataHandler.dictItemDefs[MedicalRules.StandInDonor(form)];
            var item = medical.Items[p + form];
            check(item.strImg == donor.strImg && item.strImgNorm == donor.strImgNorm && item.fZScale == donor.fZScale, "Ward-3 shows the matching Infirmaway art until its own is made: " + form);
        }

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
}
