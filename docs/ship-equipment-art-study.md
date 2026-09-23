# Ship equipment art study: a direction for Phobos Shipbreaker

**23 September 2026 — source-asset study, with in-game appearance still to confirm.**

Shipbreaker should look like a piece of installed industrial equipment: an
overhead assembly with recognisable working parts, mounting points and access
space. The AutoNav faceplate is a useful reference for our eventual control
window, but it is a poor primary reference for the machine on the ship's floor.

My earlier suggestion to carry the blue-grey casing and worn finish directly
across was too narrow. Native ship equipment includes cream cabinets, black
electrical cases, blue regulators, coloured pumps, white/yellow generator parts
and red bellows. Its common language is **functional shape, small readable colour
regions and layered rendering**, rather than one universal paint scheme.

## Evidence and limits

I inspected 22 vanilla equipment definitions and their local image files:
power storage, life support, thermal control, RCS, navigation furniture, a
turbine lifter, fusion equipment and cargo storage. I compared colour, normal,
damage and loose-object images, and checked the existing local inspection of
the game's image-loading and item-rendering code.

This is the installation used for the project's **Ostranauts 1.0.1.4** research.
Its current game assembly fingerprint still matches that earlier inspection.
No new game session was launched. This sample is deliberately varied, not an
exhaustive catalogue of all equipment or proof that every stored asset is used
unchanged in the current game. Workshop overrides and runtime state can alter
what the player sees.

One unresolved case matters: the selected **air pump's definition points to a
magenta `Debug` image**, although a plausible ordinary `ItmAirPump02Off.png`
also ships with the game. The study sheet explicitly labels that latter image
as a candidate. I have not established the runtime replacement path and do not
treat the magenta image as the game's intended style or claim that the pump is
broken in play.

The findings below distinguish **observed source evidence**, **design inference**
and **questions for screenshots**. The local reference sheets show source
textures on a neutral background, enlarged exactly four times without smoothing.
They do not simulate the game's lights, materials, wear or surrounding floor.

## What differs from the autopilot artwork

| Aspect | Approved AutoNav work | Ship equipment evidence and implication |
| --- | --- | --- |
| View | Large front-facing interface plate; very small cassette item | Overhead world object whose outline and internals convey its purpose. Avoid a perspective product photograph or a large front panel lying on the floor. |
| Composition | Broad faceplate surrounding display/control regions | Several connected functional masses: housings, coils, pumps, ducts, sockets and supports. Asymmetry and transparent gaps are common. |
| Resolution | Large UI texture, with separate 16 × 16 cassette exports | The sampled world textures use 16 image pixels per tile. Detail must work in tens of pixels, not only in a large preview. |
| Depth | Subtle painted bevels on the interface; a neutral normal map for the cassette | All 22 sampled definitions reference an existing normal map. These encode shape for lighting separately from colour. A flat normal map is a weak final choice for substantial machinery. |
| Condition | Approved intact/damaged cassette pair | Many machines have separate damage texture inputs and condition-driven rendering. Wear is not simply a permanent scratched finish painted over everything. |
| Installed versus loose | A small module placed into a console | Several loose machines are repacked into a beige edged/crate-like shape; disconnected parts may be rearranged. Installed and transport forms need separate consideration. |

**Inference:** a generated image can establish the design, but a beautiful large
illustration alone will not make a convincing game asset. Its silhouette,
64-pixel export and lighting support must agree.

## Equipment observations

Dimensions below describe texture dimensions and rectangular item-grid bounds.
They are **not** claims that every cell is obstructed: several definitions have
blank or specialised socket cells inside their bounds.

