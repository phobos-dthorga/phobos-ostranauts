using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>Queues the game's own crew jobs, exactly as the player's PDA paints them (Framework 0.78.0; owner
/// decision, 4 October 2026). Phobos crew orders work within one ship; the game's own tasks reach every ship docked
/// or moored to the worker's, so equipment that leaves work on a moored ship (the ML-2's freed panels, ore and opened
/// deposits) hands it to the crew this way. Nothing here moves an item, pays ore or decides who works: the game's
/// Haul job needs the Haul duty and a stockpile zone, its Mine job the Demolish duty and a mining tool, as always.
/// Tasks are the game's, saved by the game, and cancelled from the PDA like any painted job.</summary>
public static class NativeJobs
{
    public const string HaulDuty = "Haul", HaulInteraction = "ACTHaulItem", HaulNamePrefix = "HaulJob";
    public const string MineDuty = "Demolish", MineDepositInteraction = "ACTMineDeposit", MineNamePrefix = "MineJob";
    public const string HaulSourceTrigger = "TIsValidHaulSource", DepositCondition = "IsOreDeposit", StockpileCondition = "IsZoneStockpile";

    /// <summary>The fields of a painted job (pure): duty, interaction, target and the PDA's own task name.</summary>
    public static (string Duty, string Interaction, string Target, string Name) HaulTask(string itemId) => (HaulDuty, HaulInteraction, itemId, HaulNamePrefix + itemId);
    public static (string Duty, string Interaction, string Target, string Name) MineDepositTask(string depositId) => (MineDuty, MineDepositInteraction, depositId, MineNamePrefix + depositId);

    private static bool Add((string Duty, string Interaction, string Target, string Name) fields)
    {
        var manager = CrewSim.objInstance?.workManager;
        if (manager == null || string.IsNullOrEmpty(fields.Target)) return false;
        // The game refuses a second task for the same target and interaction, and an unknown duty or target.
        return manager.AddTask(new Task2 { strDuty = fields.Duty, strInteraction = fields.Interaction, strTargetCOID = fields.Target, strName = fields.Name });
    }

    /// <summary>Whether the game would let the PDA paint a Haul job on this item: loose on a deck (not installed, held
    /// or contained) and passing the game's own haul-source test. A missing test never passes.</summary>
    public static bool Haulable(CondOwner? item)
    {
        if (item == null || item.bDestroyed || item.ship == null || item.objCOParent != null || item.slotNow != null || item.coStackHead != null || item.HasCond("IsInstalled")) return false;
        var rule = NativeDefinitions.Trigger(HaulSourceTrigger);
        return rule != null && rule.Triggered(item);
    }
    /// <summary>Paints the game's Haul job on a loose item. False when the item is not haulable or already has one.</summary>
    public static bool Haul(CondOwner? item) => Haulable(item) && Add(HaulTask(item!.strID));

    /// <summary>Paints Haul jobs on the loose items within <paramref name="tiles"/> of a point on a ship that
    /// <paramref name="accepts"/> admits; returns how many were queued.</summary>
    public static int HaulNear(Ship? ship, Vector2 at, double tiles, Func<CondOwner, bool> accepts)
    {
        if (ship == null || accepts == null || tiles < 0) return 0;
        int queued = 0;
        foreach (var co in ship.GetCOs(null, false, false, true).ToArray())
        {
            if (co == null || co.ship != ship || !Haulable(co)) continue;
            var p = co.GetPos();
            if (Math.Abs(p.x - at.x) > tiles || Math.Abs(p.y - at.y) > tiles || !accepts(co)) continue;
            if (Add(HaulTask(co.strID))) queued++;
        }
        return queued;
    }

    /// <summary>Paints the game's Mine job on an opened ore deposit, as the PDA does: the deposit's own
    /// Mine Deposit action, under the Demolish duty.</summary>
    public static bool MineDeposit(CondOwner? deposit) =>
        deposit != null && !deposit.bDestroyed && deposit.HasCond(DepositCondition) && deposit.aInteractions != null &&
        deposit.aInteractions.Contains(MineDepositInteraction) && DataHandler.dictInteractions != null &&
        DataHandler.dictInteractions.ContainsKey(MineDepositInteraction) && Add(MineDepositTask(deposit.strID));

    /// <summary>Whether a haul (stockpile) zone exists on this ship or one attached to it: without one the game's
    /// Haul job has nowhere to bring anything.</summary>
    public static bool HasStockpile(Ship? ship)
    {
        try { return ship != null && CrewSim.coPlayer != null && ship.GetZones(StockpileCondition, CrewSim.coPlayer, true).Count > 0; }
        catch { return true; } // an unreadable zone table is not a reason to warn the player
    }
}
