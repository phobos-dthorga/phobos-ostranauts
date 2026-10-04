using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Registration;
using PhobosMedical.Core;

namespace PhobosMedical;

/// <summary>Registration, readiness and the access rule every Phobos Medical command applies.</summary>
internal static class Content
{
    internal const string ModName = "Phobos Medical";
    /// <summary>How near a crew member must stand to use the bed's panel.</summary>
    internal const int LocalAccessTiles = 3;
    internal static bool Ready { get; private set; }
    internal static void Register(Action<string> log)
    {
        Ready = false;
        try
        {
            var mod = DataHandler.dictModInfos?.Values.FirstOrDefault(m => m.strName == ModName && !m.GetIsDisabled());
            if (mod == null) throw new InvalidOperationException(Text.Get("Content.missing_package"));
            Prepare().Publish();
            Ready = true;
            log(Text.Get("Content.ready"));
        }
        catch (Exception e) { Ready = false; log(Text.Get("Content.failed")); log(e.ToString()); }
    }
    /// <summary>The whole definition set, from the care and economy packs (reloaded so a new game load sees edited files).</summary>
    internal static NativeDefinitions Prepare()
    {
        var d = new NativeDefinitions();
        Care.Load();
        Economy.Load(Content.NativeMass, id => DataHandler.dictLoot != null && DataHandler.dictLoot.ContainsKey(id));
        Definitions.Add(d);
        EquipmentEconomy.Apply(d);
        MaintenanceInformation.Register(d, "PhobosMedicalMaintenanceInformation", co => BedService.MaintenanceReason(co) ?? "");
        ItemHandling.Apply(d);
        return d;
    }
    /// <summary>A native definition's starting mass (StatMass=1xN), for the economy pack's salvage check.</summary>
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
    internal static bool Machine(CondOwner? co) => co != null && MedicalRules.IsBed(co.strCODef);
    /// <summary>Null when the acting crew member may use this bed's panel locally or through a bound console.</summary>
    internal static string? Access(CondOwner co, ConsoleBinding? binding = null)
    {
        var actor = CrewWork.Actor ?? CrewSim.GetSelectedCrew();
        if (actor == null || actor.bDestroyed || actor.HasCond("IsDead") || actor.HasCond("Unconscious") || co.bDestroyed || co.ship == null || actor.ship != co.ship) return Text.Get("Content.access");
        if (binding == null) return CrewWork.LocalAccess(actor, co, LocalAccessTiles) ? null : Text.Get("Content.access");
        return ConsoleAuthority.Check(co, binding, LocalAccessTiles, actor: actor) == ConsoleAccessFailure.None ? null : Text.Get("Content.access");
    }
}
