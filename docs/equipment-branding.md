# Equipment brands and models

Owner memorandum enacted in Framework 0.12.0, Shipbreaker 0.10.1 and Auto Nav
0.8.1. Every full equipment name starts with **Phobos'**, including the apostrophe.
These are original project brands, not existing game manufacturers.

**Asterel** makes navigation and control electronics. **Rivetline** makes salvage
machinery and material-handling equipment. Share these families where equipment
has a related purpose; new brands should serve a distinct equipment identity.

| Full display name | Previous equipment / purpose |
| --- | --- |
| Phobos' Asterel N1 Polaris Auto Nav Module | Auto Nav module; Polaris is the compatible navigation station |
| Phobos' Asterel C1 Industrial Control Console | Central industrial console |
| Phobos' Rivetline D4 Dismantling Fixture | Powered wall-panel processor |
| Phobos' Rivetline G4 Exterior Grabber | Four-wide exterior panel intake |
| Phobos' Rivetline H4 Sealed Hull Chute | Four-wide hull transfer connection |
| Phobos' Rivetline R4 Scrap Reclaimer | Four-by-four residue reclaimer |
| Phobos' Rivetline F6 Electric Furnace | Six-by-six electrical casting furnace |
| Phobos' Rivetline F6-R Exterior Radiator | Separate six-by-four heat rejection equipment |
| Phobos' Rivetline F6-P Thermal Exhaust Port | One-tile sealed deck fitting and complete underside radiator assembly |
| Phobos' Rivetline F6-S Furnace Assembly Section | Eighty-kilogram construction section |
| Phobos' Rivetline C2 Residue Collector | Two-wide collecting endpoint |
| Phobos' Rivetline D4-S Dismantling Fixture Assembly Section | Processor construction section |
| Phobos' Rivetline R4-S Scrap Reclaimer Assembly Section | Reclaimer construction section |
| Phobos' Asterel N0 Approach Assist (Prototype) | Older, opt-in development prototype |

N and C identify navigation/control electronics; D, G, H, R and C identify the
industrial product roles. The industrial digits reflect the current equipment's
width; `-S` denotes construction sections. They are model designations, not
configurable capacity, price or saved recipe revision numbers.

Intact, damaged, installed and loose forms retain the same maker/model. Internal
feeds belong to their parent machine. Residue packets use **Phobos' Rivetline**
and a descriptive material name; R2 still means recipe revision 2, not a new
chemical assay. Auto Nav offcuts/residue retain **Phobos' Asterel N1** provenance.
Shared mixed service waste is **Phobos' Spent Service Parts (0.5 kg)**: it has no
invented model or single manufacturer because multiple machines produce it.

## Framework pattern and localization

Framework's optional `Localization.EquipmentNames` applies a fixed maker/model
prefix to a translated type/variant. Each content mod owns
`mods/<Mod>/framework/equipment-names.json`, embedded in its assembly. Its keys
are existing translation keys; values contain `brand` and `model` strings.
An empty model is permitted; an empty brand/model pair gives just `Phobos'`.

Register the catalog with the four-argument `Translations.Register(owner,
assembly, englishResource, equipmentNamesResource)` overload. Normal catalog
lookups, construction registration and UI calls all receive formatted names.
The original overload remains available for consumers without branding.

For registered name keys, English and contributed catalogs contain only the
equipment type/variant, retaining any numbered placeholders. For example,
`Polaris Auto Nav Module` becomes `Phobos' Asterel N1 Polaris Auto Nav Module`.
Translators may reorder words within that descriptor; the maker/model prefix
stays fixed. Older full-name translation overrides beginning with Phobos or the
same maker/model are rejected with an English fallback and diagnostic, avoiding
doubled branding. Descriptions and ordinary messages remain complete messages.
See [localization](localization.md).

Native JSON and construction recipes retain fully rendered English fallbacks;
the build checks compare them to Framework's formatted catalog results. Keep
those fallbacks synchronized when changing a descriptor or model. Titles may
use a shorter descriptor, with the same prefix, maker and model. Existing panel
art contains no baked-in names, so this change needs no replacement sprites.
Historical art previews and research retain their recorded wording.

## Compatibility and verification

This is a display-name change. Object, overlay, recipe and translation IDs,
save-property keys, commands, package names, economics and material quantities
are unchanged. Native definitions supply the default names when objects load;
player-assigned names remain governed by native rename handling. No save files
are edited and no equipment is replaced to rename it.

The older Approach Assist stays independent of Framework: its native display
names are updated and its panel reads the native name. It remains a prototype,
not an alternative to N1's current flight and docking features.

Build checks cover localized naming, variant placeholders, legacy override
fallback, naming metadata errors, native/recipe fallback agreement and existing
gameplay/persistence rules. In-game text fit and renamed existing objects remain
for owner verification. Prepared packages are not an installation claim.
