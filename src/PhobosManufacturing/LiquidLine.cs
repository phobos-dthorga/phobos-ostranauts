using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Items;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>A liquid line's network family and native definitions: the segment forms on Framework's shared pattern in its
/// lane, its bills from the economy pack, and its ports on the equipment that holds or uses its liquid. The liquid is
/// assigned to this family, so every link and pour of it reaches through touching equipment or this line (the owner's
/// link rule), never across open floor. The Lixivar acid line (Manufacturing 0.24.0) and the Alembrine ethanol line
/// (0.38.0) are two instances.</summary>
internal sealed class LiquidLine
{
    internal static readonly LiquidLine Acid = new(AcidLineRules.Rules, () =>
        new[] { (LeachRules.Prefix, Equipment.Entry(LeachRules.Prefix).footprint), (AcidPlantRules.Prefix, Equipment.Entry(AcidPlantRules.Prefix).footprint) }
            .Concat(LiquidStores.AcidFamily.Sizes.Select(s => (s.Prefix, s.Footprint))));
    internal static readonly LiquidLine Ethanol = new(EthanolLineRules.Rules, () => LiquidStores.EthanolFamily.Sizes.Select(s => (s.Prefix, s.Footprint)));
    internal static readonly IReadOnlyList<LiquidLine> All = new[] { Acid, Ethanol };

    internal LiquidLineRules Rules { get; }
    internal FluidSegmentFamily Family { get; }
    private readonly Func<IEnumerable<(string Prefix, int Footprint)>> ported;
    private LiquidLine(LiquidLineRules rules, Func<IEnumerable<(string, int)>> ported)
    {
        Rules = rules; this.ported = ported;
        Family = new(rules.FamilyId, c => c.strCODef == rules.Installed, c => LinePorts.Points(rules.FamilyId, c.strCODef), adjacencyJoins: true) { Label = () => Text.Get(rules.TextPrefix + ".label") };
    }
    /// <summary>Every definition with a port on this line.</summary>
    internal IEnumerable<(string Prefix, int Footprint)> Ported => ported();

    internal LineSegmentSpec Spec()
    {
        string name = Text.Get(Rules.TextPrefix + ".name");
        return new LineSegmentSpec
        {
            Prefix = Rules.Prefix, Name = name, DamagedName = name + Text.Get(Rules.TextPrefix + ".damaged"), Description = Text.Get(Rules.TextPrefix + ".description"),
            Art = Definitions.ImagePath + Rules.Art, Present = Rules.Present, Intact = Rules.Intact, Kg = LiquidLineRules.SegmentKg,
            Price = Economy.SupplyPrice(Rules.Prefix), InstallTab = InstallMenu.Hvac, LooseStack = SharedLines.LooseStack, Layer = Rules.Layer,
            Family = Rules.FamilyId
        };
    }

    internal static void AddAll(NativeDefinitions d) { foreach (var line in All) line.Add(d); }

    private void Add(NativeDefinitions d)
    {
        LineFamilies.Assign(Rules.Liquid.Commodity, Family);
        var spec = Spec();
        LineDefinitions.Add(d, spec);
        // The line holds its liquid until drained (Manufacturing 0.25.0): an acid line's damaged segment mists the tanks'
        // fraction into the room as H2SO4; an ethanol line's keeps its ethanol, which can burn (EthanolLineFirePatch).
        LineContents.Declare(Family, Rules.Prefix, new[] { Rules.Held() });
        LineContents.OfferActions(d, Rules.Prefix, gas: false);
        // Bills from the pack, as Framework's own lines: repair with its material, dismantling to retained waste
        // (liquid-wetted lining is not recovered as clean metal), the full segment mass.
        var supply = Economy.Pack.supplies[Rules.Prefix];
        string waste = supply.remainder ?? Rules.Prefix + "Waste";
        MaintenanceDefinitions.Remainder(d, waste, Text.Get(Rules.TextPrefix + ".waste"), LiquidLineRules.SegmentKg);
        foreach (string form in LineDefinitions.Forms)
        {
            string id = Rules.Prefix + form; var co = d.Objects[id];
            if (form.EndsWith("Dmg", StringComparison.Ordinal))
            {
                MaintenanceDefinitions.SetStat(co, "StatRepairProgressMax", supply.repairWork);
                var repair = d.Installables[id + "Repair"];
                repair.aInputs = EconomyStock.RepairInputs(supply.repairBill);
                MaintenanceDefinitions.Repair(d, repair);
            }
            MaintenanceDefinitions.Dismantle(d, id, supply.dismantleWork, new[] { waste });
            EquipmentSaveUpgrade.Register(d, id, id);
        }
        // Ports by Framework's rule for this liquid (acid: the +X side one row below the gas port; ethanol: the -X side
        // one row below the water port).
        foreach (var (prefix, footprint) in Ported)
        {
            var port = Rules.Port(footprint);
            LineDefinitions.AddPort(d, prefix, spec, Rules.PortPoint, port.X, port.Y, port.Socket);
        }
    }
}
