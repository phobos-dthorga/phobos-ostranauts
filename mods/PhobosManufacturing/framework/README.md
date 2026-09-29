# Manufacturing content registration

Manufacturing 0.1.0 registers its equipment in code through Framework's
`ApplianceDefinitions` (three families: `PhobosVolatilesRefinery*`,
`PhobosChemicalProcessor*`, `PhobosHydrogenStore*`), its materials by cloning
native items (`PhobosNickelIronIngot`, `PhobosCarbonStock`, `PhobosRefinerySlag`,
`PhobosAnhydrousResidue`, `PhobosClayHydrates`) and its hydrogen deflagrations
as native explosion objects (`SysPhobosDeflagrationSmall/Medium/Large`, with
their entries in `data/explosions`). These identities are save-stable from 0.1.0.

`equipment-names.json` is the content-owned `Localization.EquipmentNames` map:
brand Fennmark, models V4, X2 and H2, and the branded materials. Translations
localize the type descriptors; the `Phobos'` prefix and model names stay.

There are no construction recipes: the machines are purchase-only, and ores are
mined. No OCF recipe directory or legacy aliases exist for this mod. The
`data/conditions` file carries the `PhobosManufacturingContent` marker the
plugin checks to confirm the native mod folder is enabled.
