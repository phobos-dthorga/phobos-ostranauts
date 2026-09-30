using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Hazards;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The gas stores (hydrogen, methane, oxygen, nitrogen and carbon dioxide, every size): passive Framework
/// bulk vessels with a Leak damage policy. Commands are status, owner-confirmed acceptance, an explicit vent
/// overboard and a transfer into another store of the same gas. Their hazard: a damaged store leaks (hydrogen to
/// space, the others into the room as the game's own gas) until repaired; a fuel with oxygen in the room and an
/// ignition source burns through the game's own explosion machinery, consuming the room's oxygen, heating its air
/// and, for methane, adding the carbon dioxide the burn makes. A destroyed store of a game gas releases what it
/// held into the room.</summary>
internal static class StoreService
{
    private sealed class Session { internal double LastTick; }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    internal static void Reset() => sessions = new();
    /// <summary>Station Bulk supplies: oxygen, nitrogen and carbon dioxide into any size of their stores, and (Manufacturing
    /// 0.19.0) sulfuric acid into any size of acid tank, at the game's own price per kilogram (its GasPrices table, as
    /// the refuelling kiosk charges; the game prices H2SO4 there). Nothing sells back.</summary>
    internal const double GasPurchaseStepKg = 10;
    internal static readonly Phobos.Ostranauts.Framework.Trading.VesselSupplyProvider Supplies = new(Plugin.Id, () => Content.Ready ? GasOffers() :
        Array.Empty<(Phobos.Ostranauts.Framework.Trading.BulkSupplyOffer, IReadOnlyList<string>)>());
    private static IEnumerable<(Phobos.Ostranauts.Framework.Trading.BulkSupplyOffer Offer, IReadOnlyList<string> Families)> GasOffers()
    {
        foreach (var family in new[] { GasStores.OxygenFamily, GasStores.NitrogenFamily, GasStores.CarbonDioxideFamily })
        {
            double price = NativeGasVessel.PricePerKg(family.Species);
            if (!(price > 0)) continue;
            int steps = (int)Math.Ceiling(family.Sizes.Max(s => s.CapacityKg) / GasPurchaseStepKg);
            yield return (new Phobos.Ostranauts.Framework.Trading.BulkSupplyOffer("manufacturing." + family.Species.ToLowerInvariant(), Text.Get(family.TextPrefix + ".offer"),
                Text.Get("Store.unit_kg"), price, GasPurchaseStepKg, steps), family.Sizes.Select(s => s.Prefix).ToArray());
        }
        foreach (var family in LiquidStores.Families)
        {
            double price = NativeGasVessel.PricePerKg(family.MistSpecies);
            if (!(price > 0)) continue;
            int steps = (int)Math.Ceiling(family.Sizes.Max(s => s.CapacityKg) / GasPurchaseStepKg);
            yield return (new Phobos.Ostranauts.Framework.Trading.BulkSupplyOffer("manufacturing." + family.MistSpecies.ToLowerInvariant(), Text.Get(family.TextPrefix + ".offer"),
                Text.Get("Store.unit_kg"), price, GasPurchaseStepKg, steps), family.Sizes.Select(s => s.Prefix).ToArray());
        }
    }
    /// <summary>Vent amounts in readable steps (1, 2 or 5 times a power of ten) near 4%, 20% and 40% of capacity, and all of it.</summary>
    internal static double[] VentChoices(GasStore fuel) => new[] { .04, .2, .4 }.Select(f => Nice(fuel.CapacityKg * f)).Append(fuel.CapacityKg).Distinct().OrderBy(x => x).ToArray();
    internal static double Nice(double kg)
    {
        double power = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(kg, 1e-9))));
        return new[] { 1, 2, 5, 10 }.Select(m => m * power).OrderBy(x => Math.Abs(Math.Log(x / kg))).First();
    }
    private static GasStore Fuel(CondOwner co) => GasStores.For(co.strCODef) ?? throw new InvalidOperationException("Not a fuel store: " + co.strCODef);
    private static string T(GasStore fuel, string key, params object[] args) => Text.Get(fuel.TextPrefix + "." + key, args);

    internal static string Describe(CondOwner co)
    {
        var fuel = Fuel(co);
        if (BulkVessel.Protected(co)) return Text.Get("Store.protected");
        var s = BulkVessel.Snapshot(co);
        string names = string.Join(", ", Linked(co, c => LinksTo(c, co.strID)).Select(ObjectPresentation.Name));
        string text = T(fuel, "level", s.ServiceKg, s.CapacityKg) + "\n" + Text.Get("Store.linked", names.Length == 0 ? ConsoleText.Get("not_selected") : names);
        return text + (co.HasCond("IsDamaged") ? "\n" + T(fuel, "leaking", fuel.LeakKgPerHour) : "");
    }
    /// <summary>Whether a Manufacturing machine is linked to the store with this ID, in any role.</summary>
    private static bool LinksTo(CondOwner c, string storeId)
    {
        if (ProcessorRules.IsFamily(c.strCODef)) return ProcessorService.StorePeer(c) == storeId || ProcessorService.CanisterId(c) == storeId;
        if (SabatierRules.IsFamily(c.strCODef)) return SabatierService.HydrogenPeer(c) == storeId || SabatierService.MethanePeer(c) == storeId || SabatierService.CanisterId(c) == storeId;
        if (CrackerRules.IsFamily(c.strCODef)) return CrackerService.AmmoniaPeer(c) == storeId || CrackerService.NitrogenPeer(c) == storeId || CrackerService.HydrogenPeer(c) == storeId;
        if (ManifoldRules.IsFamily(c.strCODef)) return ManifoldService.Sources(c).Any(x => x.Id == storeId);
        if (FillerRules.IsFamily(c.strCODef)) return FillerService.StateOf(c).Links.Any(x => x.Id == storeId);
        if (ChargeMachines.For(c.strCODef) is ChargeMachine charge) return charge.LinksTo(c, storeId);
        return false;
    }
    /// <summary>Stores of the same gas this one can pour into: within one tile, or along a gas line between the two line ports.</summary>
    internal static IEnumerable<CondOwner> TransferCandidates(CondOwner co)
    {
        var fuel = GasStores.For(co.strCODef);
        if (fuel == null || co.ship == null) return Enumerable.Empty<CondOwner>();
        return BulkVessels.Aboard(co.ship, fuel.Commodity).Where(c => c != co && GasStores.IsFamily(c.strCODef) && GasLine.Connection(co, ManifoldRules.StoreOutlet, c) != null).ToArray();
    }
    /// <summary>Pours everything that fits from one store into another of the same gas, under both stores' guards.</summary>
    private static bool Transfer(CondOwner co, GasStore fuel, string targetId, out string message)
    {
        var target = TransferCandidates(co).FirstOrDefault(c => c.strID == targetId);
        if (target == null) { message = Text.Get("Store.transfer_missing"); return false; }
        if (BulkVessel.Protected(target) || CommodityReservations.Held(target.strID) || CommodityReservations.Held(co.strID)) { message = Text.Get("Store.transfer_protected"); return false; }
        BufferedDrains.Settle(co); BufferedDrains.Settle(target);
        double kg = Math.Min(BulkVessel.Snapshot(co).AvailableKg, BulkVessel.Snapshot(target).HeadroomKg);
        if (kg <= 1e-9) { message = Text.Get("Store.transfer_nothing"); return false; }
        LiquidTransferGuard.Commit(new BulkVessel.Endpoint(co), new BulkVessel.Endpoint(target), kg, BulkVessel.Guard(co), BulkVessel.Guard(target));
        message = T(fuel, "transferred", kg, target.strNameFriendly);
        Plugin.Log(co.strID + ": " + message);
        return true;
    }
    // The console asks every store and tank aboard for its links in one refresh: the machines aboard are listed once per step.
    private static readonly StepMemo<Ship, CondOwner[]> machinesAboard = new();
    internal static IEnumerable<CondOwner> Linked(CondOwner store, Func<CondOwner, bool> match)
    {
        if (store.ship == null) return Enumerable.Empty<CondOwner>();
        var machines = machinesAboard.GetOrAdd(NativeSteps.Frame, store.ship, ship => ship.GetCOs(null, false, false, true)
            .Where(c => c != null && !c.bDestroyed && MachineKinds.Classify(c.strCODef) != MachineKind.None || c != null && ManifoldRules.IsFamily(c.strCODef)).ToArray());
        return machines.Where(c => !c.bDestroyed && match(c)).OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    }
    internal static EquipmentState State(CondOwner co) => BulkVessel.Protected(co) || co.HasCond("IsDamaged") || co.HasCond("IsLocked") ? EquipmentState.Blocked : EquipmentState.Ready;
    internal static string? MaintenanceReason(CondOwner co, bool dismantle)
    {
        var fuel = GasStores.For(co.strCODef);
        if (fuel == null) return null;
        if (BulkVessel.Protected(co)) return Text.Get("Maintenance.protected");
        return dismantle && BulkVessel.Snapshot(co).ServiceKg > 1e-8 ? T(fuel, "maintenance") : null;
    }
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        var fuel = Fuel(co);
        message = Text.Get("Store.protected");
        if (!Content.Ready) { message = Content.Status; return false; }
        message = Content.Access(co, binding) ?? "";
        if (message.Length > 0) return false;
        if (action == "accept") { bool ok = BulkVessel.Accept(co, Plugin.Log); message = Text.Get(ok ? "Store.accept_done" : "Store.accept_unavailable"); return ok; }
        if (action == "pause") { message = Text.Get("Store.nothing_to_pause"); return true; }
        if (action == "status") { message = Describe(co); return true; }
        if (BulkVessel.Protected(co)) { message = Text.Get("Store.protected"); return false; }
        if (action.StartsWith("transfer:", StringComparison.Ordinal))
        {
            try { return Transfer(co, fuel, action.Substring(9), out message); }
            catch (Exception e) { Plugin.Log(e.ToString()); message = Text.Get("Store.protected"); return false; }
        }
        if (!action.StartsWith("vent:", StringComparison.Ordinal)) { message = Text.Get("Content.unsupported_action"); return false; }
        if (!double.TryParse(action.Substring(5), NumberStyles.Float, CultureInfo.InvariantCulture, out double kg) || !ManufacturingRules.Finite(kg) || kg <= 0) { message = Text.Get("Store.invalid_amount"); return false; }
        try
        {
            double vented = BulkVessel.Drain(co, kg, Text.Get("Store.vent_reason"));
            message = vented > 0 ? T(fuel, "vented", vented) : Text.Get("Store.nothing_vented");
            if (vented > 0) PlayerNotices.Post(co.ship, "PhobosManufacturing.vent", NoticeLevel.Info, T(fuel, "vent_log", co.strNameFriendly, vented));
            return vented > 0;
        }
        catch (Exception e) { Plugin.Log(e.ToString()); message = Text.Get("Store.protected"); return false; }
    }

    /// <summary>After the native damage switch (contents stayed in service under the Leak policy): burn now if the
    /// room allows it, otherwise the leak begins.</summary>
    internal static void Damaged(CondOwner co)
    {
        if (co == null || co.bDestroyed || !co.HasCond("IsDamaged") || !co.HasCond("IsInstalled")) return;
        try
        {
            var fuel = Fuel(co);
            if (!Release(co, fuel, "damaged_log"))
                PlayerNotices.Post(co.ship, "PhobosManufacturing.leak", NoticeLevel.Caution, T(fuel, "leak_notice", co.strNameFriendly, fuel.LeakKgPerHour), T(fuel, "leak_banner"));
        }
        catch (Exception e) { Plugin.Log(e.ToString()); }
    }
    /// <summary>Native destruction proceeds; contents still aboard face the same release rule first, and a game gas
    /// that did not burn goes into the room rather than vanishing.</summary>
    internal static void Destroying(CondOwner co)
    {
        if (co == null || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || co.HasCond("IsModeSwitching", false)) return;
        try
        {
            var fuel = Fuel(co);
            if (Release(co, fuel, "destroyed_log") || fuel.LeakSpecies == null || BulkVessel.Protected(co)) return;
            var air = RoomHeat.Read(co);
            double held = BulkVessel.Snapshot(co).ServiceKg;
            if (air == null || held <= 1e-8) return;
            double released = BulkVessel.Drain(co, held, T(fuel, "leak_reason"));
            if (released > 0) { RoomGas.Emit(air, fuel.LeakSpecies, released); PlayerNotices.Post(co.ship, "PhobosManufacturing.release", NoticeLevel.Caution, T(fuel, "released_log", co.strNameFriendly, released)); }
        }
        catch (Exception e) { Plugin.Log(e.ToString()); }
    }
    /// <summary>Every couple of seconds while damaged: the leak advances (to space, or into the room as a native
    /// species), and any new ignition source lights what remains.</summary>
    internal static void Tick(CondOwner co)
    {
        if (co == null || co.bDestroyed || !co.HasCond("IsDamaged") || !co.HasCond("IsInstalled") || co.ship == null || (int)co.ship.LoadState < 2 || BulkVessel.Protected(co)) return;
        var s = sessions.GetValue(co, _ => new Session { LastTick = StarSystem.fEpoch });
        double now = StarSystem.fEpoch, hours = (now - s.LastTick) / Units.SecondsPerHour; s.LastTick = now;
        if (hours <= 0 || hours > 1) return;
        try
        {
            var fuel = Fuel(co);
            if (Release(co, fuel, "damaged_log")) return;
            double leak = fuel.Spec.LeakKg(hours, BulkVessel.Snapshot(co).ServiceKg);
            if (leak <= 0) return;
            var air = fuel.LeakSpecies == null ? null : RoomHeat.Read(co);
            // Hydrogen always leaks to space; methane into the room, or to space where there is no air.
            double drained = BulkVessel.Drain(co, leak, T(fuel, fuel.LeakSpecies != null && air == null ? "leak_reason_space" : "leak_reason"));
            if (air != null && drained > 0) RoomGas.Emit(air, fuel.LeakSpecies!, drained);
        }
        catch (Exception e) { Plugin.Log(e.ToString()); }
    }
    /// <summary>Burns the whole content when the room has oxygen and an ignition source; returns whether it did.</summary>
    private static bool Release(CondOwner co, GasStore fuel, string logKey)
    {
        double held = BulkVessel.Snapshot(co).ServiceKg;
        if (!fuel.IsFuel || held <= 1e-8 || BulkVessel.Protected(co) || !Ignites(co, out var air)) return false;
        var burn = fuel.Burn(held, RoomGas.HeldKg(air!, "O2"));
        if (burn.BurnedKg <= 0) return false;
        // The whole content leaves the record first; what did not burn is lost with the blast.
        if (BulkVessel.Drain(co, held, Text.Get("Store.deflagration_reason")) <= 0) return false;
        Blast(co, air!, burn);
        string log = T(fuel, logKey, co.strNameFriendly, burn.BurnedKg, burn.OxygenKg, burn.EnergyKJ / 1000, burn.LostKg);
        Plugin.Log(log);
        PlayerNotices.Post(co.ship, "PhobosManufacturing.deflagration", NoticeLevel.Caution, log, T(fuel, "deflagration_banner"));
        return true;
    }
    /// <summary>Fuel already out of its vessel (a damaged reactor's hydrogen): burns when the room allows it and
    /// returns whether it did; otherwise the caller lets it escape.</summary>
    internal static bool Ignite(CondOwner source, GasStore fuel, double kg, string logKey)
    {
        if (kg <= 1e-9 || !Ignites(source, out var air)) return false;
        var burn = fuel.Burn(kg, RoomGas.HeldKg(air!, "O2"));
        if (burn.BurnedKg <= 0) return false;
        Blast(source, air!, burn);
        Plugin.Log(Text.Get(logKey, source.strNameFriendly, burn.BurnedKg, burn.OxygenKg, burn.EnergyKJ / 1000, burn.LostKg));
        return true;
    }
    private static bool Ignites(CondOwner co, out RoomHeat.Air? air)
    {
        air = RoomHeat.Read(co);
        return air != null && GasStores.Fate(air.Room.GetCondAmount("StatGasPpO2"), FireInRoom(co, air), HearthWorking(co, air), SparkingDevice(co, air)) == HydrogenFate.Deflagrate;
    }
    /// <summary>The room loses the burned oxygen and gains the burn's native products; heat goes into its air up to
    /// the industrial ceiling and the rest is carried by the game's own explosion.</summary>
    private static void Blast(CondOwner co, RoomHeat.Air air, Deflagration burn)
    {
        RoomGas.Consume(air, "O2", burn.OxygenKg);
        foreach (var product in burn.RoomProductsKg) if (product.Value > 0) RoomGas.Emit(air, product.Key, product.Value);
        double roomRise = Math.Max(0, 333.15 - (air.Kelvin + air.PendingKelvin));
        double roomKWh = Math.Min(burn.EnergyKJ / 3600, roomRise * air.Mols * RoomHeat.GasHeatCapacityJPerMolK / RoomHeat.JoulesPerKilowattHour);
        if (roomKWh > 0) RoomHeat.Deposit(air, roomKWh);
        NativeExplosions.Spawn(co.ship, co.tf.position, GasStores.DeflagrationDefinition(burn));
    }
    private static bool SameRoom(CondOwner c, RoomHeat.Air air) => c.ship?.GetRoomAtWorldCoords1(c.GetPos(), false)?.CO == air.Room;
    private static IEnumerable<CondOwner> Nearby(CondOwner co) => co.ship?.GetCOs(null, false, false, true).Where(c => c != null && !c.bDestroyed && c != co) ?? Enumerable.Empty<CondOwner>();
    private static bool FireInRoom(CondOwner co, RoomHeat.Air air) => Nearby(co).Any(c => c.HasCond("IsFire") && !c.HasCond("IsExtinguished") && SameRoom(c, air));
    private static bool HearthWorking(CondOwner co, RoomHeat.Air air) => Nearby(co).Any(c => ChargeMachines.For(c.strCODef) is ChargeMachine charge && charge.Igniting(c) && SameRoom(c, air));
    /// <summary>The game's own spark rule: a powered device at half its damage or more throws sparks.</summary>
    private static bool SparkingDevice(CondOwner co, RoomHeat.Air air) => Nearby(co).Any(c => c.HasCond("IsPowered") && c.GetCondAmount("StatDamageMax") > 0 &&
        c.GetCondAmount("StatDamage") >= .5 * c.GetCondAmount("StatDamageMax") && SameRoom(c, air));
}
