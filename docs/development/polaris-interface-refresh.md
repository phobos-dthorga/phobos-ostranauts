# Polaris interface refresh

Prepared on 27 September 2026 for Framework 0.30.0, Auto Nav 0.21.0 and
Shipbreaker 0.29.0. Agriculture remains 0.15.2; Manufacturing remains held.
Offline checks and browser previews are separate from Unity interaction approval.

## Using the revised panels

The Flight Hub keeps its existing footprint, native Edit placement and rescue
overlay. Its six tabs now occupy two rows of three. Selected tabs have a gold
underline and bold label; buttons have visible edges and hover, pressed and
disabled states. Navigation target, fire target, operating state and warning
remain separate readouts. **Details** beside the warning opens its full text.
Departure uses the same button sizes as the other commands, with instructions
below them in a scrolling area. Resume, Disengage and Cease Fire remain outside
the scrolling body and group dialogs.

The industrial console uses the same button treatment. Navigation and the fixed
action area wrap at narrow widths; equipment and detail lists still scroll
independently. Existing draft-setting confirmation and stop controls remain.

On Fire, choose **Weapon group** to see populated native groups, installed counts
and the selected group. An empty selected group is explicitly labelled. Installed
weapons remain listed when switched off, damaged or unpowered; this does not make
them ready to fire. Readings that are not yet available say so separately.

If FCS controls the old group, changing groups asks for confirmation naming both
groups. The old group returns to native control and **may resume automatic
firing**. The new group enters **FCS Hold**, with Auto Aim off and no firing
permission. Back leaves the existing engagement unchanged. Cease Fire remains
accessible while deciding. Under native control, choosing a group changes the
selection without taking FCS ownership. **Return to Native** remains separate.

Left-click **Volleys** to increase the budget; right-click to decrease it. Both
directions wrap between 1 and 9. Changing the budget cancels existing firing
permission; press Engage again when appropriate. The visible `L+ / R-` hint is
explained here and in Fire help.

## The Artemis report

The inspected owner save contained a powered group-1 coilgun, a group-2 Artemis
Launcher switched off and a powered group-3 CIWS. Its launcher was installed,
not marked damaged, and had the native off condition. No save was modified.

