using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using PhobosMedical.Core;

namespace PhobosMedical;

/// <summary>Native definitions for the Halewright Ward-3 Medical Bed: the four-form family, its marks, its drawer, its
/// care conditions and the rest chain. The installed intact form carries the game's own bed marks
/// (<c>IsBedMedical</c>, <c>IsCushion</c>, <c>IsSheet</c>), so the game's sleep chain, time skip and other mods treat it
/// as a medical bed; loose and damaged forms carry none, so the Infirmaway's own install job and the fixers' lists
/// never take one. The rest chain is cloned from the game's own chair and sleep actions at load.</summary>
internal static class Definitions
{
    internal const string ImagePath = "phobos/medical/";
    internal static void Add(NativeDefinitions d)
    {
        AddConditions(d);
        AddInteractions(d);
        AddBed(d);
    }

    private static void Condition(NativeDefinitions d, string name, string color, int display, string[]? per = null) =>
        d.Conditions[name] = new JsonCond { strName = name, strNameFriendly = Text.Get("Condition." + name), strDesc = Text.Get("Condition." + name + "_desc"),
            strColor = color, nDisplaySelf = display, nDisplayOther = display, aPer = per ?? Array.Empty<string>(), fClampMax = 1, bRemoveAll = true };

    private static void AddConditions(NativeDefinitions d)
    {
        Condition(d, MedicalRules.InUse, "Neutral", 2);
        Condition(d, MedicalRules.Resting, "Neutral", 2);
        Condition(d, MedicalRules.Rested, "Neutral", 0);
        Condition(d, MedicalRules.CareMark, "Neutral", 0);
        // The game's own Recuperating figures on an awake patient, unchanged.
        Condition(d, MedicalRules.Recovering, "Good", 2, new[] { MedicalRules.RecuperatingEffect });
        d.Loot[MedicalRules.RestStartLoot] = new Loot { strName = MedicalRules.RestStartLoot, strType = "condition", aCOs = new[] { MedicalRules.Resting + "=1.0x1" }, aLoots = Array.Empty<string>() };
        d.Loot[MedicalRules.RestStopLoot] = new Loot { strName = MedicalRules.RestStopLoot, strType = "condition",
            aCOs = new[] { "-" + MedicalRules.Resting + "=1.0x1", "-" + MedicalRules.Rested + "=1.0x1", "-" + MedicalRules.Recovering + "=1.0x1", "-" + MedicalRules.CareMark + "=1.0x1" }, aLoots = Array.Empty<string>() };
        string machine = MedicalRules.BedMachine;
        d.Triggers[MedicalRules.BedFreeTrigger] = new CondTrigger { strName = MedicalRules.BedFreeTrigger, fChance = 1, fCount = 1, bAND = true,
            aReqs = new[] { machine, "IsInstalled", "IsCushion", "IsSheet" }, aForbids = new[] { "IsDamaged", "IsOccupied" }, aTriggers = Array.Empty<string>() };
        // Resting goes on while the patient is awake, still injured and not driven up by hunger, thirst or the toilet
        // (the game's own sitting discomforts, without the pain, infection and air bands a patient is there for).
        d.Triggers[MedicalRules.CanRestTrigger] = new CondTrigger { strName = MedicalRules.CanRestTrigger, fChance = 1, fCount = 1, bAND = true,
            aReqs = new[] { MedicalRules.Resting }, aForbids = new[] { MedicalRules.Rested, "Unconscious", "IsDead", "IsInCombat", "InSocialCombat",
                "DcFood03", "DcFood04", "DcFood05", "DcHydration03", "DcHydration04", "DcHydration05", "DcDefecate03", "DcDefecate04", "DcDefecate05" },
            aTriggers = Array.Empty<string>() };
        // The bedside drawer takes medical goods and scrap cloth (dressings), never installed equipment or people.
        d.Triggers[MedicalRules.DrawerClothTrigger] = new CondTrigger { strName = MedicalRules.DrawerClothTrigger, fChance = 1, fCount = 1, bAND = true,
            aReqs = new[] { "IsScrap", "IsSheet", "IsAbsorbent" }, aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
        d.Triggers[MedicalRules.DrawerTrigger] = new CondTrigger { strName = MedicalRules.DrawerTrigger, fChance = 1, fCount = 1, bAND = false,
            aReqs = new[] { "IsCategoryMedical" }, aForbids = new[] { "IsInstalled", "IsHuman", "IsRobot", "IsCumbersome", "IsOversized", "IsBedMedical", "IsCushion" }, aTriggers = new[] { MedicalRules.DrawerClothTrigger } };
    }

    private static JsonInteraction Clone(string native, string name, string titleKey)
    {
        var ia = NativeDefinitions.Clone(DataHandler.dictInteractions[native]);
        ia.strName = name; ia.strTitle = Text.Get(titleKey + ".title"); ia.strDesc = Text.Get(titleKey + ".action");
        ia.strTooltip = Text.Has(titleKey + ".tooltip") ? Text.Get(titleKey + ".tooltip") : ia.strTooltip;
        ia.bOpener = false; ia.strRaiseUI = null;
        return ia;
    }

    private static void AddInteractions(NativeDefinitions d)
    {
        var controls = Clone("Inventory", MedicalRules.Controls, "Interaction.controls");
        controls.fTargetPointRange = 2;
        d.Interactions[controls.strName] = controls;
        // Lay patient here: a short action taken at the bed by whoever is dragging an unconscious person.
        var lay = Clone("Inventory", MedicalRules.Lay, "Interaction.lay");
        lay.fTargetPointRange = 2;
        d.Interactions[lay.strName] = lay;

        // Rest: walk to the bed and lie down awake, the game's own sleep opener with the bed's own marks.
        var rest = Clone("SeekSleepSimple", MedicalRules.Rest, "Interaction.rest");
        rest.aInverse = new[] { MedicalRules.RestLoop + ",[us],[them]" };
        rest.LootCondsUs = MedicalRules.RestStartLoot; rest.LootCondsThem = "CONDSleepStartThem";
        rest.CTTestUs = "TCanSleep"; rest.CTTestThem = MedicalRules.BedFreeTrigger;
        d.Interactions[rest.strName] = rest;
        // The loop: the game's own chair loop, lying down, refreshing the bed's occupied mark. First the sleep gate
        // (a sleepy rester falls asleep where they lie), then itself, then getting up.
        var loop = Clone("ACTChairSitAllow", MedicalRules.RestLoop, "Interaction.rest_loop");
        loop.strTargetPoint = null; loop.strAnim = "Sleeping"; loop.strIdleAnim = "Sleeping";
        loop.aInverse = new[] { MedicalRules.RestSleep + ",[us],[them]", MedicalRules.RestLoop + ",[us],[them]", MedicalRules.RestEnd + ",[us],[them]" };
        loop.strCancelInteraction = MedicalRules.RestCancel; loop.LootCondsUs = null; loop.LootCondsThem = "CONDSleepPerThem";
        loop.CTTestUs = MedicalRules.CanRestTrigger; loop.CTTestThem = null; loop.fWorkCancelChance = 0;
        d.Interactions[loop.strName] = loop;
        // Falling asleep: passes only when the game's own sleepiness band says so, then hands over to the game's own
        // lie-down replies (medical while the bed is powered, plain otherwise), below the free-bed test of its opener.
        var sleep = Clone("ACTChairSitEnd", MedicalRules.RestSleep, "Interaction.rest_sleep");
        sleep.strAnim = "Sleeping"; sleep.strIdleAnim = "Sleeping";
        sleep.aInverse = new[] { "SeekSleepSimpleLieDownMedical,[us],[them]", "SeekSleepSimpleLieDown,[us],[them]" };
        sleep.strCancelInteraction = MedicalRules.RestCancel; sleep.LootCondsUs = MedicalRules.RestStopLoot; sleep.LootCondsThem = null;
        sleep.CTTestUs = "THasDcSleepComfort02"; sleep.CTTestThem = null; sleep.aTickersUs = Array.Empty<string>();
        d.Interactions[sleep.strName] = sleep;
        var end = Clone("ACTChairSitEnd", MedicalRules.RestEnd, "Interaction.rest_end");
        end.strCancelInteraction = MedicalRules.RestCancel; end.LootCondsUs = MedicalRules.RestStopLoot; end.LootCondsThem = "CONDSleepEndThem";
        end.aTickersUs = Array.Empty<string>();
        d.Interactions[end.strName] = end;
        var cancel = Clone("ACTChairSitCancel", MedicalRules.RestCancel, "Interaction.rest_end");
        cancel.LootCondsUs = MedicalRules.RestStopLoot; cancel.LootCondsThem = "CONDSleepEndThem"; cancel.aTickersUs = Array.Empty<string>();
        d.Interactions[cancel.strName] = cancel;
    }

    private static void AddBed(NativeDefinitions d)
    {
        string p = MedicalRules.BedPrefix;
        var bed = Care.Station(CareSchema.Bed);
        string name = Text.Get("Bed.name");
        ApplianceDefinitions.Add(d, p, name, Text.Get("Bed.description", MedicalRules.MachineKg, bed.workingKW),
            MedicalRules.Width, MedicalRules.Depth, MedicalRules.MachineKg, Economy.Price(p), ImagePath + "PhobosMedicalBed", MedicalRules.Controls, bed.idleKW, InstallMenu.Furniture);
        ApplianceDefinitions.SetPowerOverride(d, p, bed.idleKW, bed.workingKW, MedicalRules.InUse, "PowerA");
        EquipmentInventory.Apply(d, p, InventorySpec.ServiceRack(MedicalRules.DrawerWidth, MedicalRules.DrawerHeight, MedicalRules.DrawerTrigger));
        foreach (string form in MedicalRules.Forms)
        {
            var co = d.Objects[p + form]; var item = d.Items[p + form];
            bool installed = form.StartsWith("Installed", StringComparison.Ordinal), damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            co.strNameFriendly = co.strNameShort = name + (damaged ? Text.Get("Content.damaged") : "");
            co.aStartingConds = co.aStartingConds.Where(s => !s.StartsWith("IsCategoryIndustrialProducts=", StringComparison.Ordinal))
                .Concat(new[] { "IsCategoryMedical=1x1", "IsFlexible=1x1" }).ToArray();
            if (installed && !damaged)
            {
                // The game's own bed marks; it installs switched off, as the Infirmaway does, until power arrives.
                co.aStartingConds = co.aStartingConds.Concat(new[] { "IsBedMedical=1x1", "IsCushion=1x1", "IsSheet=1x1", "IsOff=1x1" }).ToArray();
                co.aInteractions = co.aInteractions.Concat(new[] { MedicalRules.Sleep, MedicalRules.Rest, MedicalRules.Lay }).Distinct().ToArray();
            }
            // Walk-to and lie-down point in the middle of the mattress, as the Infirmaway's; power from the wall row behind its head.
            co.mapPoints = new[] { MedicalRules.UsePoint + "," + MedicalRules.PatientPointOffset, MedicalRules.SleepPoint + "," + MedicalRules.PatientPointOffset,
                "PowerA,0," + ApplianceDefinitions.WallRowY(MedicalRules.Depth) };
            // The Halewright art (Medical 0.1.1) on every form, drawn as low as the game's own beds so a patient
            // lies on top of it; damaged forms take the game's damage tint over the same image.
            item.fZScale = MedicalRules.BedZScale;
            co.strPortraitImg = item.strImg;
        }
    }
}
