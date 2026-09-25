# Equipment brands and models

Owner memorandum enacted in Framework 0.12.0, Shipbreaker 0.10.1 and Auto Nav
0.8.1. Every full equipment name starts with **Phobos'**, including the apostrophe.
These are original project brands, not existing game manufacturers.

**Asterel** makes navigation and control electronics. **Rivetline** makes salvage
machinery and material-handling equipment. Share these families where equipment
has a related purpose; new brands should serve a distinct equipment identity.

**Verdemorrow Agronomics** is Agriculture's separate fictional manufacturer.
Its short brand, **Verdemorrow**, combines the intended associations of *verdant*
growth and *tomorrow*: carrying the possibility of a lasting home into space.
Its voice is hopeful and practical, with equipment that belongs aboard a working
ship. Brand line: **“Where we go, life grows.”** This identity does not represent
a real company, seed cultivar, research programme or institutional endorsement.

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
| Phobos' Verdemorrow Firstlight-4 Cultivation Rack | Four-by-four finite cultivation rack |
| Phobos' Verdemorrow Hearth-2 Galley Cooker | Two-by-two portion cooker |
| Phobos' Asterel N0 Approach Assist (Prototype) | Older, opt-in development prototype |

N and C identify navigation/control electronics; D, G, H, R and C identify the
industrial product roles. The industrial digits reflect the current equipment's
width; `-S` denotes construction sections. They are model designations, not
configurable capacity, price or saved recipe revision numbers.

## Verdemorrow product families

Owner direction, 25 September 2026; applied in Agriculture 0.1.1. Firstlight
expresses starting a living crop far from Earth; Hearth expresses food and home;
Continuance expresses retaining the next generation; Groundwork expresses the
material foundation for growth. These are fictional product families, not new
biological traits or promises of self-sufficiency.

| Family | Current full display name |
| --- | --- |
| Cultivation machinery | Phobos' Verdemorrow Firstlight-4 Cultivation Rack |
| Food-preparation machinery | Phobos' Verdemorrow Hearth-2 Galley Cooker |
| Planting stock | Phobos' Verdemorrow Continuance Seed Potato (0.2 kg) |
| Planting stock | Phobos' Verdemorrow Continuance Lettuce Seeds (5 g) |
| Formulated nutrients | Phobos' Verdemorrow Groundwork Formulated Crop Nutrients (40 g) |
| Produce | Phobos' Verdemorrow Raw Potatoes (0.4 kg) |
| Prepared food | Phobos' Verdemorrow Hearth Cooked Potatoes (0.4 kg) |
| Produce | Phobos' Verdemorrow Lettuce (0.25 kg) |
| Retained biological matter | Phobos' Verdemorrow Crop Residue |
| Retained process liquid | Phobos' Verdemorrow Agricultural Process Solution |
| Equipment dismantling remainder | Phobos' Verdemorrow Agricultural Housing Waste |

Firstlight-4 and Hearth-2 have model numbers reflecting equipment width.
Seeds, nutrient blends and meals use named product lines without machine numbers;
ordinary produce and waste use the brand and a literal description. Future plant
variety names must not imply a real cultivar or researched tolerance unless that
specific biological claim is supported separately. Keep manufacturer/product-line
names fixed across translations and translate the functional descriptions.

The owner confirmed Agriculture has never been used. Its first identities now
use `PhobosVerdemorrow...` for equipment, supplies and construction recipes, with
no migration aliases for the earlier unreleased candidates. The Phobos Agriculture
package and commands retain their product-level names. Other mods keep their own
brands and saved identifiers.

Intact, damaged, installed and loose forms retain the same maker/model. Internal
feeds belong to their parent machine. Shipbreaker residue packets use **Phobos' Rivetline**
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

Agriculture 0.3.0 adds **Phobos' Verdemorrow Groundwork Irrigation Charge (5 kg)** to the Groundwork supply family; it is finite crop water, not a drinking-water product.

Agriculture 0.4.0 adds **Phobos' Verdemorrow Groundwork W2 Water Supply Unit** and
**Phobos' Verdemorrow Groundwork Irrigation Conduit**. W2 is the supply appliance
model; the ordinary conduit has no artificial model designation. Names use the
shared equipment catalog and remain independent of native saved IDs.

Shipbreaker 0.16.0 adds **Phobos' Rivetline F6-C Sealed Coolant Conduit** to the
F6 family. It is a twin-channel industrial thermal connection, distinct from
Groundwork irrigation despite reusing the project's original fitting artwork.
