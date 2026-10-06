using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Inventory;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>"Load feed by crew" on Manufacturing's charge machines and the RM-1 feeder (Manufacturing 0.56.0; owner
/// request, 6 October 2026). Crew with the Haul duty bring feed for one charge at a time into the machine's own
/// inventory, from anywhere aboard or from the store the order names (never from anyone's hands, a locked container,
/// a weapon or charger, or feed another machine of ours is waiting on), take the machine's products to the order's
/// destination store, and press Start when a whole charge waits in a paused machine. A machine that stopped for a
/// fault is never restarted by crew: the player looks first (agent default). The RM-1 is only kept stocked with a
/// couple of remainders; it grinds by itself while it has power. Framework's crew logistics does the hauling.</summary>
internal sealed class FeedCrewProvider : ICrewWorkProvider, ICrewOrderPresentation, ICrewSkipProvider, ICrewStoreEquipment
{
    private const string StartAction = "start";
    public string Id => Plugin.Id + ".feed";
    /// <summary>The charge machine an installed (intact or damaged) form belongs to, or null.</summary>
    private static ChargeMachine? Machine(CondOwner? co)
    {
        string? id = co?.strCODef;
        return ChargeMachines.For(id) is ChargeMachine m && (id == m.Spec.Installed || id == m.Spec.Installed + "Dmg") ? m : null;
    }
    private static bool Feeder(CondOwner? co) => co?.strCODef == FeederRules.Installed || co?.strCODef == FeederRules.Installed + "Dmg";
    public bool Supports(CondOwner equipment) => equipment != null && (Feeder(equipment) || Machine(equipment) != null);
    /// <summary>Loading orders carry on after a reload, like the Shipbreaker feeds (owner decision, 29 September 2026).</summary>
    public bool RoutineResume(CondOwner equipment) => true;
    public IReadOnlyList<string> Recipes(CondOwner equipment) => new[] { Feeder(equipment) ? CrewFeedRules.RemainderRecipe : CrewFeedRules.LoadRecipe };
    public string RecipeLabel(string recipe) => Text.Get(recipe == CrewFeedRules.RemainderRecipe ? "CrewFeed.recipe_remainders" : "CrewFeed.recipe");
    /// <summary>A charge machine's tray and the RM-1's inventory stay stores for everyone else: an LC-3 may still send its
    /// residue to a V4, and any machine its remainders to an RM-1.</summary>
    public bool CountsAsStore(CondOwner equipment) => true;

    /// <summary>A unit already waiting in another of our machines that would take it: never fetched from there.</summary>
    private static bool WaitingElsewhere(CondOwner unit, CondOwner machine)
    {
        var holder = unit.objCOParent;
        if (holder == null || holder == machine || !holder.HasCond("IsInstalled")) return false;
        if (Feeder(holder)) return FeederService.CanFeed(unit);
        return Machine(holder) is ChargeMachine other && other.Loadable(unit, holder);
    }
    private static string Name(CondOwner co) => ObjectPresentation.Name(co);

