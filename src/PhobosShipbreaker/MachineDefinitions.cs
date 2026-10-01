using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosShipbreaker;

// Phobos-authored definitions built against the native installable interface.
// Preserve saved Phobos identities; no Workshop template lookup or bundled game data.
internal static class MachineDefinitions
{
    private const string P = "PhobosShipbreaker";
    internal static NativeDefinitions Create()
    {
        var d = new NativeDefinitions();
        AddFamily(d, P);
        AddFeed(d, P, "IsWall1x1", FeedFamilyConditions);
        return d;
    }

    // The same native install/repair/damage contract serves every machine; since Framework 0.58.0 the generator is
    // Framework's (MachineFamilies), so equipment that moves to Framework keeps its ids. The output is unchanged.
    internal static void AddFamily(NativeDefinitions d, string P, string buildCategory = InstallMenu.Appliances) =>
        MachineFamilies.Add(d, P, buildCategory, Text.Get("MachineDefinitions.dismantling_fixture"),
            Text.Get("MachineDefinitions.phobos_powered_dismantling_fixture"), Text.Get("MachineDefinitions.dismantling_fixture_2"));

    // The D4 feed admits the game's structural part families at the game level (any wall, any floor grate: the
    // same conditions the game's scrap kiosks buy by); FeedPatch then applies the feed families' own rule.
    // IsCategoryHull is not used: it would admit doors, hatches, docking systems and the turbine lifter.
    internal static readonly string[] FeedFamilyConditions = { "IsWall", "IsFloorGrate" };
    internal static void AddFeed(NativeDefinitions d, string P = "PhobosShipbreaker", string feedCondition = "IsWall1x1", string[]? familyConditions = null)
    {
        // Native ordinary walls are cumbersome. Keep native containment exclusions,
        // then narrow acceptance to the feed's parts; FeedPatch enforces identity/mass/count.
        var triggers = new List<string> { "TIsFitContainerSolidCumbersome" };
        if (familyConditions != null)
        {
            d.Triggers.Add(P + "TFeedFamily", new CondTrigger { strName = P + "TFeedFamily", fChance = 1, fCount = 1,
                bAND = false, aReqs = familyConditions.ToArray(), aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() });
            triggers.Add(P + "TFeedFamily");
        }
        d.Triggers.Add(P + "TFeed", new CondTrigger { strName = P + "TFeed", fChance = 1, fCount = 1,
            bAND = true, aReqs = familyConditions != null ? Array.Empty<string>() : new[] { feedCondition }, aForbids = Array.Empty<string>(),
            aTriggers = triggers.ToArray() });
        d.Objects.Add(P + "InputBin", new JsonCondOwner { strName = P + "InputBin", strNameFriendly = Text.Get("MachineDefinitions.wall_panel_feed"),
            strNameShort = Text.Get("MachineDefinitions.wall_panel_feed"), strType = "Item", strItemDef = "Blank", strPortraitImg = "blank",
            strContainerCT = P + "TFeed", nStackLimit = 1, bSlotLocked = true,
            nContainerWidth = 4, nContainerHeight = 4, aInteractions = Array.Empty<string>(),
            aStartingConds = new[] { "IsContainer=1.0x1", "IsSystem=1.0x1" }, mapSlotEffects = new[] { P + "Input", "Blank" } });
        EquipmentInventory.Declare(d.Objects[P + "InputBin"], InventorySpec.Feed(4, 4, P + "TFeed"));
        d.Slots.Add(P + "Input", new JsonSlot { strName = P + "Input", strNameFriendly = Text.Get("MachineDefinitions.wall_panel_feed"),
            strHitboxImage = "blank", nItems = 1, nDepth = 15, bCarried = true });
        d.Loot.Add(P + "Compartments", ItemLoot(P + "Compartments", P + "InputBin"));
        d.Power.Add(P + "Power", new JsonPowerInfo { strName = P + "Power", strUsePowerCT = "TIsReadyUsePower",
            aInputPts = new[] { "PowerA", "PowerB" }, bAllowExtPower = true,
            strIntPowerOn = P + "PowerChange", strIntPowerOff = P + "PowerChange" });
        d.Interactions.Add(P + "PowerChange", new JsonInteraction { strName = P + "PowerChange", strThemType = "Self",
            bIgnoreFeelings = true, aLootItms = Array.Empty<string>() });
    }
    private static Loot ItemLoot(string id, string item) => MachineFamilies.ItemLoot(id, item);
}
