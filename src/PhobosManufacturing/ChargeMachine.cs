using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ostranauts.Inventory;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The shared charge machine (Manufacturing 0.17.0, lifted from the V4's service): one bound charge at a time
/// from a feed bin, progress in powered seconds on the machine's own record, measured electricity with a declared share
/// (plus any reaction heat) into the room's air, the recipe's off-gas breathed into that air as it progresses, and a
/// settlement at the end that delivers solids to the tray and draws and deposits commodities in linked vessels in one
/// fixed order (Framework <see cref="CommoditySettlement"/>). A reload drops the session; Start resumes the retained
/// charge. A melt left waiting for a cool room longer than its own duration freezes and is lost to slag. One engine
/// instance per machine family, configured by a <see cref="ChargeMachineSpec"/>.</summary>
internal sealed class ChargeMachine
{
    private sealed class Session
    {
        internal ChargeState State = null!;
        internal bool Protected, AwaitingFeed, HeatWait, VesselWait, NeedsAttention;
        // True only while Start itself binds: the one time a charge may be made of things this machine makes.
        internal bool ByStart;
        internal double Last, NextVesselCheck, WaitSince;
        // How often an armed, idle machine looks in its own inventory for a charge (real time).
        internal readonly Cadence OwnFeed = new(ManufacturingRules.VesselRecheckSeconds);
        /// <summary>When the next finished unit may go to the product store (real time).</summary>
        internal double NextDelivery;
        internal string Status = "";
        internal string? LastStop;
    }
    internal sealed class Transfer
    {
        internal RoomHeat.Air Air = null!;
        internal double RequestedKWh, WorkSeconds, HeatFraction;
        internal EnergyReceipt Receipt = null!;
    }
    internal ChargeMachineSpec Spec { get; }
    private ConditionalWeakTable<CondOwner, Session> sessions = new();
    private readonly ConditionalWeakTable<Powered, Transfer> pending = new();
    private readonly Dictionary<string, string> workingText = new(StringComparer.Ordinal);
    private IReadOnlyList<ChargeLinkSpec>? links; private object? linksFrom;
    internal ChargeMachine(ChargeMachineSpec spec) { Spec = spec ?? throw new ArgumentNullException(nameof(spec)); }
    internal void Reset() { sessions = new(); links = null; workingText.Clear(); feedView = null; feedGates = Array.Empty<string>(); feedGateAnswers = Array.Empty<bool>(); feedKg.Clear(); }
    private string T(string key, params object[] args) => Text.Get(Spec.Text(key), args);
    private ChargeRecipeView Catalog => ChargeCatalog.For(Spec.MachineKey);
    internal IReadOnlyList<ChargeLinkSpec> Links
    {
        get
        {
            var from = ChargeCatalog.Pack;
            if (links == null || !ReferenceEquals(linksFrom, from)) { links = Spec.Links(); linksFrom = from; }
            return links;
        }
    }
    internal bool IsFamily(string? id) => Phobos.Ostranauts.Framework.Registration.EquipmentIdentity.IsFamily(id, Spec.Prefix);

    private ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, Spec.Record, Plugin.Id, 1);
    private Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co, out var found)) return found;
        bool explicitSelection = Spec.Selection == RecipeSelection.Explicit;
        var s = new Session { State = explicitSelection ? new ChargeState(true) : new RefineryState(), Status = T("paused") };
        var status = Store(co).Read(out var fields);
        if (status == SavedStateStatus.Ready)
        {
            try { s.State = explicitSelection ? ChargeState.Read(fields, true) : RefineryState.Read(fields); }
            catch (Exception e) { s.Protected = true; Plugin.Log(e.ToString()); }
        }
        else if (status != SavedStateStatus.Missing) s.Protected = true;
        s.State.Running = false;
        if (s.Protected) s.Status = T("protected");
        else if (s.State.Bound) s.Status = T("retained", s.State.RecipeId);
        sessions.Add(co, s);
        return s;
    }
    private void Save(CondOwner co, Session s) { if (!Store(co).TryWrite(s.State.Save())) { s.Protected = true; s.Status = T("protected"); } }

    // ---- Feed ----
    internal CondOwner? Feed(CondOwner co) => co.compSlots?.GetCOs(Spec.InputSlot, true, null)?.FirstOrDefault(c => c != null && c.strCODef == Spec.InputBin);
    private ChargeRecipe? SelectedRecipe(CondOwner? machine)
    {
        if (Spec.Selection != RecipeSelection.Explicit || machine == null) return null;
        var s = Get(machine);
        return s.State.Bound ? Recipe(s) : Catalog.ByRevision(s.State.Selected);
    }
    /// <summary>The unit mass an identity must carry to enter this machine's feed (for an explicit machine, only the
    /// selected recipe's feed), or null.</summary>
    private double? FeedKg(string? id, CondOwner? machine)
    {
        if (Spec.Selection == RecipeSelection.Explicit)
        {
            var selected = SelectedRecipe(machine);
            return selected == null ? null : Catalog.FeedKg(id, MetFor(machine), selected);
        }
        return AutomaticFeedKg(id, Preferred(machine));
    }
    /// <summary>The optional recipe this machine has been told to take, or empty (Manufacturing 0.51.0).</summary>
    private string Preferred(CondOwner? machine) => machine == null || machine.bDestroyed || !IsFamily(machine.strCODef) ? "" : Get(machine).State.Prefer;
    private Func<string, bool> MetFor(CondOwner? machine) => ChargeCatalog.MetWith(Spec.Met, Preferred(machine));
    // What Catalog.FeedKg(id, Spec.Met) answers, remembered (Manufacturing 0.31.0): a running machine asks it for every
    // bound unit on every power step, and the feed hook for every item offered to the bin. The table is rebuilt when
    // the catalog view changes or any requirement gate answers differently, so it never outlives what it was built from.
    private ChargeRecipeView? feedView; private string[] feedGates = Array.Empty<string>(); private bool[] feedGateAnswers = Array.Empty<bool>();
    // One table per machine preference (0.51.0): machines with different optional recipes admit different feed.
    private readonly Dictionary<string, Dictionary<string, double>> feedKg = new(StringComparer.Ordinal);
    private double? AutomaticFeedKg(string? id, string preferred)
    {
        if (id == null) return null;
        var view = Catalog;
        bool current = ReferenceEquals(feedView, view);
        for (int i = 0; current && i < feedGates.Length; i++) current = Spec.Met(feedGates[i]) == feedGateAnswers[i];
        if (!current)
        {
            feedView = view;
            // Only the owner's gates can change under a table; an optional recipe's gate is the table's own key.
            feedGates = view.All.SelectMany(r => r.Requires).Where(k => ChargeCatalog.ChosenId(k) == null).Distinct(StringComparer.Ordinal).ToArray();
            feedGateAnswers = feedGates.Select(Spec.Met).ToArray();
            feedKg.Clear();
        }
        if (!feedKg.TryGetValue(preferred, out var table))
        {
            feedKg[preferred] = table = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var recipe in view.Available(ChargeCatalog.MetWith(Spec.Met, preferred)))
                foreach (var input in recipe.ItemInputs)
                    if (!table.ContainsKey(input.Id)) table[input.Id] = input.Kg;
        }
        return table.TryGetValue(id, out double kg) ? kg : (double?)null;
    }
    private CondOwner? MachineOf(CondOwner? bin) => bin?.objCOParent;
    // The solids this machine's recipes make, rebuilt when the catalog view changes.
    private ChargeRecipeView? productView; private readonly HashSet<string> productIds = new(StringComparer.Ordinal);
    private bool OwnProduct(string? id)
    {
        var view = Catalog;
        if (!ReferenceEquals(productView, view))
        {
            productView = view; productIds.Clear();
            foreach (var recipe in view.All) foreach (var product in recipe.Products) if (!ChargeCommodities.Is(product.Id)) productIds.Add(product.Id);
        }
        return id != null && productIds.Contains(id);
    }
    /// <summary>Feed lying in the machine's own inventory (Manufacturing 0.41.0). A machine that repeats by itself never
    /// takes what it makes (a V4 would carburise its own ingots, or burn the carbon stock it just made); a charge of
    /// those is taken only by Start, once each time it is chosen.</summary>
    private List<CondOwner> OwnFeed(CondOwner co, bool byStart)
    {
        Func<CondOwner, bool> accept = u => CrewFeed(u, co) && (byStart || !OwnProduct(u.strCODef));
        var units = OwnInventoryFeed.Units(co, accept);
        // The optional feed store (Manufacturing 0.42.0): a bin or other store touching the machine or on a belt to it.
        units.AddRange(StoreFeed.Units(co, accept));
        return units;
    }
    internal bool ValidFeed(CondOwner? input, CondOwner? machine) => input != null && !input.bDestroyed && input.Crew == null &&
        !input.HasCond("IsInstalled") && input.GetCOsSafe(true).Count == 0 && input.GetLotCOs(true).Count == 0 && input.coStackHead == null && input.aStack.Count == 0 &&
        FeedKg(input.strCODef, machine) is double kg && ProcessMaterial.MassMatches(input.GetTotalMass(), kg);
    internal bool CrewFeed(CondOwner input, CondOwner? machine) => CrewLogistics.Loose(input) && FeedKg(input.strCODef, machine) is double kg && ProcessMaterial.MassMatches(input.GetCondAmount("StatMass"), kg);
    internal bool CanFeed(CondOwner bin, CondOwner input)
    {
        var machine = MachineOf(bin);
        return !bin.HasCond("IsLocked") && (CrewLogistics.IsUnitPreflight(input) ? CrewFeed(input, machine) : ValidFeed(input, machine)) &&
            bin.objContainer != null && (bin.objContainer.ContainedCOs.Contains(input) || bin.objContainer.ContainedCOs.Count < Equipment.Entry(Spec.Prefix).feedCells);
    }

    // ---- Links ----
    // Links run through Framework's shared VesselLink (Manufacturing 0.23.0): the vessel touches the machine or shares
    // the commodity's line with it, and the vessel-side port is a bank several machines share.
    internal string Peer(CondOwner co, ChargeLinkSpec link) => link.Vessel.PeerId(co);
    internal IEnumerable<CondOwner> Candidates(CondOwner co, ChargeLinkSpec link) => link.Vessel.Candidates(co, link.Accepts);
    private ChargeLinkSpec? LinkFor(string commodity) => Links.FirstOrDefault(l => l.Commodity == commodity);
    /// <summary>Whether every commodity the recipe draws has a linked vessel on this machine (an automatic machine only
    /// chooses such a recipe; whether the vessel holds enough is the settlement check's business).</summary>
    private bool Drawable(CondOwner co, ChargeRecipe recipe) => recipe.Draws.All(d => LinkFor(d.Id) is ChargeLinkSpec link && Peer(co, link).Length > 0);
    /// <summary>The linked vessel when it can settle this commodity's need now; otherwise null with the reason.</summary>
    private CondOwner? Endpoint(CondOwner co, ChargeLinkSpec link, SettlementNeed need, out string reason)
    {
        reason = link.Reason(LinkProblem.None, 0, 0);
        var vessel = CrewWork.Resolve(Peer(co, link));
        if (vessel == null || BulkVessels.Of(vessel)?.Commodity != link.Commodity || !link.Accepts(vessel)) return null;
        reason = link.Reason(LinkProblem.NotReady, 0, 0);
        if (!link.Vessel.Connected(co, vessel)) return null;
        var snapshot = BulkVessel.Snapshot(vessel);
        switch (SettlementPlan.Check(need, snapshot, CommodityReservations.Held(vessel.strID)))
        {
            case SettlementRefusal.Protected: reason = link.Reason(LinkProblem.Protected, 0, 0); return null;
            case SettlementRefusal.Busy: reason = link.Reason(LinkProblem.Busy, 0, 0); return null;
            case SettlementRefusal.Catch: reason = link.Reason(LinkProblem.Catch, snapshot.CatchKg, 0); return null;
            case SettlementRefusal.Short: reason = link.Reason(LinkProblem.Short, snapshot.AvailableKg, need.NeedAvailableKg); return null;
            case SettlementRefusal.Full: reason = link.Reason(LinkProblem.Full, snapshot.HeadroomKg, need.NeedHeadroomKg); return null;
        }
        reason = ""; return vessel;
    }
    /// <summary>A charge's commodity legs: draws, deposits of the products it will release, and circulating volumes.</summary>
    private static IReadOnlyList<SettlementNeed> Needs(ChargeRecipe recipe, IEnumerable<ProductSpec> products) => SettlementPlan.Build(
        recipe.Draws.Select(i => new SettlementLeg(i.Id, SettlementRole.Draw, i.Kg * i.Count))
            .Concat(products.Where(p => ChargeCommodities.Is(p.Id)).Select(p => new SettlementLeg(p.Id, SettlementRole.Deposit, p.Kg * p.Count)))
            .Concat(recipe.Circulates.Select(c => new SettlementLeg(c.Key, SettlementRole.Circulate, c.Value))));
    /// <summary>Every vessel the needs touch, ready now; null with the first reason otherwise.</summary>
    private List<(SettlementNeed Need, CondOwner Vessel)>? ReadyVessels(CondOwner co, IReadOnlyList<SettlementNeed> needs, out string reason)
    {
        reason = "";
        var ready = new List<(SettlementNeed, CondOwner)>();
        foreach (var need in needs)
        {
            var link = LinkFor(need.Commodity) ?? throw new InvalidOperationException("No " + need.Commodity + " link on " + Spec.Prefix);
            var vessel = Endpoint(co, link, need, out reason);
            if (vessel == null) return null;
            ready.Add((need, vessel));
        }
        return ready;
    }

    internal string? MachineProblem(CondOwner co)
    {
        if (!Content.Ready) return Content.Status;
        if (co == null || co.bDestroyed || co.strCODef != Spec.Installed || !co.HasCond("IsInstalled")) return T("install_first");
        if (co.HasCond("IsDamaged")) return T("repair_first");
        if (co.ship == null || (int)co.ship.LoadState < 2) return Text.Get("Content.ship_not_loaded");
        var feed = Feed(co);
        if (co.HasCond("IsLocked") || feed?.HasCond("IsLocked") == true || co.objContainer?.Locked == true || feed?.objContainer?.Locked == true) return T("unlock");
        if (co.HasCond("IsOverrideOff") || co.HasCond("IsSignalOff")) return T("switched_off");
        if (co.objContainer == null || feed?.objContainer == null) return T("missing_feed");
        return null;
    }
    // The working line names the recipe; it is formatted once per recipe and language, not on every power step.
    private string WorkingStatus(ChargeRecipe recipe)
    {
        string key = Phobos.Ostranauts.Framework.Localization.Translations.Language + "|" + recipe.Id;
        if (!workingText.TryGetValue(key, out var text)) workingText[key] = text = T("working", RecipeName(recipe));
        return text;
    }
    // Written only when it changes: an idle machine reaches this on every power step.
    private void SetWorking(CondOwner co, bool value) { if (co.HasCond(Spec.WorkingCondition) != value) co.SetCondAmount(Spec.WorkingCondition, value ? 1 : 0); }
    private ChargeRecipe? Recipe(Session s) => s.State.Bound ? Catalog.ByRevision(s.State.Revision) : null;
    /// <summary>The name a player sees: an outcome shows as the recipe it was chosen as, so a result stays unknown
    /// until the charge finishes.</summary>
    private static string RecipeName(ChargeRecipe recipe) => Text.Get("Recipe." + ChargeOutcomes.BaseOf(recipe.Id));
    private bool Available(ChargeRecipe recipe, CondOwner? machine) => recipe.Requires.All(MetFor(machine));
    /// <summary>The bound units, when every one of them still sits in the feed bin and is still valid feed.</summary>
    private List<CondOwner>? Charge(CondOwner co, Session s)
    {
        var bin = Feed(co)?.objContainer?.ContainedCOs;
        if (bin == null || !s.State.Bound) return null;
        var items = new List<CondOwner>();
        foreach (string id in s.State.Charge)
        {
            CondOwner? item = null;
            foreach (var c in bin) if (c.strID == id) { item = c; break; }
            if (item == null || !ValidFeed(item, co)) return null;
            items.Add(item);
        }
        return items;
    }

    internal bool Start(CondOwner co, ConsoleBinding? binding = null)
    {
        var s = Get(co);
        string? problem = Content.Access(co, binding) ?? MachineProblem(co);
        if (problem != null) { s.Status = problem; return false; }
        if (s.Protected) { s.Status = T("protected"); return false; }
        try
        {
            s.LastStop = null; s.NeedsAttention = false; s.AwaitingFeed = true;
            if (s.State.Running) return true;
            bool started;
            s.ByStart = true;
            try { started = Resume(co, s) || Bind(co, s); }
            finally { s.ByStart = false; }
            if (!started && !s.VesselWait) s.AwaitingFeed = false;
            return started || s.VesselWait;
        }
        catch (Exception ex) { Fault(co, ex); return false; }
    }
    /// <summary>A retained charge resumes exactly; one whose recipe this installation lacks stays retained.</summary>
    private bool Resume(CondOwner co, Session s)
    {
        if (!s.State.Bound) return false;
        var recipe = Recipe(s);
        if (recipe == null || !Available(recipe, co)) { s.Status = T("retained_unavailable", s.State.RecipeId); return false; }
        if (Charge(co, s) == null) { s.Status = T("charge_changed"); return false; }
        return Run(co, s, recipe);
    }
    /// <summary>Binds the largest exact charge present in the bin, or the selected recipe's charge.</summary>
    private bool Bind(CondOwner co, Session s)
    {
        SetWorking(co, false); s.VesselWait = false;
        var feed = Feed(co); var bin = feed?.objContainer?.ContainedCOs;
        if (feed == null || bin == null) { s.Status = T("feed_empty"); return false; }
        // The game shows one inventory for the machine, so a charge put in by hand lies beside the products
        // (Manufacturing 0.41.0). The charge is chosen from the feed and that inventory together, feed first, and only
        // the units of the chosen charge are then taken into the feed.
        var own = OwnFeed(co, s.ByStart);
        if (bin.Count == 0 && own.Count == 0) { s.Status = T("feed_empty"); return false; }
        var valid = bin.Where(c => ValidFeed(c, co)).Concat(own).ToList();
        ChargeRecipe? recipe;
        if (Spec.Selection == RecipeSelection.Explicit)
        {
            recipe = Catalog.ByRevision(s.State.Selected);
            if (recipe == null || !Available(recipe, co)) { s.Status = T("no_selection"); return false; }
            var counts = valid.GroupBy(c => c.strCODef, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            if (!recipe.ItemInputs.All(i => counts.TryGetValue(i.Id, out int n) && n >= i.Count)) { s.Status = T(valid.Count == 0 ? "invalid_feed" : "no_charge"); return false; }
        }
        else
        {
            recipe = Catalog.Match(valid.Select(c => c.strCODef), MetFor(co), r => Drawable(co, r));
            if (recipe == null) { s.Status = T(valid.Count == 0 ? "invalid_feed" : "no_charge"); return false; }
        }
        var units = new List<CondOwner>();
        foreach (var input in recipe.ItemInputs) units.AddRange(valid.Where(c => c.strCODef == input.Id).OrderBy(c => c.objCOParent == feed ? 0 : 1).ThenBy(c => c.strID, StringComparer.Ordinal).Take(input.Count));
        if (units.Any(u => !ChargeState.SafeId(u.strID))) { s.Status = T("invalid_feed"); return false; }
        foreach (var unit in units)
            if (unit.objCOParent != feed && !OwnInventoryFeed.Take(unit, feed)) { s.Status = Text.Get("Content.feed_blocked"); return false; }
        // A charge with an outcome table (Manufacturing 0.44.0) turns out to be one of its outcomes, decided by the
        // bound units themselves: the same units always give the same result, so nothing can reroll it.
        recipe = ChargeOutcomes.Resolve(recipe, units.Select(u => u.strID));
        s.State.RecipeId = recipe.Id; s.State.Revision = recipe.Revision; s.State.ProgressSeconds = 0; s.State.WaitSeconds = 0; s.State.EmittedKg = 0;
        s.State.Charge = units.Select(u => u.strID).ToList();
        Save(co, s);
        if (s.Protected) return false;
        return Run(co, s, recipe);
    }
    private void WaitForVessel(Session s, string why)
    {
        s.VesselWait = true; s.NextVesselCheck = Cadence.RealTime + ManufacturingRules.VesselRecheckSeconds; s.Status = T("waiting_vessel", why);
    }
    private bool Run(CondOwner co, Session s, ChargeRecipe recipe)
    {
        // Nothing heats until every vessel the charge will touch can take part: stored gases are never vented, and a
        // reagent the charge draws must be there.
        if (ReadyVessels(co, Needs(recipe, recipe.Products), out string why) == null) { WaitForVessel(s, why); return false; }
        s.State.Running = true; s.Last = StarSystem.fEpoch; s.VesselWait = false;
        s.Status = T("working", RecipeName(recipe));
        if (s.State.ProgressSeconds >= recipe.Seconds) return Finish(co, s);
        SetWorking(co, true);
        return true;
    }
    internal bool Pause(CondOwner co, bool cancel, ConsoleBinding? binding = null)
    {
        var s = Get(co);
        string? problem = Content.Access(co, binding);
        if (problem != null) { s.Status = problem; return false; }
        s.NeedsAttention = false;
        CrewWork.ManualStop(co);
        Stop(co, s, T(cancel ? "cancelled" : "paused_retained"), needsAttention: false);
        if (!cancel || s.Protected) return !s.Protected;
        // Cancelling releases the bound units unchanged; the charge's work is forfeited, its mass is not. They go back
        // into the machine's inventory, where a hand can reach them, as far as it has room.
        s.State.Clear(); Save(co, s);
        if (!s.Protected) OwnInventoryFeed.Return(Feed(co), co);
        return true;
    }
    private void Stop(CondOwner co, Session s, string message, bool needsAttention = true)
    {
        s.LastStop = message; s.NeedsAttention = needsAttention; s.AwaitingFeed = false; s.VesselWait = false; s.HeatWait = false;
        s.State.Running = false; s.Status = message; SetWorking(co, false);
    }
    internal void Block(CondOwner co, string reason) => Stop(co, Get(co), reason);
    internal void Fault(CondOwner co, Exception ex)
    {
        var s = Get(co);
        Stop(co, s, T("fault")); s.NeedsAttention = true;
        Plugin.Log(ex.ToString());
    }

    /// <summary>The step before the game's power call. A machine that was started when the game was saved is armed again
    /// once after the load (Manufacturing 0.47.0; owner decision, 5 October 2026): a retained charge resumes exactly, an
    /// armed machine goes back to waiting for feed, and a machine whose checks fail stays stopped with the reason. The
    /// saved mark follows whether the player's Start still stands.</summary>
    internal void BeforePower(CondOwner co)
    {
        if (Phobos.Ostranauts.Framework.Persistence.ResumeAfterLoad.Due(co))
        {
            var resumed = Get(co);
            if (!resumed.State.Running && !resumed.AwaitingFeed && !resumed.VesselWait)
            {
                string? problem = MachineProblem(co);
                if (problem != null || resumed.Protected) Stop(co, resumed, problem ?? T("protected"));
                else { resumed.LastStop = null; resumed.NeedsAttention = false; resumed.AwaitingFeed = true; resumed.Status = Text.Get("Content.resumed"); }
            }
        }
        Step(co);
        if (!sessions.TryGetValue(co, out var s)) return;
        bool started = s.State.Running || s.AwaitingFeed || s.VesselWait;
        Phobos.Ostranauts.Framework.Persistence.ResumeAfterLoad.Sync(co, started);
        // The optional product store (Manufacturing 0.49.0): a started machine sends what it has made there, a unit at a
        // time, so its tray does not stop it. Only its own products go; the bound charge is in the feed, out of reach.
        if (started && Cadence.RealTime >= s.NextDelivery)
        {
            s.NextDelivery = Cadence.RealTime + ManufacturingRules.DeliverySeconds;
            try { StoreDelivery.SendOne(co, u => OwnProduct(u.strCODef)); }
            catch (Exception ex) { Fault(co, ex); }
        }
    }
    private void Step(CondOwner co)
    {
        if (!sessions.TryGetValue(co, out var s)) { SetWorking(co, false); return; }
        // Rechecks follow real time: a game-time interval would shrink to every frame at fast-forward.
        if (s.VesselWait && Cadence.RealTime >= s.NextVesselCheck)
        {
            s.NextVesselCheck = Cadence.RealTime + ManufacturingRules.VesselRecheckSeconds;
            var fault = MachineProblem(co);
            if (fault != null) { Stop(co, s, fault); return; }
            var recipe = Recipe(s);
            if (recipe != null && s.State.ProgressSeconds >= recipe.Seconds) Finish(co, s);
            else if (s.AwaitingFeed) { if (!(Resume(co, s) || Bind(co, s)) && !s.VesselWait) s.AwaitingFeed = false; }
        }
        else if (s.AwaitingFeed && !s.State.Running && !s.VesselWait)
        {
            var problem = MachineProblem(co);
            if (problem != null) { Stop(co, s, problem); return; }
            // An empty feed keeps the machine armed; its own inventory is looked at every couple of real seconds.
            if (Feed(co)?.objContainer?.ContainedCOs.Count == 0 && !s.State.Bound &&
                (!s.OwnFeed.Due() || OwnFeed(co, false).Count == 0)) { s.Status = T("feed_empty"); return; }
            if (!(Resume(co, s) || Bind(co, s)) && !s.VesselWait) s.AwaitingFeed = false;
        }
        if (!s.State.Running) { if (!s.VesselWait) SetWorking(co, false); return; }
        string? machineProblem = MachineProblem(co);
        if (machineProblem != null || Charge(co, s) == null) { Stop(co, s, machineProblem ?? T("charge_changed")); return; }
        var current = Recipe(s);
        SetWorking(co, current != null && s.State.ProgressSeconds < current.Seconds);
    }
    /// <summary>Reaction heat released per working hour (0 for a machine without one, or an absorbing reaction).</summary>
    private static double ReactionKW(ChargeRecipe? recipe) => recipe == null || recipe.ReactionKWh <= 0 ? 0 : recipe.ReactionKWh * Units.SecondsPerHour / recipe.Seconds;
    internal bool BeginPower(Powered power, CondOwner co, ref double amount, out Transfer? transfer)
    {
        transfer = null;
        pending.Remove(power);
        var shape = Equipment.Entry(Spec.Prefix);
        bool working = co.HasCond(Spec.WorkingCondition);
        double demand = working ? shape.workingKW : shape.idleKW;
        double electricHeat = working ? shape.workingKW * shape.roomHeatFraction : shape.idleKW;
        double seconds = amount * Units.SecondsPerHour / demand;
        double heatKW = electricHeat + (working && sessions.TryGetValue(co, out var bound) ? ReactionKW(Recipe(bound)) : 0);
        var air = RoomHeat.Read(co);
        var heat = RoomHeat.Check(air, heatKW, seconds);
        if (!heat.Admitted)
        {
            // Not a stop: no power is drawn this step and the charge keeps its permission. A melt that waits
            // too long freezes (see AfterPower); a drying charge simply resumes when the room can take the heat.
            if (sessions.TryGetValue(co, out var s) && s.State.Running)
            {
                if (!s.HeatWait) { s.HeatWait = true; s.WaitSince = StarSystem.fEpoch; }
                s.Status = RoomHeat.Describe(heat) + (Spec.HeatNote ? " " + T("heat_note") : "");
            }
            co.ZeroCondAmount("IsPowered");
            return false;
        }
        if (sessions.TryGetValue(co, out var ready)) SettleWait(co, ready);
        transfer = new Transfer { Air = air!, RequestedKWh = amount, WorkSeconds = seconds, HeatFraction = electricHeat / demand, Receipt = NativeEnergyReceipts.Begin(power, co, amount) };
        pending.Add(power, transfer);
        return true;
    }
    /// <summary>Heat-wait time accrues to the bound charge; a melt over its own duration is frozen.</summary>
    private void SettleWait(CondOwner co, Session s)
    {
        if (!s.HeatWait) return;
        s.HeatWait = false;
        double waited = StarSystem.fEpoch - s.WaitSince;
        if (waited > 0 && s.State.Bound) { s.State.WaitSeconds += waited; Save(co, s); }
    }
    internal void FinishPower(Powered power, CondOwner co, Transfer? transfer)
    {
        pending.Remove(power);
        if (transfer == null) return;
        double supplied = NativeEnergyReceipts.Complete(power, co, transfer.Receipt);
        if (!ManufacturingRules.Finite(supplied)) throw new InvalidOperationException("Invalid charge machine energy receipt.");
        Session? s = null;
        var recipe = sessions.TryGetValue(co, out s) && s.State.Running && co.HasCond(Spec.WorkingCondition) ? Recipe(s) : null;
        double before = s?.State.ProgressSeconds ?? 0;
        if (recipe != null)
        {
            // Progress is powered seconds: a partial supply credits a partial step.
            double poweredSeconds = transfer.RequestedKWh > 0 ? transfer.WorkSeconds * Math.Min(1, supplied / transfer.RequestedKWh) : 0;
            double now = StarSystem.fEpoch, elapsed = Math.Max(0, now - s!.Last); s.Last = now;
            s.State.ProgressSeconds = Math.Min(recipe.Seconds, s.State.ProgressSeconds + Math.Min(elapsed, poweredSeconds));
        }
        // The electricity's room share, plus the reaction heat over the progress just made (an absorbing reaction
        // takes its share out of the electricity's heat, never below nothing).
        double reaction = recipe == null ? 0 : recipe.ReactionKWhOver(s!.State.ProgressSeconds - before);
        if (reaction == 0) RoomHeat.Deposit(transfer.Air, supplied, transfer.HeatFraction);
        else RoomHeat.Deposit(transfer.Air, Math.Max(0, supplied * transfer.HeatFraction + reaction), 1);
        if (recipe == null) return;
        double due = recipe.OffGasDueKg(s!.State.ProgressSeconds / recipe.Seconds, s.State.EmittedKg);
        if (due > 0)
        {
            foreach (var pair in recipe.Split(due)) if (pair.Value > 0) RoomGas.Emit(transfer.Air, pair.Key, pair.Value);
            s.State.EmittedKg += due;
        }
        Save(co, s);
    }
    internal void Forget(Powered power) { pending.Remove(power); NativeEnergyReceipts.Forget(power); }
    internal void AfterPower(CondOwner co)
    {
        if (!sessions.TryGetValue(co, out var s) || !s.State.Running) return;
        try
        {
            var recipe = Recipe(s);
            if (recipe == null) { Stop(co, s, T("charge_changed")); return; }
            if (s.HeatWait)
            {
                double waited = StarSystem.fEpoch - s.WaitSince;
                if (Spec.Spoiled != null && Spec.Spoiled(recipe, s.State.WaitSeconds + Math.Max(0, waited))) { SettleWait(co, s); Finish(co, s); }
                return;
            }
            s.Status = co.HasCond("IsPowered") ? WorkingStatus(recipe) : T("waiting_power");
            if (s.State.ProgressSeconds >= recipe.Seconds && co.HasCond("IsPowered")) Finish(co, s);
        }
        catch (Exception ex) { Fault(co, ex); }
    }

    /// <summary>The charge becomes its products: every vessel the charge touches is checked, then the settlement
    /// places the solids, removes the bound units, draws and deposits the commodities under their conversion
    /// journals. A frozen melt yields slag.</summary>
    private bool Finish(CondOwner co, Session s)
    {
        SetWorking(co, false);
        var recipe = Recipe(s);
        var units = Charge(co, s);
        if (recipe == null || units == null || MachineProblem(co) != null) { Stop(co, s, T("charge_changed")); return false; }
        bool spoiled = Spec.Spoiled != null && Spec.Spoiled(recipe, s.State.WaitSeconds);
        var products = spoiled ? Spec.SpoiledProducts!(recipe) : recipe.Products;
        var vessels = ReadyVessels(co, Needs(recipe, products), out string why);
        if (vessels == null) { WaitForVessel(s, why); return false; }
        double depositKg = products.Where(p => ChargeCommodities.Is(p.Id)).Sum(p => p.Kg * p.Count);
        var delivery = new ChargeDelivery(this, co, units, recipe.Solids(products).ToList(), recipe.ChargeKg - recipe.OffGasKg - depositKg);
        var result = CommoditySettlement.Commit(vessels, units[0].strID, delivery);
        if (result != DeliveryResult.Completed)
        {
            s.VesselWait = true; s.NextVesselCheck = Cadence.RealTime + ManufacturingRules.VesselRecheckSeconds; s.Status = T("tray_full"); return false;
        }
        Plugin.Log(T(spoiled ? "spoiled_log" : "completed_log", RecipeName(recipe), co.strID));
        if (spoiled) Phobos.Ostranauts.Framework.Notices.PlayerNotices.Post(co.ship, "PhobosManufacturing.spoiled", Phobos.Ostranauts.Framework.Notices.NoticeLevel.Caution, T("spoiled_notice", co.strNameFriendly));
        s.State.Cycles++; s.State.Clear(); Save(co, s);
        if (s.Protected) return true;
        if (!(Resume(co, s) || Bind(co, s)) && !s.VesselWait) { s.AwaitingFeed = Feed(co)?.objContainer?.ContainedCOs.Count > 0 && s.AwaitingFeed; if (!s.AwaitingFeed) s.Status = T("complete"); }
        return true;
    }
    private sealed class ChargeDelivery : IBatchDelivery
    {
        private readonly ChargeMachine owner; private readonly CondOwner machine; private readonly List<CondOwner> units; private readonly IReadOnlyList<ProductSpec> specs; private readonly double solidsKg;
        private readonly List<CondOwner> products = new(); private TrayDelivery? plan; private bool retired;
        internal ChargeDelivery(ChargeMachine owner, CondOwner machine, List<CondOwner> units, IReadOnlyList<ProductSpec> specs, double solidsKg)
        { this.owner = owner; this.machine = machine; this.units = units; this.specs = specs; this.solidsKg = solidsKg; }
        public bool InputConsumed => retired || units.All(u => u.bDestroyed);
        public bool Prepare()
        {
            if (machine.objContainer == null || units.Any(u => u.objCOParent != owner.Feed(machine) || !owner.ValidFeed(u, machine))) return false;
            foreach (var spec in specs)
            for (int n = 0; n < spec.Count; n++)
            {
                var product = DataHandler.GetCondOwner(spec.Id) ?? throw new InvalidOperationException(owner.T("missing_output", spec.Id));
                products.Add(product);
                if (product.coStackHead != null || product.aStack.Count != 0 || product.GetCOsSafe(true).Count != 0 || !ProcessMaterial.MassMatches(product.GetTotalMass(), spec.Kg))
                    throw new InvalidOperationException(owner.T("output_definition_changed", spec.Id));
                if (!machine.objContainer.AllowedCO(product)) return false;
            }
            if (!ProcessMaterial.Balanced(solidsKg, products.Select(p => p.GetTotalMass()))) throw new InvalidOperationException(owner.T("unbalanced"));
            // Products go into the tray as stacks since Manufacturing 0.30.0 (Framework TrayDelivery).
            plan = TrayDelivery.Plan(machine.objContainer, products);
            return plan != null;
        }
        public void PlaceProducts()
        {
            if (plan == null) throw new InvalidOperationException(owner.T("unprepared"));
            plan.Place();
            if (products.Any(p => !StackUnits.Inside(p, machine))) throw new InvalidOperationException(owner.T("output_placement_failed"));
        }
        public void ConsumeInput()
        {
            if (units.Any(u => u.objCOParent != owner.Feed(machine) || !owner.ValidFeed(u, machine))) throw new InvalidOperationException(owner.T("charge_changed"));
            foreach (var unit in units)
            {
                bool gone;
                try { unit.RemoveFromCurrentHome(bForce: true); }
                finally { gone = unit.objCOParent == null && unit.ship == null; }
                if (!gone) throw new InvalidOperationException(owner.T("input_not_removed"));
                unit.Destroy();
            }
            retired = true;
            machine.objContainer.Redraw(); owner.Feed(machine)?.objContainer?.Redraw();
        }
        public void RollbackProducts()
        {
            plan?.Rollback();
            foreach (var product in products)
            {
                if (product == null || product.bDestroyed) continue;
                if (product.objCOParent != null || product.ship != null) product.RemoveFromCurrentHome(bForce: true);
                product.Destroy();
            }
            products.Clear();
            machine.objContainer.Redraw();
        }
    }

    // ---- Presentation and commands ----
    internal EquipmentState State(CondOwner co)
    {
        if (co.HasCond("IsDamaged") || co.HasCond("IsLocked")) return EquipmentState.Blocked;
        sessions.TryGetValue(co, out var s);
        if (s?.NeedsAttention == true || s?.Protected == true) return EquipmentState.Blocked;
        if (co.HasCond(Spec.WorkingCondition)) return co.HasCond("IsPowered") ? EquipmentState.Running : EquipmentState.Waiting;
        return s != null && (s.AwaitingFeed || s.VesselWait) ? EquipmentState.Waiting : EquipmentState.Paused;
    }
    /// <summary>Whether this machine is working and powered: an ignition source for a leaking store beside it.</summary>
    internal bool Igniting(CondOwner co) => Spec.IgnitionSource && co.HasCond(Spec.WorkingCondition) && co.HasCond("IsPowered");
    internal string Describe(CondOwner co)
    {
        var s = Get(co);
        var recipe = Recipe(s);
        var shape = Equipment.Entry(Spec.Prefix);
        int count = Feed(co)?.objContainer?.ContainedCOs.Count ?? 0;
        string charge = recipe == null ? T("no_charge_bound") : T("charge", RecipeName(recipe), s.State.ProgressSeconds, recipe.Seconds, s.State.WaitSeconds);
        string primary = Links.Count == 0 ? "" : Peer(co, Links[0]);
        string selected = Spec.Selection != RecipeSelection.Explicit ? "" : "\n" + (Catalog.ByRevision(s.State.Selected) is ChargeRecipe chosen ? T("selected", Text.Get("Recipe." + chosen.Id)) : T("no_selection"));
        string? extra = Spec.ExtraStatus?.Invoke(co);
        return T("status", s.Status, count, shape.feedCells, charge,
            co.HasCond("IsPowered") ? Text.Get("Content.powered") : Text.Get("Content.no_power"), ObjectPresentation.Name(primary), s.State.Cycles) + selected +
            "\n" + T("demand", shape.workingKW, RoomHeat.Machine(shape.workingKW * shape.roomHeatFraction)) +
            (extra == null ? "" : "\n" + extra) + (StoreFeed.Describe(co) is { Length: > 0 } fed ? "\n" + fed : "") + (StoreDelivery.Describe(co) is { Length: > 0 } sent ? "\n" + sent : "") + (s.LastStop == null ? "" : "\n" + Text.Get("Content.last_stop", s.LastStop));
    }
    /// <summary>The panel's fields: one per commodity link (shown once a vessel is in reach or linked, or always for
    /// the primary one), and the recipe choice on an explicit-selection machine.</summary>
    internal IEnumerable<EquipmentField> Fields(CondOwner co)
    {
        foreach (var link in Links)
        {
            var vessels = Candidates(co, link).ToArray();
            string peer = Peer(co, link);
            // A link with a vessel of its cargo aboard is shown even when none is in reach, so its sheet can say why.
            var cargo = link.Vessel;
            if (!link.AlwaysShow && vessels.Length == 0 && peer.Length == 0 && !BulkVessels.AboardAnyState(co.ship, cargo.Commodity).Any(v => v != co)) continue;
            yield return Provider.LinkField(link.FieldLabel(), link.ActionPrefix, peer, vessels.Select(v => (v, LinkChoices.Label(co, v, cargo, link.Deposit))),
                () => LinkChoices.Note(co, cargo, vessels));
        }
        yield return StoreFeed.Field(co);
        yield return StoreDelivery.Field(co);
        if (Spec.Selection == RecipeSelection.Explicit)
        {
            var s = Get(co);
            var chosen = Catalog.ByRevision(s.State.Selected);
            yield return new(Text.Get("Provider.recipe_field"), chosen == null ? Text.Get("Provider.link_none") : Text.Get("Recipe." + chosen.Id),
                Catalog.Available(Spec.Met).Select(r => ("recipe:" + r.Id, Text.Get("Recipe." + r.Id))), chosen == null ? "" : "recipe:" + chosen.Id);
        }
        // Optional recipes (0.51.0): what this machine does with feed it would otherwise leave alone.
        var optional = Optional().ToArray();
        if (optional.Length > 0)
        {
            string preferred = Preferred(co);
            yield return new(T("prefer_field"), preferred.Length == 0 || Catalog.ById(preferred) == null ? T("prefer_none") : Text.Get("Recipe." + preferred),
                new[] { (PreferPrefix + "none", T("prefer_none")) }.Concat(optional.Select(r => (PreferPrefix + r.Id, Text.Get("Recipe." + r.Id)))), PreferPrefix + (preferred.Length == 0 ? "none" : preferred));
        }
    }
    internal const string PreferPrefix = "prefer:";
    /// <summary>The optional recipes this installation could take: their other requirements are met.</summary>
    private IEnumerable<ChargeRecipe> Optional() => Catalog.All.Where(r => ChargeCatalog.IsOptional(r) && !ChargeOutcomes.IsHidden(r.Id) &&
        r.Requires.Where(k => ChargeCatalog.ChosenId(k) == null).All(Spec.Met));
    /// <summary>Sets or clears the optional recipe (idle and unbound, like a recipe selection): "none" leaves that feed alone.</summary>
    internal bool SelectPreference(CondOwner co, string value, ConsoleBinding? binding, out string reason)
    {
        reason = Content.Access(co, binding) ?? "";
        if (reason.Length > 0) return false;
        var s = Get(co);
        if (s.Protected) { reason = T("protected"); return false; }
        if (s.State.Running || s.State.Bound || co.HasCond(Spec.WorkingCondition)) { reason = T("prefer_busy"); return false; }
        string id = value == "none" ? "" : value;
        if (id.Length > 0 && !Optional().Any(r => r.Id == id)) { reason = T("prefer_missing", value); return false; }
        s.State.Prefer = id; Save(co, s);
        if (s.Protected) { reason = T("protected"); return false; }
        s.Status = reason = id.Length == 0 ? T("prefer_cleared") : T("prefer_set", Text.Get("Recipe." + id));
        return true;
    }
    internal bool Link(CondOwner co, ChargeLinkSpec link, string id, ConsoleBinding? binding, out string reason)
    {
        reason = Content.Access(co, binding) ?? "";
        if (reason.Length > 0) return false;
        if (co.HasCond(Spec.WorkingCondition) || sessions.TryGetValue(co, out var s) && s.State.Running) { reason = T("link_busy"); return false; }
        if (id == "none") { link.Vessel.Unlink(co, CrewWork.Resolve); reason = link.Unlinked(); return true; }
        var vessel = Candidates(co, link).FirstOrDefault(v => v.strID == id);
        if (vessel == null) { reason = link.Missing(); return false; }
        if (!link.Vessel.Link(co, vessel, CrewWork.Resolve, out reason)) return false;
        reason = link.Linked(); return true;
    }
    /// <summary>Chooses the recipe the next charge binds (explicit machines only): idle and unbound, so a bound or
    /// running charge keeps its recipe; units of another recipe stay in the feed until removed.</summary>
    internal bool SelectRecipe(CondOwner co, string value, ConsoleBinding? binding, out string reason)
    {
        reason = Content.Access(co, binding) ?? "";
        if (reason.Length > 0) return false;
        if (Spec.Selection != RecipeSelection.Explicit) { reason = Text.Get("Content.unsupported_action"); return false; }
        var s = Get(co);
        if (s.Protected) { reason = T("protected"); return false; }
        if (s.State.Running || s.State.Bound || co.HasCond(Spec.WorkingCondition)) { reason = T("select_busy"); return false; }
        var recipe = Catalog.ById(value) ?? (int.TryParse(value, out int revision) ? Catalog.ByRevision(revision) : null);
        if (recipe == null || !Available(recipe, co)) { reason = T("select_missing", value); return false; }
        s.State.Selected = recipe.Revision; Save(co, s);
        if (s.Protected) { reason = T("protected"); return false; }
        s.Status = reason = T("selected", Text.Get("Recipe." + recipe.Id));
        return true;
    }
    internal string? MaintenanceReason(CondOwner co)
    {
        var s = Get(co);
        if (s.Protected) return Text.Get("Maintenance.protected");
        return s.State.Bound ? Text.Get(Spec.MaintenanceChargeKey) : null;
    }
    /// <summary>Whether any of this machine's commodity links points at the vessel.</summary>
    internal bool LinksTo(CondOwner co, string vesselId) => Links.Any(l => Peer(co, l) == vesselId);
    internal bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = T("fault");
        if (!Content.Ready) { message = Content.Status; return false; }
        if (action.StartsWith("recipe:", StringComparison.Ordinal)) return SelectRecipe(co, action.Substring(7), binding, out message);
        if (action.StartsWith(PreferPrefix, StringComparison.Ordinal)) return SelectPreference(co, action.Substring(PreferPrefix.Length), binding, out message);
        if (action.StartsWith(StoreFeed.ActionPrefix, StringComparison.Ordinal))
        {
            message = Content.Access(co, binding) ?? "";
            return message.Length == 0 && StoreFeed.Command(co, action, out message);
        }
        // The longest matching prefix wins, so gas-link:<store>: is never read as a shorter prefix.
        foreach (var link in Links.OrderByDescending(l => l.ActionPrefix.Length))
            if (action.StartsWith(link.ActionPrefix, StringComparison.Ordinal)) return Link(co, link, action.Substring(link.ActionPrefix.Length), binding, out message);
        bool result;
        switch (action)
        {
            case "start": result = Start(co, binding); break;
            case "pause": result = Pause(co, false, binding); break;
            case "cancel": result = Pause(co, true, binding); break;
            case "status": result = true; break;
            default: message = Text.Get("Content.unsupported_action"); return false;
        }
        message = Describe(co);
        return result;
    }
}