    public CrewWorkOffer? Next(CondOwner co, StandingOrder order, out string reason)
    {
        reason = CrewWork.Message("waiting");
        if (!Content.Ready) { reason = Content.Status; return null; }
        bool feeder = Feeder(co);
        var machine = Machine(co);
        if (!co.HasCond("IsInstalled") || co.HasCond("IsDamaged") || co.HasCond("IsLocked") || (feeder ? FeederService.Protected(co) : machine == null || machine.Protected(co)))
        { reason = Text.Get("CrewFeed.blocked", Name(co)); return null; }
        if (!Recipes(co).Contains(order.Recipe)) { reason = Text.Get(feeder ? "CrewFeed.select_remainders" : "CrewFeed.select_recipe"); return null; }
        return feeder ? NextForFeeder(co, order, out reason) : NextForCharge(co, machine!, order, out reason);
    }
    private static CrewWorkOffer? NextForCharge(CondOwner co, ChargeMachine machine, StandingOrder order, out string reason)
    {
        reason = CrewWork.Message("waiting");
        var output = CrewLogistics.Output(co, order, co, machine.CrewProduct, CrewRole.Industry);
        if (output != null) return output;
        if (machine.NeedsSelection(co)) { reason = Text.Get("CrewFeed.no_selection", Name(co)); return null; }
        var plan = machine.CrewPlan(co);
        if (plan.Ready != null)
        {
            switch (machine.State(co))
            {
                case EquipmentState.Paused: return new CrewWorkOffer(StartAction, Text.Get("CrewFeed.start", Name(co)), CrewRole.Industry, co, 10);
                case EquipmentState.Blocked: reason = Text.Get("CrewFeed.stopped", Name(co), machine.Status(co)); return null;
                default: reason = Text.Get("CrewFeed.charge_waiting", Name(co)); return null;
            }
        }
        if (plan.Short.Count == 0) { reason = Text.Get("CrewFeed.no_recipe", Name(co)); return null; }
        foreach (var shortfall in plan.Short)
            foreach (var missing in shortfall.Missing)
            {
                string id = missing.Key;
                var offer = CrewLogistics.Supply(co, order, co, u => u.strCODef == id && machine.Loadable(u, co) && !WaitingElsewhere(u, co), CrewRole.Industry);
                if (offer != null) return offer;
            }
        string closest = Text.Get("Recipe." + ChargeOutcomes.BaseOf(plan.Short[0].Recipe.Id));
        reason = Missing(co, order, u => machine.Loadable(u, co) && !WaitingElsewhere(u, co), "CrewFeed.none_aboard", "CrewFeed.none_in_store", closest);
        return null;
    }
    private static CrewWorkOffer? NextForFeeder(CondOwner co, StandingOrder order, out string reason)
    {
        reason = CrewWork.Message("waiting");
        if (!CrewFeedRules.FeederWants(OwnInventoryFeed.Units(co, FeederService.CanFeed).Count)) { reason = Text.Get("CrewFeed.feeder_stocked", Name(co)); return null; }
        if (!FeederRules.Fits(FeederService.HeldKg(co), 0.001)) { reason = Text.Get("Feeder.full", FeederRules.CapacityKg); return null; }
        Func<CondOwner, bool> accept = u => FeederService.CanFeed(u) && !WaitingElsewhere(u, co);
        var offer = CrewLogistics.Supply(co, order, co, accept, CrewRole.Industry);
        if (offer != null) return offer;
        reason = Missing(co, order, accept, "CrewFeed.feeder_none", "CrewFeed.feeder_none_in_store", "");
        return null;
    }
    /// <summary>Why nothing was brought: feed is aboard but there is no room for it, or there is none where crew may look.</summary>
    private static string Missing(CondOwner co, StandingOrder order, Func<CondOwner, bool> accept, string noneAboard, string noneInStore, string closest)
    {
        if (order.Source != StandingOrder.ShipWide) return Text.Get(noneInStore, ObjectPresentation.Name(order.Source), closest);
        return CrewLogistics.Aboard(co, co).Any(accept) ? Text.Get("CrewFeed.no_room", Name(co)) : Text.Get(noneAboard, Name(co), closest);
    }

    public bool Complete(CrewWorkContext context, CrewWorkOffer offer, out string reason)
    {
        var next = Next(context.Equipment, context.Order, out reason);
        if (next?.Action != offer.Action || offer.Action != StartAction || Machine(context.Equipment) is not ChargeMachine machine) return false;
        // The crew's Start never makes a charge of the machine's own products (steel stays the player's call).
        bool done = machine.Start(context.Equipment, ownProducts: false);
        reason = done ? CrewWork.Message("done") : machine.Status(context.Equipment);
        return done;
    }
    /// <summary>Stopping the order leaves a running charge alone: it finishes by itself.</summary>
    public void Suspend(CondOwner equipment) { }