| Equipment sampled | Texture / grid bounds | Observed visual language | Useful lesson for Shipbreaker |
| --- | --- | --- | --- |
| Ship battery; compact battery | 32 × 32 / 2 × 2; 16 × 32 / 1 × 2 | Dark box, terminal/fastener dots, sparse yellow details; very restrained colour texture | A few well-placed details can read better than dense grime. Use a dark electrical enclosure as a supporting component. |
| CO2 and contaminant scrubbers | 48 × 32 / 3 × 2; 32 × 16 / 2 × 1 | Cream/grey housing, exposed piping and fins, contrasting small controls; irregular outlines | Strong references for serviceable equipment assembled from distinct parts. |
| Air pump, candidate Off image | 16 × 48 / 1 × 3 | Narrow mechanical assembly with a compact control end | Useful proportions, but current on-screen appearance needs a screenshot because of the Debug-reference discrepancy. |
| Cooler and heater | 64 × 48 / 4 × 3; 32 × 32 / 2 × 2 | Cooler has repeated pale fins and a dark central mechanism; heater has an X-braced form with red elements | Repeated mechanical structure communicates function. A service enclosure can occupy only part of a machine's outline. |
| Miura Hydra and Kang 2202 RCS regulators | 48 × 48 / 3 × 3; 32 × 32 / 2 × 2 | Blue angular housing with multiple couplings versus a darker, more compact bent outline | Similar functions do not require identical colour or silhouette. Connections help explain equipment. |
| RCS thruster assembly | 16 × 48 / 1 × 3 | Brass/ochre central body, dark fittings, small contrasting ends | Distinct material regions and connector ends remain readable at small size. |
| Navigation console as a world object | 48 × 48 / 3 × 3 | Dark wraparound console and seat, with a clear occupied/working arrangement | Even the nav station's world art is very different from its full-screen control UI. |
| Turbine lifter | 64 × 64 / 4 × 4 | Large circular fan beneath horizontal grille bars in a square deck | Excellent size reference, but its fan would falsely describe Shipbreaker as ventilation machinery. This is the current placeholder. |
| Compact IC fusion reactor | 48 × 48 / 3 × 3 | Red rectangular enclosure, central warning mark, two strong circular details | Hazard marks can be broad and readable without covering the machine in stripes. |
| Sulaiman reactor core | 80 × 80 / 5 × 5 | Blue circular centre, surrounding octagonal housings, red connecting sections, small peripheral equipment | A clear hierarchy: dominant working form, repeated subassemblies, then tiny details. |
| Fusion core pump and cryo pump | 16 × 48 / 1 × 3; 32 × 48 / 2 × 3 | Green pump body or blue/yellow pair of mechanisms, red bellows, grey couplings | Different components are identifiable by form and local colour. Much of their raw colour art is flat. |
| MHD generator | 48 × 48 / 3 × 3 | Paired white/yellow banks, connecting dark/red lines, projecting grey/red neck | Particularly useful for a recognisable industrial assembly without an all-enclosing box. |
| Field-coil assembly | 80 × 80 / 5 × 5 | Dark circular area, four blue coils in yellow frames and a central component | Large dark working areas can anchor several bright, clearly separated parts. |
| Fuel regulator | 48 × 80 / 3 × 5 | Strong yellow upper assembly, extended neck and red/grey joints | Functional paths can dominate the silhouette; shape should explain where material or power travels. |
| Fusion laser array and pellet feeder | Both 16 × 48 / 1 × 3 | Different red/yellow heads attached to similarly organised bellows and couplings | Shared connection language can make a family coherent while the heads remain distinct. |
| Cargo pod | 48 × 96 / 3 × 6 | Large coloured panels, contrasting frame/straps, corner details and top attachment | Useful for containment and handling cues; not a reason to make the processor a sealed cargo box. |

The displayed colours are observations of the source files. Their apparent
brightness and saturation in a ship cannot be calibrated from these figures.

## The rendering facts that affect art production

### Scale and pixels

All 22 sampled texture sizes match **16 pixels per grid-bound tile**. The
inspected ordinary item path also calculates visual scale from texture size
divided by 16, and the PNG loader selects point filtering.

