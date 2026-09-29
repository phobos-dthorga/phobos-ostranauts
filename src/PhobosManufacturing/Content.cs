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
            var mod = DataHandler.dictModInfos?.Values.FirstOrDefault(m => m.strName == ModName && !m.GetIsDisabled());
            if (mod == null) throw new InvalidOperationException(Text.Get("Content.missing_package"));
            var prepared = Prepare(ShipbreakerStock.Available);
            prepared.Publish();
            Ready = true; Status = Text.Get(ShipbreakerStock.Available ? "Content.ready_steel" : "Content.ready");
            log(Status);
        }
        catch (Exception e) { Ready = false; Status = Text.Get("Content.failed"); log(e.ToString()); }
    }
    /// <summary>The whole definition set. The steel recipe's feed admission depends on the flag, so offline checks
    /// exercise both installations without touching plugin state.</summary>
    internal static NativeDefinitions Prepare(bool steelStock)
    {
        var d = new NativeDefinitions();
        Definitions.Add(d, steelStock);
        EquipmentEconomy.Apply(d);
        RegionalEconomy.Apply(d);
        MiningLoot.Add(d);
        MaintenanceInformation.Register(d, "PhobosManufacturingMaintenanceInformation", co => MaintenanceReason(co, true) ?? MaintenanceReason(co, false) ?? "");
        ItemHandling.Apply(d);
        return d;
    }
    internal static CondOwner? Resolve(string? id) => CrewWork.Resolve(id);
    internal static bool Machine(CondOwner? co) => co != null && (RefineryRules.IsFamily(co.strCODef) || ProcessorRules.IsFamily(co.strCODef) || HydrogenRules.IsFamily(co.strCODef));
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
        if (RefineryRules.IsFamily(co.strCODef)) return RefineryService.MaintenanceReason(co);
        if (ProcessorRules.IsFamily(co.strCODef)) return ProcessorService.MaintenanceReason(co);
        if (HydrogenRules.IsFamily(co.strCODef)) return HydrogenService.MaintenanceReason(co, dismantle);
        return null;
    }
}
