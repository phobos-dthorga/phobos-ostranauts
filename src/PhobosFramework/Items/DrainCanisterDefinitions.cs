using System;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Items;

/// <summary>The Rivetline D20 drain canister's native definition (Framework 0.63.0): one 1 x 1 pocketable item that never
/// stacks (each canister keeps its own record of what it holds), sold with the lines. Its liquid is a saved record and
/// its native mass the housing plus that liquid (<see cref="DrainCanisters"/>).</summary>
public static class DrainCanisterDefinitions
{
    public const string Art = SharedLines.ImagePath + "DrainCanister";
    /// <summary>The canister's price: a lined 20 litre steel can, well above its 3 kg of steel (scrap steel is 3.6 cr/kg).</summary>
    public const double Price = 40;
    /// <summary>The canister's own condition and the container rule that admits only canisters: a store with no general
    /// inventory (an acid tank) takes canisters through <see cref="ApplianceDefinitions.SetRack"/> with this trigger.</summary>
    public const string Marker = "IsPhobosDrainCanister", RackTrigger = "TIsPhobosDrainCanister";
    /// <summary>A canister rack a store offers for pouring: four cells, one row of two by two.</summary>
    public const int RackWidth = 2, RackHeight = 2;

    internal static void Add(NativeDefinitions d)
    {
        string id = DrainCanisterRules.Id;
        d.Conditions[Marker] = new JsonCond { strName = Marker, strNameFriendly = Text.Get("DrainCanister.name"), strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        d.Triggers[RackTrigger] = new CondTrigger { strName = RackTrigger, fChance = 1, fCount = 1, bAND = true, aReqs = new[] { Marker },
            aForbids = new[] { "IsInstalled" }, aTriggers = Array.Empty<string>() };
        var co = NativeDefinitions.Clone(DataHandler.dictCOs["ItmScrapTrash"]);
        co.strName = id; co.strNameFriendly = co.strNameShort = Text.Get("DrainCanister.name");
        co.strDesc = Text.Get("DrainCanister.description", DrainCanisterRules.Litres, DrainCanisterRules.DryKg);
        co.nStackLimit = 1; co.inventoryWidth = co.inventoryHeight = 1;
        co.aTickers = co.aUpdateCommands = Array.Empty<string>();
        co.aInteractions = new[] { "DropItem", "PickupItem" };
        co.aStartingConds = new[] { "IsSolid=1x1", "IsPocketable=1x1", "IsCategoryIndustrialProducts=1x1", "StatDamageMax=1x10", Marker + "=1x1",
            "StatMass=1x" + DrainCanisterRules.DryKg.ToString(CultureInfo.InvariantCulture), "StatBasePrice=1x" + Price.ToString(CultureInfo.InvariantCulture) };
        var item = NativeDefinitions.Clone(DataHandler.dictItemDefs[co.strItemDef]);
        co.strItemDef = item.strName = id;
        item.strImg = Art; item.strImgNorm = Art + "Normal"; item.strImgDamaged = "blank";
        co.strPortraitImg = Art;
        d.Items[id] = item; d.Objects[id] = co;
    }
}
