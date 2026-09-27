# Bulk agricultural storage: artwork audit and pilot brief

27 September 2026. **Research only: no production imagery generated.**
The [R3 proposal](agriculture-bulk-storage-design.md) is not registered equipment.
Native references belong to **Blue Bottle Games, [Ostranauts](https://bluebottlegames.com/ostranauts)**.
Inspection is local; screenshots of these sheets must not enter a distributed
mod package or be supplied to an external generation service.

## Actual references inspected

The repeatable audit reads native `data/items` and `data/condowners`, resolves
their image names under `StreamingAssets/images`, and records image dimensions,
alpha bounds, colour/normal/damage names and SHA-256 hashes. It includes twelve
native references and three existing Phobos assets, plus eighteen colour/map
comparison cells. All six generated sheets were visually inspected at 1x/4x.
They are neutral-background source comparisons, not simulated game lighting.

| Native identity | Colour / normal / damage image names | Native dimensions | Reuse conclusion |
|---|---|---|---|
| `ItmCanister01` | `ItmCanister01` / `ItmCanister01n` / `ItmCanister01Dmg` | 16 x 16 each | Shape/normal reference. Its orange top-down cylinder reads well, but is too small to stretch into a 3 x 3 reservoir. |
| `ItmCanister01Loose` | `ItmCanister01Loose` / `ItmCanister01Loosen` / `ItmCanister01DmgLoose` | 16 x 16 each | Beige transport frame is a useful visual convention; retain a distinct original R3 loose form. |
| `ItmCanister01Dmg` | `ItmCanister01Dmg` / `ItmCanister01n` / no further damage image | 16 x 16 | Local buckling/open damage, not a uniform brown overlay; research reference only. |
| `ItmCanisterLH02` | `ItmCanisterLH02` / `ItmCanisterLH02n` / none | 48 x 48 | Strong reservoir reference, but baked D2O lettering makes unchanged reuse unsuitable. |
| `ItmCanisterLH02Loose` | `ItmCanisterLH02Loose` / `ItmCanisterLH02Loosen` / none | 48 x 48 | Clear transport framing; still carries baked D2O identity. |
| `ItmCanisterLHe01` | `ItmCanisterLHe01` / shared `ItmCanisterLH02n` / none | 48 x 48 | Cryo lettering, bright blue frame and cold symbol imply the wrong function. Do not reuse unchanged. |
| `ItmCanisterO2Small` | `ItmCanisterO2Small` / `ItmCanisterO2Smalln` / `ItmCanisterO2SmallDmg` | 16 x 16 | Readable valve/body silhouette, but oxygen-bottle convention would misidentify dry nutrients. |
| `ItmFusionCorePump01` | `ItmFusionCorePump01` / `ItmFusionCorePump01n` / `ItmFusionCorePump01Dmg02` | 16 x 48 | Green body, red bellows, discrete grey coupling; reference for fittings, not an added R3 pump. |
| `ItmFusionCryoPump01` | `ItmFusionCryoPump01` / `ItmFusionCryoPump01n` / `ItmFusionCryoPump01Dmg02` | 32 x 48 | Contrasting mechanisms explain connections; full assembly falsely implies cryogenic equipment. |
| `ItmConduit00` | `ItmConduit00Sheet` / `ItmConduit01Sheetn` / none | 64 x 64 atlas | Raw colour sheet is magenta; native shader/sheet selection matters. Not a ready-painted agricultural pipe. |
| `ItmCargoPod01` | `containers/ItmCargoPod01` / `containers/ItmCargoPod01n` / none | 48 x 96 | Panel/frame/strap hierarchy; silhouette too large and cargo-like for this R3. |
| `ItmAtmoScrubber01` | `ItmAtmoScrubber01` / `ItmAtmoScrubber01n` / `ItmAtmoScrubber01Dmg02` | 48 x 32 | Good cream housing, dark couplings and service-control reference. Avoid borrowing the whole scrubber. |

Important geometry finding: installed D2O/cryo definitions have a **7 x 7 socket
array with a 48 x 48 picture**, whereas the loose D2O has a 3 x 3 array. Texture
size is not permission to copy their collision/socket/attachment definitions.
R3 must use its own reviewed 3 x 3 construction and service geometry. The earlier
equipment study's 22 matched examples did not include these tanks.

Existing Phobos comparators are `WaterSupply.png` and `Workup.png` (both 32 x 32)
and `WaterPipe.png` (16 x 16), under Agriculture's image directory. W2's muted
teal round vessel and grey service column make the best family reference; B2's
bronze frame is a secondary material cue. R3 should preserve native broad shapes
and restrained highlights, not increase grime/detail merely to look industrial.

## Reuse and generation requirements

| Asset/element | Classification | Production decision |
|---|---|---|
| R3 installed shell | **New original asset** | One 48 x 48 export from at least a 96 x 96 retained master; circular/rounded sealed vessel inside a low square frame; muted green service column on +X, access on -Y; no fuel/cryogenic text. |
| R3 loose shell | **New original asset** | Registered transport-frame layer around the same vessel; 48 x 48 export, same pivot and transparent margins. |
| R3 damaged installed/loose | **New original asset** | One registered dent/isolated-service damage layer, two deterministic composites; no animated liquid plume or explosion. Colour and normal variants must agree. |
| Native-style lighting | **New original asset** | Original curved-shell normal maps matching the chosen silhouette; native normals are local references, not redistributed maps or a generic flat replacement. |
| W2 machine artwork and downstream pipes | **Reuse unchanged** | Existing approved Phobos exports and native sheet behaviour; no network/art replacement. |
| Direct-coupling fitting | **Runtime composition** | Reuse the existing Phobos `WaterPipe` appearance as a visual donor only, registered to the selected R3/W2 touching edge. No new conduit item, electrical connection or painted fictitious route. If native overlay registration cannot be isolated, include an original fitting in the R3 service layer instead. |
| 500 g nutrient charge | **Reuse unchanged** | Existing original `stock-nutrients` exported supply picture and normal support; new item name/mass distinguishes size, no misleading oxygen cylinder. |
| R3 inventory picture / picker picture | **Reuse unchanged** | The appropriate R3 loose/world derivative when authored, using existing portrait lookup; no separate illustration required. |
| Panel portrait larger detail | **Unnecessary** | Scale the world derivative through the existing picture adapter. Do not claim nearest-neighbour enlargement creates detail. |
| Return-container family | **Unnecessary** | Deferred machinery; current recorded-drainage and reject pictures suffice for the existing chain. |
| R3 dismantling remainder | **Reuse unchanged** | Framework `MaintenanceDefinitions.Remainder` already references the native `ItmScrapTrash` presentation at runtime; new R3 masses/IDs, no new waste sprite or crop-nutrient assay. |
| Clean water versus retained catch | **Runtime composition** | Live labelled quantities/status on panels; distinct normal/isolated service indicator if renderer permits. Colour alone is not the status. |
| Buttons, selectors, numeric fields, gauges, frames | **Reuse unchanged** | Existing Framework compact shell and audited native donors. No new raster control-panel faceplate. |
| Fill-height animations, flowing-liquid loops, spill/gas effects | **Unnecessary** | No exposed liquid level or native atmospheric chemical release exists in the first slice. |

Only the R3 body/transport/damage/normal family is a justified new production-art
request. Start with one representative installed base, not a full set of generated
variants. New smaller fitting masters need 4x per axis when their intended short
side is at most 32 pixels. Larger assets need at least 2x. Retain larger originals;
do not upscale a small donor and call it a higher-detail source.

## Runtime loading and control isolation

Native world items name colour/normal inputs through `JsonItemDef.strImg` and
`strImgNorm`; the current item/material path uses `DataHandler.LoadPNG` and
`DataHandler.GetMaterial`, including point filtering and normal conversion.
Framework `ObjectPresentation.Picture` already resolves native/Phobos picture
names and supplies a deliberate neutral fallback. R3 can use that path unchanged.

`GUIStationRefuel` loads `GUIShip/GUIStationRow` at runtime. The inspected
`GUIStationRow.Awake` attaches a slider listener; `Init(..., Action)` adds another
listener when given a callback, and `OnSliderChanged` changes the native refuelling
audio flags. Thus the **whole row controller is not an inert visual donor**.
Repeated initialization must not accumulate callbacks. Prefer existing Framework
numeric/selection widgets with native appearance. Any extracted visual subtree
must be instantiated under an inactive owned parent, stripped of gameplay events,
and checked for external references before activation.

Reuse already-audited `NativeInstruments` only if a real R3 reading needs one.
Its compatibility check is pinned to the inspected assembly. Relevant existing
donors include reactor LED artwork for genuine fill fractions and air-pump
font/frame visuals documented in the [furnace audit](furnace-ui-and-art.md).
No pressure/temperature dial is justified for an unsimulated variable. No full
reactor, station or air-pump controller is cloned. Standard widgets remain the
fallback when native donors fail audit or resources are unavailable.

## First pilot and acceptance

The production brief is one top-down sealed R3 body, separate +X service assembly,
transport frame, damage layer and registered normal support. Use the new original
body rather than covering baked D2O text with an unverified overlay. Palette:
off-white/grey containment, W2-like muted green/teal service surface, dark joints,
very sparse amber service cue. No institutional logos, printed readings, studio
cast shadows, isometric perspective or exaggerated glossy bevels.

Set 48 x 48 runtime registration, centred pivot and the blueprint's exact coupling
points before generation. Keep maps and layers on one canvas. Follow the existing
[generation](asset-generation-policy.md) and [resolution](artwork-resolution-policy.md)
policies: simple layers preferably PixelLab; a high-resolution original base via
image generation only if it helps. No mandatory dual-provider pass. Provider,
prompt, source/master hashes, costs, licence terms, crop/pivot and repeatable export
settings belong in the future asset manifest; do not invent records now.

Review the pilot beside W2/B2 and the native reservoir references at **1x and 4x**,
then in all four rotations against its proposed tile/access geometry. Check the
loose/intact/damaged distinction, silhouette, palette and pixel-cluster agreement.
Normal-map strength and game lighting remain owner-run Unity checks; the current
raw-map sheets establish alignment and source conventions only. Check panels at
1080p, 1440p and 3440 x 1440, increased UI scale, long names/translations and missing
artwork. Expand to the remaining state family only after the pilot passes.

## Reproduce the local sheets

Use the existing Pillow-capable Python runtime, with the locally configured game
path (no machine path belongs in tracked files):

```powershell
$game = (Get-Content .local/install-settings.json | ConvertFrom-Json).OstranautsPath
python scripts/inspect-equipment-art.py --game-path $game --profile agriculture-bulk --output .local/research/agriculture-bulk/art
```

Outputs: `bulk-1.png` through `bulk-3.png`, `bulk-maps-1.png` through
`bulk-maps-3.png`, and `inventory.json`. The script refuses output outside ignored
`.local`. Paths/names in this document are factual lookup metadata; game pixels
and decompiled controllers remain local. Reuse references at runtime where
appropriate; do not distribute extracted vanilla textures, edited copies or
third-party mod artwork. Art provenance is separate from the scientific citations
in the research report. No licence or provider endorsement is implied.