For our existing **4 × 4 machine**, a **64 × 64 world image** is therefore the
appropriate starting export. A 1024-pixel generated concept must not be plugged
straight into that world-image field. Keep the intended four-tile footprint and
evaluate the reduced image as part of art preparation.

At this scale, a fastener may be one or two pixels. Feed rails, a working head,
the processing bed and the collection area deserve the available pixels before
tiny warning text or decorative cables. A large portrait may add clarity, but
cannot compensate for an unreadable world sprite.

### Colour, relief and light are different inputs

The source comparison is revealing: the turbine lifter and several fusion
components have quite flat colour areas, while their normal maps contain much
more information about recesses, curved surfaces and edges. Other equipment,
including the older scrubber artwork, already has more tonal detail in its
colour image. There is variation within the game's art, not one flatness rule.

The inspected world-material path binds `strImg` as colour and `strImgNorm` as
the normal input; normal PNGs go through the game's normal conversion step.
Seventeen of the sampled definitions also declare separate light entries.
These are distinct from a bright patch painted into the colour image.

**Design inference:** retain enough local shading to describe materials, but
avoid a dramatic fixed studio light, a long cast shadow painted into the sprite,
or all-over polished bevels. Plan a normal map shaped to the machine. Its final
strength and the amount of shading baked into colour need an in-game reference.
Do not infer that a bright painted indicator proves the machine is powered.

### Damage should describe a failure

Twenty of the 22 sampled definitions reference existing damage textures. The
battery and scrubber examples include local breaches, darkening and exposed
internals; the MHD generator has broken/disturbed structure. The damage image
is an input to a condition-dependent material, not evidence that all of that
damage is always visible.

For Shipbreaker, useful damage cues would be a bent feed guide, a damaged working
head cover or a local electrical burn. Maintain registration between intact and
damaged parts. The current prototype assigns the same turbine colour image to
all four machine variants, so it does not yet offer a meaningful visual damaged
state.

### Transport forms can change

The battery, MHD generator and turbine lifter have visibly different loose
images. The generator's parts are packed more compactly; beige borders resemble
transport packaging. The native turbine lifter even changes from 4 × 4 installed
to 3 × 3 loose in its definitions.

That is evidence of the game's convention, **not a proposed size change for our
mod**. Our machine and assembly section are currently 4 × 4. Keep those gameplay
dimensions and stable identities while deciding whether the loose artwork should
look braced, strapped or partially folded within that same footprint.

## Recommended Shipbreaker design direction

This is a proposed brief, not an approved final design or new artwork.

**Form:** a low industrial dismantling fixture seen from directly above, with
one dominant rectangular working bed and recognisable mechanisms around it.
It should read as a place where a panel is held and separated, rather than a
reactor, fan, furnace or generic computer cabinet.

**Functional layout:** show a shallow feed rack/guide, clamping or holding points,
one compact processing head or cross-member, a small service enclosure, and a
distinct collection recess. Keep the operator's approach edge visually clear.
The exact facing should be checked against the installed interaction position.
Do not paint four permanent wall panels or a permanently loaded output heap
onto a machine that may be empty.

The **8 × 8 output tray is an inventory capacity**, not an eight-by-eight-tile
external structure. All of the machine's physical features must still fit its
4 × 4 installation. Similarly, the four-panel feed is capacity, not permission
to draw an extra machine-sized rack outside the footprint.

**Materials and colour:** start with a dark working bed and metal mechanisms,
one distinguishable painted service cover and restrained functional accents.
Cream, grey and limited blue-grey are all supported by the references. Small
ochre/yellow guards could retain some Phobos continuity. A uniformly blue-grey
box is not necessary; exact colour balance remains open until screenshots.

**Wear:** use restrained baseline handling wear. Reserve major damage for the
damaged state. The quantity of grime in the raw source textures varies, so
"very weathered everywhere" is not an evidence-based requirement.

