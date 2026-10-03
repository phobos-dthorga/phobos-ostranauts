using System;
using System.Linq;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Items;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

/// <summary>Framework's water tanks (the S3 to S5 moved from Shipbreaker in Framework 0.58.0, and the new S2) and the T2
/// thaw unit against the game's own data: passive vessel definitions with a general inventory, the thaw unit's feed
/// and power, the native ice masses the recipe depends on, the shared vessel registry, and the optional Ship's Water
/// waste contract.</summary>
internal static class SiloNativeChecks
{
    internal static void Run(NativeDefinitions d, NativeDefinitions framework, NativeDefinitions agriculture, string repo, Action<bool, string> check)
    {
        double Stat(JsonCondOwner co, string key) => EquipmentSaveUpgrade.Amount(co.aStartingConds, key);
        check(WaterTanks.All.Select(t => (t.Model, t.Footprint, t.CapacityKg, t.DryKg, t.Price)).SequenceEqual(new[]
            { ("S2", 2, 400.0, 125.0, 2950.0), ("S3", 3, 1000.0, 240.0, 4800.0), ("S4", 4, 1960.0, 365.0, 6780.0), ("S5", 5, 3330.0, 465.0, 8860.0) }),
            "The tank ladder: S2 one tile narrower than the S3 on the shared size rule, S4 and S5 as Shipbreaker sold them");
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            foreach (var tank in WaterTanks.All)
            {
                var co = framework.Objects[tank.Prefix + state]; var item = framework.Items[co.strItemDef];
                check(item.nCols == tank.Footprint && item.aSocketAdds.Length == tank.Footprint * tank.Footprint && co.inventoryWidth == tank.Footprint && co.jsonPI == null && co.aTickers.Length == 0,
                    "Each tank is an unpowered vessel of its own footprint: " + tank.Prefix + state);
                check(co.nContainerWidth == 2 && co.nContainerHeight == 2 && co.strContainerCT == "TIsFitContainerSolid" && co.aStartingConds.Any(s => s.StartsWith("IsContainer=", StringComparison.Ordinal)) &&
                    co.aSlotsWeHave.Length == 0, "Each tank has a four-cell service rack at every size and no feed: " + tank.Prefix + state);
                check(Stat(co, "StatMass") == tank.DryKg && Stat(co, "StatBasePrice") == (state.EndsWith("Dmg", StringComparison.Ordinal) ? (int)tank.Price / 4 : (int)tank.Price),
                    "Each tank begins empty at its dry mass and carries its price: " + tank.Prefix + state);
                check(co.strNameFriendly.StartsWith("Phobos' Rivetline " + tank.Model + " ", StringComparison.Ordinal), "Each tank carries its Rivetline model name: " + tank.Prefix + state);
                check(BulkVessels.SpecFor(tank.Prefix + state) is BulkVesselSpec spec && spec.CapacityKg == tank.CapacityKg && spec.Record == tank.Record && spec.Owner == WaterTanks.RecordOwner,
                    "Each tank is its own registered water vessel under the saved record names and owner: " + tank.Prefix + state);
                check(co.mapPoints.Contains("use,0," + (-8 * tank.Footprint - 8)) && framework.Installables.ContainsKey(tank.Prefix + state + "Dismantle"), "Each tank has a use point and native jobs: " + tank.Prefix + state);
                string runtime = "phobos/framework/" + tank.Art;
                check(item.strImg == runtime && item.strImgNorm == runtime + "Normal" && co.strPortraitImg == runtime && item.strImgDamaged == runtime,
                    "Every form uses the tank's overhead sprite, its normal and its portrait: " + tank.Prefix + state);
                if (state == "Installed")
                    check(DataHandler.dictCOs[tank.Installed].aInteractions.Contains(WaterTanks.Controls) && DataHandler.dictCOs[tank.Installed].aInteractions.Contains("Inventory") &&
                          PhobosAgriculture.BulkDefinitions.Work.All(w => DataHandler.dictCOs[tank.Installed].aInteractions.Contains(PhobosAgriculture.BulkDefinitions.WorkId(w))),
                        "An installed tank offers its Control Panel, Inventory and Agriculture's reservoir crew work: " + tank.Prefix);
                check(co.aInteractions.Contains(WaterTanks.Controls) == state.StartsWith("Installed", StringComparison.Ordinal),
                    "Both installed forms keep the Control Panel (a damaged silo shows its trapped water); loose forms have none: " + tank.Prefix + state);
            }
            // The silo ids stayed exactly the same when they moved: Shipbreaker no longer defines them.
            check(!d.Objects.ContainsKey(WaterTanks.BasePrefix + state), "Shipbreaker no longer defines the silos: " + state);
            var thaw = d.Objects[ThawRules.Prefix + state]; var thawItem = d.Items[thaw.strItemDef];
            check(thawItem.nCols == 2 && thawItem.aSocketAdds.Length == 4 && thaw.inventoryWidth == 2 && thaw.inventoryHeight == 2, "T2 occupies two by two native tiles: " + state);
            check(thaw.nContainerWidth * thaw.nContainerHeight == ThawRules.TrayCells && Stat(thaw, "StatMass") == ThawRules.MachineKg, "T2 tray holds two gangue cells and the unit weighs 120 kg: " + state);
            check(thaw.strNameFriendly.StartsWith("Phobos' Rivetline T2 ", StringComparison.Ordinal), "T2 carries the Rivetline T2 name: " + state);
            check(thaw.aSlotsWeHave.Contains(ThawRules.InputSlot), "T2 has its private ice feed: " + state);
            if (state.StartsWith("Installed", StringComparison.Ordinal))
                check(thaw.aInteractions.Count(i => i == IndustrialRules.LocalControls) == 1 && thaw.aInteractions.Contains("Inventory") &&
                    thaw.aInteractions.Contains(IndustrialRules.FeedOrder) == !state.EndsWith("Dmg", StringComparison.Ordinal), "Installed T2 has Control Panel, Inventory and (intact) Load feed by crew: " + state);
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
        // Refining as a business (owner approval, 1 October 2026): the game prices methane ice at 20, below the water inside
        // it; corrected in place to 100, the thaw earns about twice the block at the station water and game methane prices.
        check(Stat(methane, "StatBasePrice") == ThawRules.MethaneIcePrice && ThawRules.MethaneIcePrice == 100, "The game's methane ice is re-priced in place, the definition never republished");
        double thawed = ThawRules.ClathrateWaterKg * Phobos.Ostranauts.Framework.Items.WaterTanks.WaterPricePerKg + ThawRules.MethaneKg *
            double.Parse(DataHandler.dictLoot["GasPrices"].aCOs.Single(e => e.StartsWith("CH4=", StringComparison.Ordinal)).Split('x')[1], System.Globalization.CultureInfo.InvariantCulture);
        check(thawed >= 1.5 * ThawRules.MethaneIcePrice && thawed <= 2.5 * ThawRules.MethaneIcePrice, "Thawing a methane ice block earns 1.5 to 2.5 times the block at live prices: " + thawed);
        // Saved blocks follow the corrected price on load: an explicit old price (the game's 20 or 0.45.0's 250) is refreshed,
        // a compressed save already reads the definition, and a refreshed block is left alone.
        foreach (double old in new[] { 20.0, 250.0 })
        {
            var saved = new JsonCondOwnerSave { strCODef = ThawRules.MethaneIce, strID = "saved-ice", aConds = new[] { "StatMass=1.0x24.84", "StatBasePrice=1.0x" + old.ToString(System.Globalization.CultureInfo.InvariantCulture) } };
            var loaded = Phobos.Ostranauts.Framework.Registration.EquipmentSaveUpgrade.Upgrade(saved);
            check(Phobos.Ostranauts.Framework.Registration.EquipmentSaveUpgrade.Amount(loaded.aConds, "StatBasePrice") == ThawRules.MethaneIcePrice && loaded.strID == saved.strID &&
                  Phobos.Ostranauts.Framework.Registration.EquipmentSaveUpgrade.Amount(loaded.aConds, "StatMass") == 24.84 && saved.aConds[1].EndsWith("x" + old.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal),
                "A saved methane ice block priced " + old + " loads at the corrected price, keeping its identity and mass, without editing the save's own copy");
            check(ReferenceEquals(Phobos.Ostranauts.Framework.Registration.EquipmentSaveUpgrade.Upgrade(loaded), loaded), "The price refresh is idempotent");
        }
        var compressed = new JsonCondOwnerSave { strCODef = ThawRules.MethaneIce, strID = "compressed-ice", aConds = new[] { "DEFAULT" } };
        check(ReferenceEquals(Phobos.Ostranauts.Framework.Registration.EquipmentSaveUpgrade.Upgrade(compressed), compressed), "A compressed saved block already reads the corrected definition");
        check(Stat(gangue, "StatBasePrice") == 0, "Ice gangue stays worthless");
        var gangueItem = DataHandler.dictItemDefs[gangue.strItemDef];
        check(gangueItem.nCols == 1 && gangueItem.aSocketAdds.Length == 1, "Native gangue is a one-cell item that fits the tray");
        var power = d.Power[ThawRules.Prefix + "Power"];
        check(Math.Abs(power.fAmount - ThawRules.IdleKW / Units.SecondsPerHour) < 1e-12 && power.strOverrideCond == ProcessRules.Working &&
            Math.Abs(power.fOverrideAmount - ThawRules.WorkingKW / Units.SecondsPerHour) < 1e-12 && power.aInputPts.SequenceEqual(new[] { "PowerA" }), "T2 draws 0.1 kW idle and 6 kW thawing through one native input point");
        check(d.Objects[ThawRules.Installed].mapPoints.Contains("PowerA,0,24") && d.Objects[ThawRules.Installed].jsonPI == ThawRules.Prefix + "Power", "T2 exposes its power point, in the wall row behind it, and info");
        // The shared registry: Framework's tanks keep the silo record names; Agriculture's retired reservoirs stay registered for conversion.
        var siloSpec = BulkVessels.SpecFor(WaterTanks.BasePrefix + "Installed"); var tankSpec = BulkVessels.SpecFor(PhobosAgriculture.BulkDefinitions.Tank + "Installed");
        check(siloSpec != null && siloSpec.CapacityKg == 1000 && siloSpec.DryKg == 240 && siloSpec.Commodity == "water" && siloSpec.Record == "ShipbreakerSilo", "S3 is a registered water vessel of 1,000 kg under its saved record");
        check(tankSpec != null && tankSpec.CapacityKg == 120 && tankSpec.DryKg == 25 && tankSpec.Commodity == "water" && tankSpec.Record == "AgricultureBulk" && tankSpec.Owner != siloSpec!.Owner,
            "The retired R3 stays registered by Agriculture with the record name every saved R3 carries, for conversion");
        check(BulkVesselSpec.CapacityFromVolume(1, 1000) == WaterTanks.BaseCapacityKg, "The S3 holds one cubic metre of water at the declared density");
        // Optional Ship's Water waste contract: the trigger and stat names Ship's Water 0.16.1 uses, read here without the plugin.
        check(ShipsWaterSupply.WasteVesselTrigger == "TIsWasteVesselInstalled" && ShipsWaterSupply.WasteStat == "StatLiqH2OWaste", "The waste deposit binds the inspected Ship's Water names");
        check(ShipsWaterSupply.WasteCapacityKg(null!) == null, "Without a tank no waste capacity is known; without the plugin none is either (checked in play, not here)");
        check(typeof(ShipsWaterSupply).GetMethod("DepositWaste", new[] { typeof(CondOwner), typeof(ILiquidReservoir), typeof(double), typeof(LiquidTransferGuard) }) != null &&
              typeof(ShipsWaterSupply).GetMethod("Refill", new[] { typeof(CondOwner), typeof(ILiquidReservoir), typeof(double), typeof(double), typeof(LiquidTransferGuard) }) != null,
            "Draw and deposit share the guarded transfer contract, from the object that reaches the tanks (Framework 0.59.0)");
        // The 0.58.0 signatures stay for content built against them, marked obsolete because they ignore the link rule.
        check(new[] { typeof(ShipsWaterSupply).GetMethod("DepositWaste", new[] { typeof(Ship), typeof(ILiquidReservoir), typeof(double), typeof(LiquidTransferGuard) }),
                      typeof(ShipsWaterSupply).GetMethod("Refill", new[] { typeof(Ship), typeof(ILiquidReservoir), typeof(double), typeof(double), typeof(LiquidTransferGuard) }),
                      typeof(ShipsWaterSupply).GetMethod("Refill", new[] { typeof(Ship), typeof(ILiquidReservoir), typeof(double), typeof(double) }) }
                .All(m => m != null && m.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length == 1),
            "The earlier ship-wide adapter calls remain for older content, marked obsolete");
        // Economy: the tank bills add up to each housing (the S2, S4 and S5 derived through the ladder), and the T2's too.
        foreach (var spec in TankEconomy.Specs)
        {
            double mass = Stat(framework.Objects[spec.Prefix + "Installed"], "StatMass");
            double Bill(int[] bill) => bill.Select((n, i) => n * Stat(DataHandler.dictCOs[TankEconomy.Materials[i]], "StatMass")).Sum();
            check(Math.Abs(Bill(spec.Salvage) - mass) < 1e-9 && Math.Abs(Bill(spec.BrokenSalvage) - mass) < 1e-9, "Salvage bills conserve the tank's housing mass: " + spec.Prefix);
        }
        foreach (var spec in EquipmentEconomy.Machines.Where(m => m.Prefix == ThawRules.Prefix))
        {
            double mass = Stat(d.Objects[spec.Prefix + "Installed"], "StatMass");
            double Bill(int[] bill) => bill.Select((n, i) => n * Stat(DataHandler.dictCOs[EquipmentEconomy.Materials[i]], "StatMass")).Sum();
            check(Math.Abs(Bill(spec.Salvage) - mass) < 1e-9 && Math.Abs(Bill(spec.BrokenSalvage) - mass) < 1e-9, "Salvage bills conserve the housing mass: " + spec.Prefix);
        }
        // Dedicated artwork (assets/artwork-completion): one overhead sprite serves every form and the portrait.
        foreach (var (folder, image, size) in WaterTanks.All.Select(t => ("mods/PhobosFramework/images/phobos/framework/", t.Art, 16 * t.Footprint))
                     .Concat(new[] { ("mods/PhobosShipbreaker/images/phobos/shipbreaker/", ThawDefinitions.ThawArt, 32) }))
            foreach (string suffix in new[] { "", "Normal" })
            {
                var png = System.IO.File.ReadAllBytes(System.IO.Path.Combine(repo, folder + image + suffix + ".png"));
                int Size(int offset) => (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
                check(Size(16) == size && Size(20) == size, "World sprite uses the native footprint size: " + image + suffix);
            }
        foreach (var tank in WaterTanks.All)
            check(!System.IO.File.Exists(System.IO.Path.Combine(repo, "mods/PhobosShipbreaker/images/phobos/shipbreaker", tank.Art + ".png")), "Shipbreaker no longer ships the tank sprite: " + tank.Art);
    }
}
