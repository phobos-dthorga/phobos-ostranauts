using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Persistence;

/// <summary>Framework 0.56.0 load-time definition conversion over the game's own saved-data types: a retired
/// definition's saved item and object name the new one, its records move to their new names (rewritten where the
/// content asks), other items are untouched, and a second pass changes nothing. The game re-reads map points and
/// socket adds from the definition on load, which is why conversion only renames.</summary>
internal static class DefinitionMigrationChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        DefinitionMigrations.Reset();
        try
        {
            DefinitionMigrations.Register("PhobosTestOldTank", "PhobosTestNewTank", new Dictionary<string, string> { ["PhobosState.OldRecord"] = "PhobosState.NewRecord" },
                (oldName, values) => { values["capacity"] = "1000"; values["from"] = oldName; return values; });
            throws(() => DefinitionMigrations.Register("PhobosTestNewTank", "PhobosTestOther"), "A migration target cannot itself be migrated");
            throws(() => DefinitionMigrations.Register("PhobosTestSame", "PhobosTestSame"), "A migration needs two different ids");
            var record = DataHandler.ConvertDictToStringArray(new Dictionary<string, string> { ["capacity"] = "120", ["water"] = "42.5" });
            var item = new JsonItem { strName = "PhobosTestOldTank", strID = "tank-1", fX = 3, fY = 4, fRotation = 90,
                aGPMSettings = new[] { new JsonGUIPropMap { strName = "PhobosState.OldRecord", dictGUIPropMap = record },
                                       new JsonGUIPropMap { strName = "PhobosMaterialPort.example.In", dictGUIPropMap = new[] { "schema", "1" } } } };
            var other = new JsonItem { strName = "ItmMineral02", strID = "rock" };
            check(DefinitionMigrations.Pending(new[] { item, other }).SequenceEqual(new[] { "PhobosTestOldTank" }), "Only the retired definition is pending");
            check(DefinitionMigrations.Convert(item) && !DefinitionMigrations.Convert(other), "The retired item converts; others are left alone");
            var values = DataHandler.ConvertStringArrayToDict(item.aGPMSettings[0].dictGUIPropMap);
            check(item.strName == "PhobosTestNewTank" && item.strID == "tank-1" && item.fX == 3 && item.fY == 4 && item.fRotation == 90,
                "The converted item keeps its id, position and rotation");
            check(item.aGPMSettings[0].strName == "PhobosState.NewRecord" && values["water"] == "42.5" && values["capacity"] == "1000" && values["from"] == "PhobosState.OldRecord",
                "The record moves to its new name with its contents and the content's rewrite");
            check(item.aGPMSettings[1].strName == "PhobosMaterialPort.example.In", "Saved links are carried unchanged");
            check(!DefinitionMigrations.Convert(item) && DefinitionMigrations.Converted["PhobosTestOldTank"] == 1, "A second pass changes nothing (idempotent)");
            var saved = new JsonCondOwnerSave { strID = "tank-1", strCODef = "PhobosTestOldTank", aConds = new[] { "IsInstalled=1.0x1" } };
            var copy = DefinitionMigrations.Convert(saved);
            check(copy.strCODef == "PhobosTestNewTank" && saved.strCODef == "PhobosTestOldTank" && copy.aConds.SequenceEqual(saved.aConds),
                "The saved object is copied under the new definition, conditions kept, the original untouched");
            var plainSave = new JsonCondOwnerSave { strCODef = "ItmMineral02" };
            check(ReferenceEquals(DefinitionMigrations.Convert(plainSave), plainSave), "Other saved objects are returned as they are");
            check(typeof(JsonItem).GetProperties().All(p => !p.Name.Contains("Point") && !p.Name.Contains("Socket") && !p.Name.Contains("SpriteSheet")),
                "The game saves no map points, socket adds or sprite sheets per item: new line ports reach old saves from the definition");

            // Agriculture 0.31.0: a saved R3 becomes Framework's S3 (and R4, R5 the S4, S5), keeping its water record,
            // links, position and wear, with its records under the silo names and owner and its mass moved by the
            // difference in housing.
            DefinitionMigrations.Reset();
            PhobosAgriculture.Definitions.Prepare();
            string[] Envelope(string owner) => DataHandler.ConvertDictToStringArray(new Dictionary<string, string> { ["schema"] = "1", ["owner"] = owner, ["data.service"] = "100" });
            JsonItem Reservoir(string id, string prefix, string record) => new()
            {
                strName = prefix + "Installed", strID = id, fX = 5, fY = 6, fRotation = 180,
                aGPMSettings = new[]
                {
                    new JsonGUIPropMap { strName = "PhobosState." + record, dictGUIPropMap = Envelope(PhobosAgriculture.Plugin.Id) },
                    new JsonGUIPropMap { strName = "PhobosMaterialPort.PhobosAgriculture.BulkOut", dictGUIPropMap = new[] { "schema", "1" } }
                }
            };
            var r3 = Reservoir("r3-1", PhobosAgriculture.BulkDefinitions.Tank, "AgricultureBulk");
            var r4 = Reservoir("r4-1", PhobosAgriculture.BulkDefinitions.Tank + "Medium", "AgricultureBulkMedium");
            check(DefinitionMigrations.Convert(r3) && DefinitionMigrations.Convert(r4) && r3.strName == "PhobosProcessSiloInstalled" && r4.strName == "PhobosProcessSiloMediumInstalled" &&
                r3.strID == "r3-1" && r3.fX == 5 && r3.fY == 6 && r3.fRotation == 180, "Each reservoir becomes the silo of its footprint, in place, with its id");
            var water = DataHandler.ConvertStringArrayToDict(r3.aGPMSettings[0].dictGUIPropMap);
            check(r3.aGPMSettings[0].strName == "PhobosState.ShipbreakerSilo" && r4.aGPMSettings[0].strName == "PhobosState.ShipbreakerSiloMedium" &&
                water["owner"] == Phobos.Ostranauts.Framework.Items.WaterTanks.RecordOwner && water["data.service"] == "100",
                "The water record takes the silo's name and owner, contents unchanged");
            check(r3.aGPMSettings[1].strName == "PhobosMaterialPort.PhobosAgriculture.BulkOut", "The W2 link is carried: the tank's outlet bank keeps the reservoir's port id");
            var r3Save = new JsonCondOwnerSave { strID = "r3-1", strCODef = PhobosAgriculture.BulkDefinitions.Tank + "Installed",
                aConds = new[] { "IsInstalled=1.0x1", "StatMass=1.0x145", "StatDamage=1.0x3", PhobosAgriculture.BulkDefinitions.Tank + "Machine=1.0x1", "StatBasePrice=1.0x450", "IsLocked=1.0x1" } };
            var s3 = DefinitionMigrations.Convert(r3Save);
            double Amount(string[] conds, string name) => double.Parse(conds.Single(c => c.StartsWith(name + "=", StringComparison.Ordinal)).Split('x').Last(), System.Globalization.CultureInfo.InvariantCulture);
            check(s3.strCODef == "PhobosProcessSiloInstalled" && Amount(s3.aConds, "StatMass") == 360 && Amount(s3.aConds, "StatBasePrice") == 4800 && Amount(s3.aConds, "StatDamage") == 3 &&
                s3.aConds.Any(c => c.StartsWith("IsLocked=", StringComparison.Ordinal)) && s3.aConds.Any(c => c.StartsWith("PhobosProcessSiloMachine=", StringComparison.Ordinal)) &&
                !s3.aConds.Any(c => c.StartsWith(PhobosAgriculture.BulkDefinitions.Tank + "Machine=", StringComparison.Ordinal)),
                "Saved conditions take the silo's: 120 kg of water now weighs with a 240 kg housing, the silo's price and family mark, wear and locks kept");
            var fresh = DefinitionMigrations.Convert(new JsonCondOwnerSave { strID = "r3-2", strCODef = PhobosAgriculture.BulkDefinitions.Tank + "Loose", aConds = new[] { "DEFAULT" } });
            check(fresh.strCODef == "PhobosProcessSiloLoose" && Amount(fresh.aConds, "StatMass") == 240, "An untouched loose reservoir becomes an empty loose silo");
        }
        finally { DefinitionMigrations.Reset(); }
    }
}