**Branding:** a compact identifier or colour cue can carry Phobos identity.
Readable names, values and status remain live interface text. The existing
AutoNav faceplate can guide a later control-window design without dictating
the machine's body.

**Implementation consequences, when art is approved:** replace the turbine
references for installed/loose and damaged forms; supply matching colour and
normal inputs; set damage and portrait references deliberately; give the
assembly section and retained residue their own clear appearances. The input
bin is inherited from Salvage Workshop and should also be reviewed so a native
or Workshop-looking container does not inadvertently dominate the new object.
No gameplay or art-integration code was changed during this study.

## What screenshots would resolve

The source files are sufficient to establish scale, component layout conventions
and the need for normal/damage support. I am **not yet confident about the final
in-game shading, material finish and visual density**. Those are the reasons to
ask for screenshots before generating the final look.

Two ordinary play screenshots would be enough to begin:

1. **An engineering or utility area at your usual playing zoom**, showing several
   installed machines, some floor and preferably a crew member for scale.
   Scrubbers, a cooler, batteries, RCS equipment or a reactor installation are
   useful; there is no need to gather every example.
2. **A closer view of one or two substantial machines** you consider especially
   representative of the game's look. A scrubber/cooler and an exposed fusion
   assembly would give a useful contrast. Include a selected-item name if the
   equipment comes from a mod or is hard to identify.

Optional, only if readily available: an already damaged or loose machine, or
the air pump as it actually appears. Do not damage equipment, rearrange the ship
or stage a special test for this report. Ordinary screenshots with the existing
lighting and UI are fine. An intact/damaged pair or a different lighting view
would be extra evidence, not a prerequisite checklist.

**Next art step:** use those views to settle the lighting/detail target, then
generate one intact fixture concept and inspect its 64 × 64 silhouette before
deriving the matching variants. AutoNav's approved artwork remains unchanged.

## Sources and reproducibility

Primary evidence is the owner's local game installation, inspected read-only:

- `Ostranauts_Data/StreamingAssets/data/items/*.json` and
  `data/condowners/*.json`: image references, names, dimensions and state variants.
- `Ostranauts_Data/StreamingAssets/images/`: colour, normal, damage and loose
  textures for the named equipment. The inventory records exact image names
  and SHA-256 fingerprints.
- Previously inspected local `Item.SetData`, `Item.SetUpInventoryMaterial`,
  `DataHandler.LoadPNG` and `DataHandler.GetMaterial`: scale, point filtering,
  light declarations and separate material inputs. Game assembly SHA-256:
  `1DC1858A8EDC514EC089F2FD7C55932C7F9B62B0A96201C15E2B2720122A03A7`.
- [Current Phobos definitions](../src/PhobosShipbreaker/Content.cs),
  [fixture controls](../src/PhobosShipbreaker/FixturePanel.cs) and
  [approved AutoNav art](../assets/phobos-autonav/README.md): what our current
  prototype actually implements and how the previous artwork differs.
- Salvage Workshop 0.8.71's `SWB_SorterInstalled` definition was checked only as
  our implementation dependency: its 2 × 2 sorter art is **not** counted as a
  vanilla art sample or treated as the primary style authority.

Generate the local figures with Python and Pillow:

```powershell
python scripts/inspect-equipment-art.py --game-path '<local Ostranauts folder>'
```

The script reads selected definitions and creates an inventory plus three study
sheets under ignored `.local/research/equipment-art/`. It changes no game files.
The references below are local research artifacts and will appear only after
running it. Game-derived pixels and decompiled sources are not included in the
versioned report, our mod artwork, packages or public distribution.

### Local reference sheets

- [Utilities, power storage, RCS and the current placeholder](../.local/research/equipment-art/equipment-1.png)
- [Fusion equipment and cargo storage](../.local/research/equipment-art/equipment-2.png)
- [Colour, normal, damage and loose-state comparison](../.local/research/equipment-art/equipment-states.png)
- [Source inventory and fingerprints](../.local/research/equipment-art/inventory.json)