    // --- Time-skips: the machines are stepped as they were before they took orders ---------------------------------
    public bool CanAdvance(CondOwner equipment, out string reason) { reason = Content.Status; return Content.Ready; }
    public void BeforeSkip() { }
    public void AfterSkip() { }

    // --- The Crew panel ---------------------------------------------------------------------------------------------
    public OrderFields Fields(CondOwner equipment) => Feeder(equipment) ? OrderFields.Source | OrderFields.Routine : OrderFields.Source | OrderFields.Destination | OrderFields.Routine;
    public IEnumerable<Ship> Targets(CondOwner equipment) => Array.Empty<Ship>();
    public OrderState Activity(CondOwner co, StandingOrder order)
    {
        if (co.HasCond("IsDamaged") || co.HasCond("IsLocked")) return OrderState.Blocked;
        var state = Feeder(co) ? FeederService.State(co) : Machine(co)?.State(co) ?? EquipmentState.Blocked;
        return state == EquipmentState.Blocked ? OrderState.Blocked : state == EquipmentState.Running ? OrderState.Running : OrderState.Waiting;
    }
    public bool Validate(CondOwner equipment, StandingOrder draft, out string reason) { reason = ""; return true; }
    public bool RelevantStore(CondOwner equipment, StandingOrder draft, CondOwner store, bool output)
    {
        if (output) return true;
        if (Feeder(equipment)) return CrewLogistics.Contents(store).Any(FeederService.CanFeed);
        return Machine(equipment) is ChargeMachine machine && CrewLogistics.Contents(store).Any(u => machine.Loadable(u, equipment));
    }

    /// <summary>The right-click toggle, like the game's own Toggle Power.</summary>
    internal static bool Toggle(CondOwner co, out string message)
    {
        bool feeder = co?.strCODef == FeederRules.Installed;
        bool supported = co != null && (feeder || Machine(co) is ChargeMachine m && co.strCODef == m.Spec.Installed);
        return CrewOrderToggle.Toggle(co!, supported, feeder ? CrewFeedRules.RemainderRecipe : CrewFeedRules.LoadRecipe,
            "CrewFeed.on", "CrewFeed.off", "CrewFeed.not_owned", "CrewFeed.unsupported", out message);
    }
}

/// <summary>Manufacturing's right-click crew-order switches (the L2's bottle order, the loading order): on enables the
/// order with the ship-wide source (a store already chosen in the Crew panel is kept) and resume after a reload; off is
/// a manual stop. Every refusal says why.</summary>
internal static class CrewOrderToggle
{
    internal static bool Toggle(CondOwner co, bool supported, string recipe, string onKey, string offKey, string notOwnedKey, string unsupportedKey, out string message)
    {
        if (co == null || co.bDestroyed || !supported || !co.HasCond("IsInstalled")) { message = Text.Get(unsupportedKey); return false; }
        if (!CrewWork.CanManage(co)) { message = Text.Get(notOwnedKey); return false; }
        var order = CrewWork.Order(co);
        if (order.Protected) { message = CrewWork.Message("protected"); return false; }
        if (order.Permission == WorkPermission.Enabled && order.Recipe == recipe)
        {
            CrewWork.SetPermission(co, WorkPermission.Stopped);
            message = Text.Get(offKey, co.strNameFriendly); return true;
        }
        bool configured = CrewWork.Configure(co, o => { o.Recipe = recipe; if (o.Source == "none") o.Source = StandingOrder.ShipWide; o.ResumeRoutine = true; });
        if (configured) CrewWork.SetPermission(co, WorkPermission.Enabled);
        if (!configured || CrewWork.Order(co).Permission != WorkPermission.Enabled) { message = CrewWork.Message("protected"); return false; }
        message = Text.Get(onKey, co.strNameFriendly) + "\n" + CrewWork.Status(co); return true;
    }
}
