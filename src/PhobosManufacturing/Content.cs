using System;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Construction;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Registration;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>Registration, readiness and the access rule every Manufacturing command applies.</summary>
internal static class Content
{
    internal const string ModName = "Phobos Manufacturing";
    internal static bool Ready { get; private set; }
    internal static string Status { get; private set; } = "";
    internal static void Register(Action<string> log)
    {
        Ready = false; Status = Text.Get("Content.loading");
        try
        {
            ShipbreakerStock.Resolve();
            AgricultureStock.Resolve();
            var mod = DataHandler.dictModInfos?.Values.FirstOrDefault(m => m.strName == ModName && !m.GetIsDisabled());
            if (mod == null) throw new InvalidOperationException(Text.Get("Content.missing_package"));
            var prepared = Prepare(ShipbreakerStock.Available);
            prepared.Publish();
            Ready = true; Status = Text.Get(ShipbreakerStock.Available ? "Content.ready_steel" : "Content.ready");
            log(Status);
        }
        catch (Exception e) { Ready = false; Status = Text.Get("Content.failed"); log(e.ToString()); }
    }
    /// <summary>The whole definition set. The definitions are the same with or without the optional providers: the
    /// steel and makeup recipes are gated at run time by the providers' detection, not by what is registered. The flag
    /// names the installation an offline check describes.</summary>
    internal static NativeDefinitions Prepare(bool steelStock)
    {
        var d = new NativeDefinitions();
        Vessels.Load();
        Materials.Load();
        Equipment.Load();
        RefineryRecipes.Load(NativeMass);
        Economy.Load(NativeMass, id => DataHandler.dictLoot != null && DataHandler.dictLoot.ContainsKey(id));
        Definitions.Add(d);
        EquipmentEconomy.Apply(d);
        RegionalEconomy.Apply(d);
        MiningLoot.Add(d);
        MaintenanceInformation.Register(d, "PhobosManufacturingMaintenanceInformation", co => MaintenanceReason(co, true) ?? MaintenanceReason(co, false) ?? "");
        ItemHandling.Apply(d);
        return d;
    }
    /// <summary>A native definition's starting mass, as the game writes it (StatMass=1xN), for the economy pack's salvage check.</summary>
    internal static double? NativeMass(string id)
    {
        if (DataHandler.dictCOs == null || !DataHandler.dictCOs.TryGetValue(id, out var co)) return null;
        foreach (string cond in co.aStartingConds ?? Array.Empty<string>())
        {
            if (!cond.StartsWith("StatMass=", StringComparison.Ordinal)) continue;
            string amount = cond.Substring(cond.IndexOf('x') + 1);
            return double.TryParse(amount, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double kg) ? kg : null;
        }
        return null;
    }
    internal static CondOwner? Resolve(string? id) => CrewWork.Resolve(id);
    internal static bool Machine(CondOwner? co) => co != null && MachineKinds.IsOurs(co.strCODef);
    /// <summary>Anything whose removal work Manufacturing may refuse: its machines and stores, and the acid line's segments.</summary>
    internal static bool Maintained(CondOwner? co) => Machine(co) || co != null && LiquidLines.ForDefinition(co.strCODef) != null;
    /// <summary>Null when the acting crew member may command this machine locally or through the bound console.</summary>
    internal static string? Access(CondOwner co, ConsoleBinding? binding = null, CondOwner? worker = null)
    {
        var actor = worker ?? CrewWork.Actor ?? CrewSim.GetSelectedCrew();
        if (actor == null || actor.bDestroyed || actor.HasCond("IsDead") || actor.HasCond("Unconscious") || co.bDestroyed || co.ship == null || actor.ship != co.ship) return Text.Get("Content.access");
        if (binding == null) return CrewWork.LocalAccess(actor, co, ManufacturingRules.LocalAccessTiles) ? null : Text.Get("Content.access");
        return ConsoleAuthority.Check(co, binding, ManufacturingRules.ConsoleAccessTiles, actor: actor) == ConsoleAccessFailure.None ? null : Text.Get("Content.access");
    }
    /// <summary>Why removal work is refused when it is offered (never by blocking native destruction).</summary>
    internal static string? MaintenanceReason(CondOwner? co, bool dismantle)
    {
        if (co == null) return null;
        if (ChargeMachines.For(co.strCODef) is ChargeMachine charge) return charge.MaintenanceReason(co);
        if (ProcessorRules.IsFamily(co.strCODef)) return ProcessorService.MaintenanceReason(co);
        if (SabatierRules.IsFamily(co.strCODef)) return SabatierService.MaintenanceReason(co);
        if (CrackerRules.IsFamily(co.strCODef)) return CrackerService.MaintenanceReason(co);
        if (GasStores.IsFamily(co.strCODef)) return StoreService.MaintenanceReason(co, dismantle);
        if (LiquidStores.IsFamily(co.strCODef)) return LiquidStoreService.MaintenanceReason(co);
        if (ManifoldRules.IsFamily(co.strCODef)) return ManifoldService.MaintenanceReason(co);
        if (FillerRules.IsFamily(co.strCODef)) return FillerService.MaintenanceReason(co);
        if (RegulatorRules.IsFamily(co.strCODef)) return RegulatorService.MaintenanceReason(co);
        if (BottlerRules.IsFamily(co.strCODef)) return BottlerService.MaintenanceReason(co);
        if (FeederRules.IsFamily(co.strCODef)) return FeederService.MaintenanceReason(co, dismantle);
        return null;
    }
}
