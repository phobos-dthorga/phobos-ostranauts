using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The crew order "Keep suit bottles charged" on an installed L2. Crew with the Haul duty bring loose suit
/// O2 bottles that are not yet charged from anywhere aboard (the deck, unlocked containers and other machines' trays,
/// as the game's own Reload job searches; never from someone's hands or suit, a locked container or another L2's rack)
/// into the rack, and press Start when a bottle there needs filling. Charged bottles go to the order's destination
/// store when one is chosen, otherwise they wait in the rack. Framework's crew logistics does the hauling.</summary>
internal sealed class FillerCrewProvider : ICrewWorkProvider, ICrewOrderPresentation
{
    public string Id => Plugin.Id;
    public bool Supports(CondOwner equipment) => equipment != null && equipment.strCODef == FillerRules.Installed;
    public bool RoutineResume(CondOwner equipment) => true;
    public IReadOnlyList<string> Recipes(CondOwner equipment) => new[] { FillerRules.BottleRecipe };
    public string RecipeLabel(string recipe) => Text.Get("Filler.crew_recipe");

    private static double Fill(CondOwner bottle) => NativeGasVessel.TryRead(bottle, out var r) ? r.FillFraction : 1;
    private static bool Uncharged(CondOwner c) => NativeGasVessel.IsBottle(c) && !c.HasCond("IsDamaged") && !FillerRules.Charged(Fill(c)) &&
        !FillerRules.IsFamily(c.objCOParent?.strCODef);
    private static bool ChargedBottle(CondOwner c) => NativeGasVessel.IsBottle(c) && FillerRules.Charged(Fill(c));

    public CrewWorkOffer? Next(CondOwner co, StandingOrder order, out string reason)
    {
        reason = CrewWork.Message("waiting");
        if (!Content.Ready || FillerService.Protected(co) || !co.HasCond("IsInstalled") || co.HasCond("IsDamaged") || co.HasCond("IsLocked"))
        { reason = Text.Get("Filler.crew_blocked"); return null; }
        if (order.Recipe != FillerRules.BottleRecipe) { reason = Text.Get("Filler.crew_select_recipe"); return null; }
        if (FillerService.StateOf(co).Mode == FillerMode.Decant) { reason = Text.Get("Filler.crew_decant"); return null; }
        var output = CrewLogistics.Output(co, order, co, ChargedBottle, CrewRole.Industry);
        if (output != null) return output;
        var rack = FillerService.Rack(co).ToArray();
        if (rack.Length < FillerRules.RackCells)
        {
            var supply = CrewLogistics.Supply(co, order, co, Uncharged, CrewRole.Industry);
            if (supply != null) return supply;
        }
        if (rack.Any(b => !FillerRules.Charged(Fill(b))) && !co.HasCond(ManufacturingRules.Filling) && FillerService.State(co) == Phobos.Ostranauts.Framework.Controls.EquipmentState.Paused)
            return new CrewWorkOffer("start", Text.Get("Filler.crew_start"), CrewRole.Industry, co, 10);
        reason = Text.Get(rack.Length == 0 ? "Filler.crew_no_bottles" : "Filler.crew_charged");
        return null;
    }
    public bool Complete(CrewWorkContext context, CrewWorkOffer offer, out string reason)
    {
        var next = Next(context.Equipment, context.Order, out reason);
        if (next?.Action != offer.Action) return false;
        bool done = offer.Action == "start" && FillerService.Start(context.Equipment);
        reason = done ? CrewWork.Message("done") : Text.Get("Filler.crew_blocked");
        return done;
    }
    /// <summary>Stopping the order leaves a running fill alone: the station stops by itself at 99%.</summary>
    public void Suspend(CondOwner equipment) { }

    // --- The Crew panel ------------------------------------------------------------------------------------------
    public OrderFields Fields(CondOwner equipment) => OrderFields.Source | OrderFields.Destination | OrderFields.Routine;
    public IEnumerable<Ship> Targets(CondOwner equipment) => Array.Empty<Ship>();
    public OrderState Activity(CondOwner co, StandingOrder order) =>
        FillerService.Protected(co) || co.HasCond("IsDamaged") || co.HasCond("IsLocked") ? OrderState.Blocked :
        co.HasCond(ManufacturingRules.Filling) ? OrderState.Running : OrderState.Waiting;
    public bool Validate(CondOwner equipment, StandingOrder draft, out string reason) { reason = ""; return true; }
    public bool RelevantStore(CondOwner equipment, StandingOrder draft, CondOwner store, bool output) =>
        output || CrewLogistics.Contents(store).Any(Uncharged);

    /// <summary>The right-click toggle, like the game's own Toggle Power: on enables the order with the ship-wide
    /// source (a store already chosen in the Crew panel is kept); off is a manual stop.</summary>
    internal static bool Toggle(CondOwner co, out string message)
    {
        if (!co.HasCond("IsInstalled") || co.bDestroyed || co.strCODef != FillerRules.Installed) { message = Text.Get("Filler.crew_blocked"); return false; }
        if (!CrewWork.CanManage(co)) { message = Text.Get("Filler.crew_not_owned"); return false; }
        var order = CrewWork.Order(co);
        if (order.Protected) { message = CrewWork.Message("protected"); return false; }
        if (order.Permission == WorkPermission.Enabled && order.Recipe == FillerRules.BottleRecipe)
        {
            CrewWork.SetPermission(co, WorkPermission.Stopped);
            message = Text.Get("Filler.crew_off", co.strNameFriendly); return true;
        }
        bool configured = CrewWork.Configure(co, o => { o.Recipe = FillerRules.BottleRecipe; if (o.Source == "none") o.Source = StandingOrder.ShipWide; o.ResumeRoutine = true; });
        if (configured) CrewWork.SetPermission(co, WorkPermission.Enabled);
        if (!configured || CrewWork.Order(co).Permission != WorkPermission.Enabled) { message = CrewWork.Message("protected"); return false; }
        message = Text.Get("Filler.crew_on", co.strNameFriendly) + "\n" + CrewWork.Status(co); return true;
    }
}
