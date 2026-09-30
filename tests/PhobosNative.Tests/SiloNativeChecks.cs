using System;
using System.Linq;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

/// <summary>The S3 silo and T2 thaw unit against the game's own data: passive vessel definitions, the thaw
/// unit's feed and power, the native ice masses the recipe depends on, the shared vessel registry across
/// Shipbreaker and Agriculture, and the optional Ship's Water waste contract.</summary>
internal static class SiloNativeChecks
{
    internal static void Run(NativeDefinitions d, NativeDefinitions agriculture, string repo, Action<bool, string> check)
    {
        double Stat(JsonCondOwner co, string key) => EquipmentSaveUpgrade.Amount(co.aStartingConds, key);
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            var silo = d.Objects[SiloRules.Prefix + state]; var siloItem = d.Items[silo.strItemDef];
            check(siloItem.nCols == 3 && siloItem.aSocketAdds.Length == 9 && silo.inventoryWidth == 3 && silo.inventoryHeight == 3, "S3 occupies three by three native tiles: " + state);
            check(silo.jsonPI == null && silo.aTickers.Length == 0 && silo.nContainerWidth == 0 && silo.aSlotsWeHave.Length == 0 && !silo.aStartingConds.Any(s => s.StartsWith("IsContainer=", StringComparison.Ordinal)),
                "S3 is a passive vessel: no electricity, no container, no feed: " + state);
            check(Stat(silo, "StatMass") == SiloRules.DryKg, "S3 begins empty at its dry mass: " + state);
            check(Stat(silo, "StatBasePrice") == (state.EndsWith("Dmg", StringComparison.Ordinal) ? SiloRules.Price / 4 : SiloRules.Price), "S3 authored prices: " + state);
            check(silo.strNameFriendly.StartsWith("Phobos' Rivetline S3 ", StringComparison.Ordinal), "S3 carries the Rivetline S3 name: " + state);
            foreach (var size in SiloRules.Sizes.Skip(1))
            {
                var big = d.Objects[size.Prefix + state]; var bigItem = d.Items[big.strItemDef];
                check(bigItem.nCols == size.Footprint && bigItem.aSocketAdds.Length == size.Footprint * size.Footprint && big.inventoryWidth == size.Footprint && big.jsonPI == null &&
                    big.nContainerWidth == 0 && Stat(big, "StatMass") == size.DryKg, "Each larger silo is a passive vessel of its own footprint at its dry mass: " + size.Prefix + state);
                check(big.strNameFriendly.StartsWith("Phobos' Rivetline S" + size.Footprint + " ", StringComparison.Ordinal) &&
                    Stat(big, "StatBasePrice") == (state.EndsWith("Dmg", StringComparison.Ordinal) ? (int)size.Price / 4 : (int)size.Price), "Each larger silo carries its Rivetline model and price: " + size.Prefix + state);
                check(BulkVessels.SpecFor(size.Prefix + state)?.CapacityKg == size.CapacityKg && BulkVessels.SpecFor(size.Prefix + state)?.Record == size.Record,
                    "Each larger silo is its own registered water vessel: " + size.Prefix + state);
                check(big.mapPoints.Contains("use,0," + (-8 * size.Footprint - 8)) && d.Installables.ContainsKey(size.Prefix + state + "Dismantle"), "Each larger silo has a use point and native jobs: " + size.Prefix + state);
            }
            var thaw = d.Objects[ThawRules.Prefix + state]; var thawItem = d.Items[thaw.strItemDef];
            check(thawItem.nCols == 2 && thawItem.aSocketAdds.Length == 4 && thaw.inventoryWidth == 2 && thaw.inventoryHeight == 2, "T2 occupies two by two native tiles: " + state);
            check(thaw.nContainerWidth * thaw.nContainerHeight == ThawRules.TrayCells && Stat(thaw, "StatMass") == ThawRules.MachineKg, "T2 tray holds two gangue cells and the unit weighs 120 kg: " + state);
            check(thaw.strNameFriendly.StartsWith("Phobos' Rivetline T2 ", StringComparison.Ordinal), "T2 carries the Rivetline T2 name: " + state);
            check(thaw.aSlotsWeHave.Contains(ThawRules.InputSlot), "T2 has its private ice feed: " + state);
            if (state.StartsWith("Installed", StringComparison.Ordinal))
            {
                check(silo.aInteractions.Count(i => i == IndustrialRules.LocalControls) == 1 && !silo.aInteractions.Contains(IndustrialRules.FeedOrder), "Installed S3 has one Control Panel and no loading order: " + state);
                check(thaw.aInteractions.Count(i => i == IndustrialRules.LocalControls) == 1 && thaw.aInteractions.Contains("Inventory") &&
                    thaw.aInteractions.Contains(IndustrialRules.FeedOrder) == !state.EndsWith("Dmg", StringComparison.Ordinal), "Installed T2 has Control Panel, Inventory and (intact) Load feed by crew: " + state);
            }
        }
        var feed = d.Objects[ThawRules.InputBin]; var trigger = DataHandler.dictCTs[feed.strContainerCT];
        check(feed.nContainerWidth * feed.nContainerHeight == ThawRules.FeedCapacity && d.Slots[ThawRules.InputSlot].bHide, "Ice feed holds two blocks as a hidden slot with its own window");
        check(feed.strNameFriendly.StartsWith("Phobos' Rivetline T2 ", StringComparison.Ordinal), "The ice feed is branded with its machine");
        var ice = DataHandler.dictCOs[ThawRules.Ice]; var gangue = DataHandler.dictCOs[ThawRules.Gangue]; var methane = DataHandler.dictCOs["ItmIce02"];
        check(Stat(ice, "StatMass") == ThawRules.IceKg && Stat(gangue, "StatMass") == ThawRules.GangueKg, "The recipe uses the game's own ice and gangue masses (24.7 kg, 2.0 kg)");
        check(ice.nStackLimit > 1, "Water ice stacks natively, so the feed bin refuses merging");
        check(trigger.TriggeredDataCO(new DataCO(ice), false), "The ice feed admits water ice at the game level");
        check(trigger.TriggeredDataCO(new DataCO(methane), false) && new DataCO(methane).HasCond("IsIce"), "Methane ice passes the game-level IsIce rule into the feed");
        foreach (string outside in new[] { ThawRules.Gangue, "ItmScrapSteel", ProcessRules.Wall, "ItmCanisterLH02Loose" })
            check(trigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[outside]), false) == new DataCO(DataHandler.dictCOs[outside]).HasCond("IsIce"), "Only ice enters the thaw feed at the game level: " + outside);
        check(ThawRules.ValidIce(methane.strName, Stat(methane, "StatMass"), true, true, true) && Stat(methane, "StatMass") == ThawRules.MethaneIceKg,
            "Methane ice is accepted at the game's own 24.84 kg");
        // Refining value (owner, 30 September 2026): the game's own methane ice price stands; the thaw gains value from a
        // mined feed and five kilowatt-hours, and neither product has a sell route, so there is no trade loop to guard.
        check(Stat(methane, "StatBasePrice") == 20, "The game's methane ice price is left as the game set it (the 0.45.0 correction is withdrawn)");
        check(Stat(gangue, "StatBasePrice") == 0, "Ice gangue stays worthless");
        var gangueItem = DataHandler.dictItemDefs[gangue.strItemDef];
        check(gangueItem.nCols == 1 && gangueItem.aSocketAdds.Length == 1, "Native gangue is a one-cell item that fits the tray");
        var power = d.Power[ThawRules.Prefix + "Power"];
        check(Math.Abs(power.fAmount - ThawRules.IdleKW / Units.SecondsPerHour) < 1e-12 && power.strOverrideCond == ProcessRules.Working &&
            Math.Abs(power.fOverrideAmount - ThawRules.WorkingKW / Units.SecondsPerHour) < 1e-12 && power.aInputPts.SequenceEqual(new[] { "PowerA" }), "T2 draws 0.1 kW idle and 6 kW thawing through one native input point");
        check(d.Objects[ThawRules.Installed].mapPoints.Contains("PowerA,0,8") && d.Objects[ThawRules.Installed].jsonPI == ThawRules.Prefix + "Power", "T2 exposes its power point and info");
        // The shared registry: both mods declare their vessels through Framework, with distinct records.
        var siloSpec = BulkVessels.SpecFor(SiloRules.Installed); var tankSpec = BulkVessels.SpecFor(PhobosAgriculture.BulkDefinitions.Tank + "Installed");
        check(siloSpec != null && siloSpec.CapacityKg == 1000 && siloSpec.DryKg == 240 && siloSpec.Commodity == "water" && siloSpec.Record == SiloRules.Record, "S3 is a registered water vessel of 1,000 kg");
        check(tankSpec != null && tankSpec.CapacityKg == 120 && tankSpec.DryKg == 25 && tankSpec.Commodity == "water" && tankSpec.Record == "AgricultureBulk" && tankSpec.Owner != siloSpec!.Owner,
            "The R3 reservoir is registered by Agriculture with the record name every saved R3 already carries");
        check(BulkVesselSpec.CapacityFromVolume(1, 1000) == SiloRules.CapacityKg, "The silo holds one cubic metre of water at the declared density");
        // Optional Ship's Water waste contract: the trigger and stat names Ship's Water 0.16.1 uses, read here without the plugin.
        check(ShipsWaterSupply.WasteVesselTrigger == "TIsWasteVesselInstalled" && ShipsWaterSupply.WasteStat == "StatLiqH2OWaste", "The waste deposit binds the inspected Ship's Water names");
        check(ShipsWaterSupply.WasteCapacityKg(null!) == null, "Without a tank no waste capacity is known; without the plugin none is either (checked in play, not here)");
        check(typeof(ShipsWaterSupply).GetMethod("DepositWaste") != null && typeof(ShipsWaterSupply).GetMethod("Refill", new[] { typeof(Ship), typeof(ILiquidReservoir), typeof(double), typeof(double), typeof(LiquidTransferGuard) }) != null,
            "Draw and deposit share the guarded transfer contract");
        // Economy: dismantling either machine loses value, checked by the shared economy audit; here the bills add up.
        foreach (var spec in EquipmentEconomy.Machines.Where(m => SiloRules.IsFamily(m.Prefix + "Installed") || m.Prefix == ThawRules.Prefix))
        {
            double mass = Stat(d.Objects[spec.Prefix + "Installed"], "StatMass");
            double Bill(int[] bill) => bill.Select((n, i) => n * Stat(DataHandler.dictCOs[EquipmentEconomy.Materials[i]], "StatMass")).Sum();
            check(Math.Abs(Bill(spec.Salvage) - mass) < 1e-9 && Math.Abs(Bill(spec.BrokenSalvage) - mass) < 1e-9, "Salvage bills conserve the housing mass: " + spec.Prefix);
        }
        // Dedicated artwork (assets/artwork-completion): one overhead sprite serves every form and the portrait;
        // CompletionArtworkChecks verifies the installed forms' bindings, sizes and hashes against the manifest.
        foreach (var (prefix, image, size) in new[] { (SiloRules.Prefix, SiloDefinitions.SiloArt, 48), (ThawRules.Prefix, SiloDefinitions.ThawArt, 32) })
        {
            string runtime = "phobos/shipbreaker/" + image;
            foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
            {
                var co = d.Objects[prefix + state]; var item = d.Items[co.strItemDef];
                check(item.strImg == runtime && item.strImgNorm == runtime + "Normal" && co.strPortraitImg == runtime && item.strImgDamaged == runtime,
                    "Every form uses the dedicated overhead sprite, its normal and its portrait: " + prefix + state);
            }
            foreach (string suffix in new[] { "", "Normal" })
            {
                var png = System.IO.File.ReadAllBytes(System.IO.Path.Combine(repo, "mods/PhobosShipbreaker/images", runtime + suffix + ".png"));
                int Size(int offset) => (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
                check(Size(16) == size && Size(20) == size, "World sprite uses the native footprint size: " + image + suffix);
            }
            check(!System.IO.File.Exists(System.IO.Path.Combine(repo, "mods/PhobosShipbreaker/images", runtime + "Portrait.png")), "The retired placeholder portrait is gone: " + image);
        }
    }
}