[Blue Bottle Games' Ostranauts](https://store.steampowered.com/app/1022980/Ostranauts/)
1.0.1.5 native `GetActivatedWeapons(true)` uses the installed-and-on weapon
trigger, excluding off and damaged weapons. That collection is appropriate for
native firing eligibility, but did not describe the installed inventory. This
explains the old group-2 “Weapon 0/0 — Unavailable” display. A fresh ship-scoped
inventory now includes the Artemis while native firing
eligibility, power and assignment remain untouched.

Automatic Artemis firing is not added by this change. The existing unsupported
automatic missile envelope and target-lock restrictions still apply; switching
the launcher on is not a promise that FCS can fire it. See
[Fire control](../auto-nav-fire-control.md) for the supported conditions.

## Artwork review and provenance

Suitability ratings below are editorial decisions about a concrete interface
need, not measured usability or performance results.

| Candidate | Suitability | Decision |
| --- | --- | --- |
| Native button faces, live outlines and selection marks | HIGH | Reuse native visuals at runtime; keep labels and states live. |
| New modular frame/backplate | MEDIUM after layout trial | Existing frames accommodate the revised layout. No unmet need justifies generation in this pass. |
| Decorative replacement knobs/icons | LOW | Do not generate. |

The native button donor is Blue Bottle Games' `GUIShip/GUIAirPump`,
`pnlInside/btnDone`. Only sprite/type information is reused at runtime; no donor
controller, behaviour or callback is attached. No native image is exported,
redistributed or supplied to an image provider. Existing original Phobos
faceplates and selected masters remain intact; there are no new generated or
rejected candidates in this delivery. See the
[asset policy](asset-generation-policy.md) and
[artwork resolution policy](artwork-resolution-policy.md).

The revised standing rule allows a MEDIUM-HIGH or HIGH replacement when native
reuse cannot meet a demonstrated layout/readability need. Any later qualifying
pilot must be flat and front-facing, dimensioned for the established layout,
without painted labels, fake instruments or fixed tabs. Preserve masters,
provenance and deterministic exports before expanding production.

## Implementation and checks

Framework adds opt-in `PolarisWidgets`, `SecondaryClick.Bind` and
`ConsoleShell.UsePolarisStyle`; other panels retain their defaults. Right-click
dispatch checks both the button and inherited interaction guards, consumes the
event, and does not also invoke the left-click action.

Inventory discovery is fresh and limited to visible Fire/picker reads or explicit
commands. It excludes docked neighbours, destroyed objects and uninstalled
weapons. Presentation never powers equipment, claims ownership, advances aiming
or authorizes shots. Guided handoffs recheck console, operator, ship, preferences,
ownership revision and exact destination membership. Selection is saved before
the old hold is released; a rejected save or stale confirmation preserves it.
No fire permission or aiming reference transfers to the new group.

The shared ten-per-second routine refresh, immediate action/context refresh and
unchanged-widget suppression remain. Widget bindings are retained; hidden tabs
do not discover weapon inventory. Existing flight, docking, sensor, persistence,
processing and native-boundary suites remain part of the package builds.

Browser checks cover all six tabs plus docking, group selection, handoff and
expanded warnings at 300/400/600-pixel widths with ordinary and expanded text:
60 layout cases. Shared and Polaris industrial previews cover five sizes each,
including narrow navigation and independent scrolling: 10 cases. These are
schematic browser previews, not screenshots of the native Unity widgets.

Focused fixtures cover the three reported groups, damage/power changes, removal,
replacement, reassignment, stale context, competing ownership, protected saves
and volley wrap. Native firing safeguards are checked independently from the
presentation fixtures. Builds and offline tests do not establish in-game layout
or missile support. No additional performance captures were requested or taken;
the owner's earlier improvement report remains qualitative evidence.

## Offline verification results

All five packages build. Focused checks pass: 350 sensor/presentation/handoff
assertions, 69 native fire-control assertions, 20 presentation/discovery/right-click
checks and 305 synthetic installer checks. The broader flight, torch, docking,
Framework, crew, processing, fluid and conservation suites also pass, together
with 11,250 native-definition checks and 75 Python maintenance tests. Counts
include parameter sweeps and are not a measure of gameplay coverage.

The reviewed ledgers cover 1,742 English entries, 145 documents, 14 other text
surfaces and 263 first-party source files. Generated item references, dependency
minimums and all 80 release records pass their consistency checks.

## Optional owner playtest

- Open and close the hub; switch all six tabs, expand a long warning, and check
  Edit, placement and the rescue overlay.
- Check group 2 identifies the off Artemis and suggests checking power or its control signal. Change groups under native
  control, then cancel and confirm an FCS handoff. Confirm the warning matches
  the intended old-group return to native control.
- Try Volleys in both directions at 1 and 9; verify one step per click and that
  firing needs a fresh Engage. Try Cease Fire while a group dialog is open.
- Open the industrial console at narrow and wide sizes. Check scrolling,
  settings drafts, machinery stop controls, ordinary flight/docking and reload.

Unity interaction, final native artwork appearance and measured FPS improvement
remain unverified until owner playtesting. Preserve existing rollback packages
and the six earlier captures.

## Owner-reported regression and correction, 27 September 2026

The owner screenshots of the original delivery show white enabled buttons with
unreadable pale labels, and Departure/Info text meeting the fixed action strip.
The browser previews missed the native styling failure: a white face tint and
inherited brightness were unsuitable for the reused native graphic.

Framework 0.30.1 owns all button-state colours and sets brightness to one. Auto
Nav 0.21.1 covers obsolete painted interior dividers with a live backplate and
draws separate frames from the same registration as the controls. The outer
case and original master remain intact. Departure, Info and dialogs have inset
scroll viewports with contrasting scrollbar handles; fixed actions stay outside.
Shipbreaker 0.29.1 requires the shared correction. The previews now
use the same dark state colours. Compiled palette checks cover text contrast
against a white source graphic; these do not substitute for Unity rendering.

When current sensors recover, the header now shows Resume guidance rather than
the old load-time suspension reason. Info retains that reason as the last event.
No sensor checks are bypassed and Resume must still validate current conditions.

The earlier phrase "switched off" was too specific for the Artemis. Inspection
of Blue Bottle Games' installed 1.0.1.5 native launcher definition found automatic
power-up through electricity and no manual Turn on interaction. The saved off
condition alone does not identify its cause. The card now says "Off: check power
or control signal"; inspect its electrical supply and any connected control
signal. This is local native-definition evidence, not a verified live wiring
fault. Automatic Artemis firing remains unsupported by this change.

The original verification results above describe the initial delivery. This
correction adds a recovered-sensor display regression and native palette/binding
checks. Corrected Unity appearance, scrolling and interaction remain unverified
until owner playtesting; no new performance captures are required.

The owner approved smaller, vanilla-like secondary text and compact buttons.
Commands/help now use 20 design pixels (10 at the minimum 300-pixel panel width),
while key header readouts remain 24. Tabs and scrolling commands are 40 pixels
high; fixed emergency controls remain 48. This frees room for ordinary Departure
instructions and keeps longer text scrollable. Suitability of live framing is
HIGH as an editorial judgement: it removes conflicting raster boundaries without
generating replacement artwork. The initial no-unmet-need judgement above is
superseded by the owner's boundary report; original masters are preserved.

New checks cover separated live frames, controls within their assigned frames,
inset scroll viewports, reaching the final content line and footer immobility
while scrolling. Browser previews use the actual Departure catalogue text.

Correction verification: 352 sensor/presentation/handoff assertions, 69 fire
control assertions, 11,281 native-definition checks, 305 synthetic installer
checks, 75 Python maintenance tests and 70 browser layout cases pass. All five
packages build; Agriculture remains 0.15.2 and Manufacturing remains held.
These counts include parameter sweeps. Native rendering, scrollbar interaction
and readability of the smaller text still await owner playtesting.
