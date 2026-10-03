using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>How a charge machine picks the recipe it binds: the largest complete charge in the feed (the V4), or the
/// recipe the crew selected on the panel (the F6's pattern).</summary>
internal enum RecipeSelection { Automatic, Explicit }

/// <summary>Why a linked vessel cannot take part in settling a charge now.</summary>
internal enum LinkProblem { None, NotReady, Protected, Busy, Catch, Short, Full }

/// <summary>One commodity link of a charge machine: the vessel it draws from, deposits into or circulates through,
/// the reciprocal port pair (new ids per machine, because pairing is one-to-one per port), which vessels qualify, and
/// the texts the panel and status lines use.</summary>
internal sealed class ChargeLinkSpec
{
    internal string Commodity { get; }
    /// <summary>The action prefix a link command carries (the V4 keeps <c>link:</c> and <c>gas-link:&lt;store&gt;:</c>).</summary>
    internal string ActionPrefix { get; }
    internal string MachinePort { get; }
    internal string PeerPort { get; }
    /// <summary>Which registered vessels of the commodity qualify (a gas link takes only that gas's stores).</summary>
    internal Func<CondOwner, bool> Accepts { get; }
    internal Func<string> FieldLabel { get; }
    /// <summary>Shown on the panel even with nothing in reach (the V4's water vessel); otherwise shown once a candidate or a link exists.</summary>
    internal bool AlwaysShow { get; }
    /// <summary>The status fragment for a problem: (problem, kilograms the vessel offers, kilograms the charge needs).</summary>
    internal Func<LinkProblem, double, double, string> Reason { get; }
    internal Func<string> Linked { get; }
    internal Func<string> Unlinked { get; }
    internal Func<string> Missing { get; }
    /// <summary>Whether the machine deposits into the vessel (a destination shows "full" on the panel, a source "empty").</summary>
    internal bool Deposit { get; }
    /// <summary>The shared link (Manufacturing 0.23.0): the vessel-side port is a bank, so several machines share one
    /// vessel, and the vessel must touch the machine or share the commodity's line with it.</summary>
    internal VesselLink Vessel { get; }
    internal ChargeLinkSpec(string commodity, string actionPrefix, string machinePort, string peerPort, Func<CondOwner, bool> accepts, Func<string> fieldLabel, bool alwaysShow,
        Func<LinkProblem, double, double, string> reason, Func<string> linked, Func<string> unlinked, Func<string> missing, bool deposit = false)
    {
        Commodity = commodity; ActionPrefix = actionPrefix; MachinePort = machinePort; PeerPort = peerPort; Accepts = accepts; FieldLabel = fieldLabel; AlwaysShow = alwaysShow;
        Reason = reason; Linked = linked; Unlinked = unlinked; Missing = missing; Deposit = deposit;
        Vessel = new VesselLink(machinePort, peerPort, commodity);
    }
}

/// <summary>Everything that distinguishes one charge machine from another: identities and records, texts, the feed
/// rule, the recipe catalog key and requirement gate, how recipes are chosen, what happens to a melt left waiting,
/// whether it can ignite a leaking store, and its commodity links. The engine (<see cref="ChargeMachine"/>) is shared.</summary>
internal sealed class ChargeMachineSpec
{
    // The derived ids are built once: the feed hooks compare InputBin for every container admission in the game.
    private string prefix = "", installed = "Installed", inputBin = "InputBin", inputSlot = "Input", feedTrigger = "TFeed";
    internal string Prefix
    {
        get => prefix;
        set { prefix = value; installed = value + "Installed"; inputBin = value + "InputBin"; inputSlot = value + "Input"; feedTrigger = value + "TFeed"; }
    }
    internal string Installed => installed;
    internal string InputBin => inputBin;
    internal string InputSlot => inputSlot;
    internal string FeedTrigger => feedTrigger;
    /// <summary>The game-level stock trigger the feed bin admits besides native ore (the V4 keeps its original name).</summary>
    internal string StockTrigger { get; set; } = "";
    /// <summary>Own stock identities the feed admits at the game level (each carries an <c>&lt;id&gt;Identity</c> condition).</summary>
    internal IReadOnlyList<string> StockFeed { get; set; } = Array.Empty<string>();
    /// <summary>Whether the game-level feed rule also admits native ore (the V4's TIsOre rule).</summary>
    internal bool AdmitsOre { get; set; }
    /// <summary>Further native conditions the game-level feed rule admits (the V4's CO2 filters, Manufacturing 0.27.0).</summary>
    internal IReadOnlyList<string> FeedConditions { get; set; } = Array.Empty<string>();
    internal string Record { get; set; } = "";
    /// <summary>The catalog key of this machine's recipes (<c>refinery</c>).</summary>
    internal string MachineKey { get; set; } = "";
    /// <summary>Prefix of every status and log text key (<c>Refinery</c>).</summary>
    internal string TextPrefix { get; set; } = "";
    internal string SnapshotKind { get; set; } = "";
    internal string Art { get; set; } = "";
    internal RecipeSelection Selection { get; set; }
    /// <summary>A working, powered machine is an ignition source for a leaking store beside it (the V4's hearth).</summary>
    internal bool IgnitionSource { get; set; }
    internal string WorkingCondition { get; set; } = ManufacturingRules.Working;
    /// <summary>The machine adds its own line (<c>heat_note</c>) to the shared heat-wait message.</summary>
    internal bool HeatNote { get; set; }
    /// <summary>Whether each requirement key a recipe names is met on this installation.</summary>
    internal Func<string, bool> Met { get; set; } = _ => false;
    /// <summary>A melt left waiting longer than this spoils (null: this machine's charges never spoil).</summary>
    internal Func<ChargeRecipe, double, bool>? Spoiled { get; set; }
    internal Func<ChargeRecipe, IReadOnlyList<ProductSpec>>? SpoiledProducts { get; set; }
    /// <summary>The commodity links, rebuilt when the recipe catalog is reloaded.</summary>
    internal Func<IReadOnlyList<ChargeLinkSpec>> Links { get; set; } = () => Array.Empty<ChargeLinkSpec>();
    /// <summary>Extra status lines (the V4's missing-Shipbreaker note), or null.</summary>
    internal Func<CondOwner, string?>? ExtraStatus { get; set; }
    /// <summary>Why removal is refused while a charge is bound.</summary>
    internal string MaintenanceChargeKey { get; set; } = "Maintenance.charge";
    internal string Text(string key) => TextPrefix + "." + key;
}
