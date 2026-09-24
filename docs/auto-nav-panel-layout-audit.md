# Auto Nav panel layout audit

Inspected **Ostranauts 1.0.1.5** after the owner's **Auto Nav 0.4.1** screenshot
showed an oversized panel with placement margins. Auto Nav 0.4.2 corrected its
height; follow-up screenshots of 0.4.3 confirmed it remained too narrow.
**Auto Nav 0.5.0** now matches the standard native column width as described below.
The latest runtime correction remains owner-tested work.

## Vanilla evidence

Read the installed game's serialized navigation prefabs in `resources.assets`,
the interaction-panel host in `level2`, native `guipropmaps`, and the navigation
loader, drag and fit-check methods. `scripts/inspect-nav-layout.py` produces a
local metadata audit with source hashes, geometry and component names. It needs
UnityPy (inspected version 1.25.3); it is an optional research tool, not a runtime
or build dependency. Extracted data and decompiled code stay outside Git.

| Vanilla panel | Container anchors, min → max | Board width × height |
| --- | --- | --- |
| Time / Zoom | (0, 0.8) → (0.25, 1) | 25% × 20% |
| Display Controls | (0.25, 0.8) → (0.5, 1) | 25% × 20% |
| Control Toggle | (0.5, 0.8) → (0.6, 1) | 10% × 20% |
| Flight Dynamics | (0.65, 0.4) → (0.9, 0.6) | 25% × 20% |
| Mooring Control | (0.65, 0.4) → (0.9, 0.6) | 25% × 20% |

These are default prefab footprints, not a claim that all five occupy those
locations simultaneously. Module roots stretch across the board. Their
`Container` children use normalized anchors, zero size offsets and centred
pivots. The native drag handler lives on that same container; its `bg` child
provides fit feedback. The interaction host uses an aspect fitter (about 2.0786)
with horizontal insets, so the navigation board is approximately **1.966:1**,
not the full screenshot's aspect ratio. Read the actual board rectangle at runtime
rather than treating screen pixels or a 16:9/ultrawide window as the panel canvas.

The loader applies saved or default anchors before validating placement. Native
dragging and saving round anchor coordinates to **two decimal places**. Fit
checks reject rectangles outside the board or overlapping active modules. Native
Edit mode deliberately holds the simulation paused until closed.

## Defect and correction

Our earlier default was **30% × 25%**. A child `AspectRatioFitter` then fitted the
2:1 artwork inside that larger rectangle. At a 1.966:1 board, the placement
rectangle was about 2.36:1: visible artwork and native placement bounds disagreed.
The old panel was also taller than the common 20% native row. Fixing the drag
component in 0.4.1 exposed this separate geometry problem.

Version 0.4.2 used **20% board height**, deriving width from that physical height
and the approved **2:1 faceplate**, and stretches the artwork across that exact
container. On the inspected board the resulting width is about **20.34%**. The
faceplate, buttons and native placement rectangle share the same bounds; there
is no child aspect fitter or artwork resampling. Labels remain localized live
text with automatic sizing. Approved image files are unchanged.

The owner subsequently confirmed that this height was right but the width did
not fill the standard column: the 2:1 bitmap constraint produced about 20.34%
width beside vanilla's 25%. This was an incorrect choice of layout constraint,
not a screenshot-resolution problem.

Version **0.5.0** uses **25% board width × 20% board height**, exactly the common
native footprint. It keeps the approved PNG byte-for-byte unchanged and uses a
runtime sliced Image: corner regions (including screws) retain uniform scaling
based on the existing height, while the centre and horizontal edges widen. This
uses the same Unity sliced-image mechanism as our industrial panel. The overall
panel aspect now follows the native column; it is no longer forced to 2:1.
Live labels and button hit areas remain within the same visible rectangle. The
runtime-created sprite is destroyed with the panel; the shared texture is retained.

The board-relative row and column determine size. The native rounded-anchor boundary
normalizes only `AutoNavPanel` instances before fit checking and saving. This is
necessary because the loader overwrites prefab sizes with saved/default anchors.
It handles both intact and damaged modules and old instance defaults. It keeps
the top-left position, changes only our panel's dimensions and leaves vanilla
overlap/out-of-board checks in charge. It does not clamp an invalid drop, displace
other panels or force our module on top of existing equipment. Native anchor
rounding still has its usual one-percent grid precision.

No save files are edited. Reopening rebuilds our panel with corrected geometry;
the game's normal layout saving records its placement. If the native layout
rejects that position, place the module again in Edit mode. A fully occupied
board needs rearrangement or removal of another panel before Polaris will fit.

## Verification

Geometry checks cover measured board proportions at multiple scales, other
parent aspect ratios, legacy footprint reduction, preserved top-left position,
25% × 20% bounds, repeat reload rounding without drift and invalid dimensions.
The package's compiled-component check rejects a child aspect fitter and requires
the bounds hook as well as the correct native navigation drag component. These
checks do not execute Unity's UI/event lifecycle.

Owner check after installation: use Edit to place Polaris in a clear area; compare
both its width and height with Time / Zoom or Display Controls; leave Edit, reopen the console
and confirm size and position persist. Check text readability at the usual UI
scale and normal interaction after leaving Edit. No new module needs spawning
solely for this update.
