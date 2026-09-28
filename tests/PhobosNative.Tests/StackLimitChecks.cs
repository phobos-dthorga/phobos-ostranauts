using System;
using System.Collections.Generic;

internal static class StackLimitChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var expected = new Dictionary<string, int> {
            ["PhobosVerdemorrowWaterConduitLoose"] = 10,
            ["PhobosVerdemorrowWaterConduitLooseDmg"] = 10,
            ["PhobosFurnaceCoolantConduitLoose"] = 10,
            ["PhobosFurnaceCoolantConduitLooseDmg"] = 10,
            ["PhobosAutoNavBoardOffcut"] = 10,
            ["PhobosAutoNavBoardResidue"] = 10,
            ["PhobosVerdemorrowContinuancePotato"] = 10,
            ["PhobosVerdemorrowContinuanceLettuce"] = 50,
            ["PhobosVerdemorrowGroundworkNutrients"] = 25,
            ["PhobosVerdemorrowGroundworkMakeup"] = 25,
            ["PhobosVerdemorrowRawPotatoes"] = 10,
            ["PhobosVerdemorrowHearthPotatoes"] = 10,
            ["PhobosVerdemorrowLettuce"] = 10,
            ["PhobosVerdemorrowGroundworkRecoveryCartridge"] = 3,
            ["PhobosVerdemorrowGroundworkBulkNutrients"] = 3,
            ["PhobosVerdemorrowGroundworkIrrigation"] = 3,
            ["PhobosRivetlineCoolantCharge"] = 3,
            ["PhobosFloorRejectR1"] = 10,
            ["PhobosAluminiumIngot"] = 10,
            ["PhobosSteelIngot"] = 10
        };
        var seen = new HashSet<string>();
        foreach (var pack in new[] { PhobosAgriculture.Definitions.Prepare(), PhobosShipbreaker.Content.Prepare(), PhobosAutoNav.EquipmentContent.Prepare() })
        foreach (var item in pack.Objects.Values)
        {
            if (!item.strName.StartsWith("Phobos", StringComparison.Ordinal)) continue;
            int limit = expected.TryGetValue(item.strName, out int value) ? value : 1;
            check(Math.Max(1, item.nStackLimit) == limit, "Only approved loose supplies stack; installed fixtures and other goods remain individual: " + item.strName);
            if (expected.ContainsKey(item.strName)) seen.Add(item.strName);
        }
        check(seen.SetEquals(expected.Keys), "Every approved stackable identity exists in prepared native definitions");
    }
}
