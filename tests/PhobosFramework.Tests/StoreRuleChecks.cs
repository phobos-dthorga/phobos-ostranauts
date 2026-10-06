using System;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Inventory;

/// <summary>Framework 0.116.0 (owner report, 6 October 2026: the V4's store picker offered a CIWS turret): which of the
/// game's containers count as stores is the <c>stores</c> data pack, judged by the game's own condition, container-rule
/// and interaction names.</summary>
internal static class StoreRuleChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var rule = StoreRules.Pack;
        StoreSchema.Validate(rule);
        Func<string, bool> Has(params string[] conds) => c => Array.IndexOf(conds, c) >= 0;
        var open = new[] { "Inventory" };
        check(StoreRules.Excluded(rule, Has("IsShipWeapon", "IsPowered"), open, "TIsFitAmmo20mm"), "A ship weapon's magazine is not a store");
        check(StoreRules.Excluded(rule, Has(), open, "TIsFitAmmoMissile03"), "Any ammunition rule is not a store, by its prefix");
        check(StoreRules.Excluded(rule, Has("IsToilet01"), Array.Empty<string>(), "TIsFitContainerSolid"), "A toilet is not a store");
        check(StoreRules.Excluded(rule, Has(), Array.Empty<string>(), "TIsFitContainerSolid"), "A container the player cannot open from its menu is not a store");
        check(StoreRules.Excluded(rule, Has("IsRechargingContainer"), open, "TIsFitContainerBattery04"), "A battery charger is not a store");
        check(StoreRules.Excluded(rule, Has("IsSafePumpTestudo"), open, "TIsFitContainerSafePumpO2Bottle"), "Another mod's bottle pump is not a store");
        check(!StoreRules.Excluded(rule, Has("IsRack", "IsStorageFurniture"), open, "TIsFitContainerSolidCumbersome"), "A rack is a store");
        check(!StoreRules.Excluded(rule, Has("IsFridge01"), open, "TIsFitContainerSolid"), "A fridge is a store");
        check(!StoreRules.Excluded(rule, Has(), new[] { "Inventory", "PhobosShipbreakerControls" }, "PhobosShipbreakerTFeed"), "A Phobos bin with its own rule is a store");
        check(StoreRules.Sealed(rule, Has("IsShipWeapon"), null) && !StoreRules.Sealed(rule, Has(), "TIsFitContainerSolid"),
            "Crew never take from a weapon, but a plain container is not sealed, open entry or not");
        // A pack that asks for nothing excludes nothing.
        var empty = new StorePack { schemaVersion = 1, schema = StoreSchema.Name };
        check(!StoreRules.Excluded(empty, Has("IsShipWeapon"), null, "TIsFitAmmo20mm"), "An empty stores pack excludes nothing");
        void Bad(Action<StorePack> mutate, string message)
        {
            var pack = new StorePack { schemaVersion = 1, schema = StoreSchema.Name, requireInteraction = "Inventory" };
            pack.excludeConditions.Add("IsShipWeapon"); pack.excludeContainers.Add("TIsFitAmmo*");
            StoreSchema.Validate(pack);
            mutate(pack); bool failed = false;
            try { StoreSchema.Validate(pack); } catch (ArgumentException) { failed = true; }
            check(failed, message);
        }
        Bad(p => p.requireInteraction = "Open Me", "An interaction is a plain name");
        Bad(p => p.excludeConditions.Add("IsShipWeapon"), "A condition is listed once");
        Bad(p => p.excludeConditions.Add("Is Weapon"), "A condition is a plain name");
        Bad(p => p.excludeContainers.Add("T*"), "A container prefix needs at least three letters");
        Bad(p => p.excludeContainers.Add("TIsFit*Ammo"), "A star may only end a container name");
        Bad(p => { for (int i = 0; i < StoreSchema.MaxNames; i++) p.excludeConditions.Add("IsThing" + i); }, "The condition list has a limit");
    }
}
