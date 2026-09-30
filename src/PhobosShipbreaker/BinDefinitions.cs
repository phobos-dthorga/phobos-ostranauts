using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Native definitions for the Rivetline material bins: passive stores on the shared machine family
/// contract, with an ordinary inventory grid on every form so a native mode switch (damage, repair, install)
/// carries the contents across. No power, no feed, no tickers and no crew provider: the game and our crew orders
/// treat an installed bin as an ordinary unlocked container.</summary>
internal static class BinDefinitions
{
    internal static void Add(NativeDefinitions d)
    {
        // Refuse rather than fall back: an unknown trigger name resolves to the game's always-true Blank trigger.
        if (NativeDefinitions.Trigger(BinRules.NativeMiningOutput) == null || NativeDefinitions.Trigger(BinRules.NativeSolid) == null)
            throw new InvalidOperationException(Text.Get("Bin.missing_native_trigger", BinRules.NativeMiningOutput));
        d.Triggers[BinRules.Trigger] = new CondTrigger { strName = BinRules.Trigger, fChance = 1, fCount = 1, bAND = true,
            aReqs = Array.Empty<string>(), aForbids = Array.Empty<string>(), aTriggers = new[] { BinRules.NativeSolid, BinRules.NativeMiningOutput } };
        foreach (var size in BinRules.Sizes) AddBin(d, size);
    }

    private static void AddBin(NativeDefinitions d, BinSize size)
    {
        string p = size.Prefix; int footprint = size.Footprint;
        // The game's own Storage Bay installs from the furniture tab; players look for storage there.
        MachineDefinitions.AddFamily(d, p, InstallMenu.Furniture);
        d.Conditions[p + "Machine"].strNameFriendly = Text.Get(size.NameKey);
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            bool installed = state.StartsWith("Installed", StringComparison.Ordinal), damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
            var co = d.Objects[p + state]; var item = d.Items[p + state];
            co.strNameFriendly = co.strNameShort = Text.Get(size.NameKey) + (damaged ? Text.Get("Content.damaged") : "");
            co.strDesc = Text.Get("Bin.description", size.DryKg, size.Grid, footprint);
            // An ordinary store: its own grid and admission rule, no hidden feed compartment, no electricity.
            co.strLoot = "Blank"; co.aSlotsWeHave = Array.Empty<string>();
            co.strContainerCT = BinRules.Trigger; co.nContainerWidth = co.nContainerHeight = size.Grid;
            co.jsonPI = null; co.aTickers = Array.Empty<string>();
            co.aInteractions = new[] { "Inventory" };
            co.inventoryWidth = co.inventoryHeight = footprint;
            Content.SetStat(co, "StatMass", size.DryKg);
            co.mapPoints = new[] { "use,0," + (-8 * footprint - 8) };
            item.nCols = footprint; item.fZScale = 0.5f;
            item.aSocketAdds = Enumerable.Repeat(installed ? "TILFixtureAdds" : "TILItemAdds", footprint * footprint).ToArray();
            item.aSocketReqs = SiloDefinitions.Border(footprint, installed ? "TILFloor" : "Blank");
            item.aSocketForbids = SiloDefinitions.Border(footprint, installed ? "TILObstruction" : "TILItemForbids");
            // One dedicated overhead sprite for every form, as the inventory portrait too; damaged forms use the
            // game's damage tint. The master and its provenance are in assets/artwork-completion.
            Content.ApplyArtwork(co, item, size.Art, size.Art);
            co.strPortraitImg = item.strImg;
        }
    }
}
