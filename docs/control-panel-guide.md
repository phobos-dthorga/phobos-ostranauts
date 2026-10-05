# Phobos control panels

Framework 0.103.0, Agriculture 0.53.0, Shipbreaker 0.80.0 and Auto Nav 0.34.0
prepare this interface update. Manufacturing's machines and stores, Framework's water
silos, and Shipbreaker's T2 thaw unit and ML-2 mining laser share one Control Panel:
Operation, Connections (Settings on the laser) and Details. These are unpublished
development candidates.

## Finding controls

Open a machine's Control Panel to work beside it, or use C1 for equipment on
your ship. **Operation** holds Start/Pause and the current job. **Supplies &
connections** holds stores, links and materials. **Details & diagnostics** shows
technical information when you need to investigate a fault.

Crew operations has **Orders**, **Crew & Training** and **Time-skip** pages. The
crew roster shortcut is **Orders & training**. Long lists scroll; on narrower
screens, use Back to return from equipment details to the list. Long compact
labels may end in an ellipsis; selected storage names wrap above their buttons.

Auto Nav keeps separate navigation, propulsion, fire and departure controls in
Polaris. Stop and Cease Fire remain available while editing flight settings.
Use the navigation console's Edit controls to place the hub.

## Applying settings

Changes to a settings form are not saved until you choose **Apply**. **Discard**
reloads the saved settings. When leaving an edited form, choose Apply, Discard or
Keep editing. If equipment or access changed meanwhile, the form stays open and
explains why the settings could not be applied.

**Stop acts immediately**, even while editing. If another control stops the job,
reload the form before applying older changes. Changing an enabled standing order
pauses it; choose Resume when ready. Applying settings alone does not start work,
harvest a crop, drain supplies, undock or fire weapons.

## Choosing storage, connections and names

**Change** opens a searchable list. Approved stores with relevant
contents sort first; Show empty / unsuitable stores makes the other eligible stores
visible. For an input store, **Use anything aboard** chooses the whole ship
instead of one store: the deck, unlocked stores and other machines' product trays. Missing saved selections are retained and labelled unavailable. Connection
pickers retain their content mod's candidate rules. Mission targets are limited
to the already bound mission or saved resumable flight; no new target is acquired.

Since Framework 0.57.0 a machine's store and tank connections list only what it
can actually reach: equipment touching it, or on the same water or gas line.
A line joins whatever it runs under or right beside (since Framework 0.69.0).
Each choice says how (*touching*, *process water line*, *gas line*) and marks a
destination that is *full* or a source that is *empty*. Under the choices,
**Aboard, but not offered** names anything left out and the first thing to fix:
loose, damaged, locked, no working line touching it, or a drained line. If a
different kind of line lies there instead, such as an Irrigation Conduit where a
Process Water Line is needed, it names that too: only the line the cargo travels
in joins. A store's own panel lists every machine linked to it. See
[linking machines and stores](manufacturing-player-guide.md#linking-machines-and-stores).
Since Framework 0.63.0 the lines hold what they carry, and a drained or vented line
reaches nothing until it is returned to service; see
[draining and venting](lines-and-draining.md).

**Locate** centres a temporary ship view on the selected object. Locate is disabled
when no object is available; Clear is disabled when there is no saved selection.
**Clear** edits the draft and reports that Apply is still required. An unavailable
saved selection can still be cleared deliberately.

**Pick on ship** shows a mode banner and dims the background around eligible
objects. Brackets mark candidates; a line from the source machine animates cyan
when an object can be chosen. Click overlapping objects to open
a short choice list, or use Available objects to return to the full picker. Right
or middle drag pans the view; scroll zooms. Back, Cancel or Escape restores the
original panel, draft, camera position, zoom and follow preference. Picking chooses an object for the form; it does not move crew or operate machinery. Cancel closes the picker.
Access and candidate membership are checked again when applying.

An optional nickname helps you recognize equipment. The original item name stays unchanged. Normal lists use names and location;
full object IDs remain available in diagnostics. Available native equipment art
and portraits are referenced locally, with a neutral outline when absent. The
original Phobos frame and its [provenance](../assets/phobos-industrial-console/README.md)
are reused unchanged. No game-derived art is redistributed.

## Time-skip

The native time-skip screen offers a separate Phobos estimate button in its lower
left area. Its collision warnings and Go control are retained. The Phobos view
separates crew/shift availability, onboard work and resource limits, and suspended
exterior operations. Estimates describe current work and the selected horizon;
they are not promises that future power, supplies or access will remain available.
Native and Framework execution rules, crew time budgets and material accounting
are unchanged. Exterior work and manoeuvres still need explicit Resume afterwards.

## Validation boundary

Automated validation covers builds, the existing native/content checks, draft
copying and manual-stop preservation, stale fingerprints, selection admission,
overlapping candidates and input capture/release policy. The browser reference
checks 1080p, 1440p, 3440×1440 and increased scale, including independent scrolling
and a narrow layout. It does not execute Unity layout or game input.

Owner checks in Ostranauts are still required:

- Open every equipment family locally, from C1, crew roster, and native time-skip.
  Compare 1080p, 1440p, ultrawide, larger UI scale and long translations.
- Check body text, native gauge alignment, scroll areas and fixed Apply/Stop.
  Try duplicate names, many stores and absent artwork/native widget donors.
- Edit, apply, discard, stop, close/reopen and change configuration elsewhere.
  Destroy or move equipment and remove a selected store while a draft is open.
- Pick overlapping objects, right-click, drag, use force-walk modifiers, Escape
  and return to a dirty form. Confirm that no movement, inventory action or target
  command occurred and the previous selection is unchanged.
- Check F6 hot-state/access restrictions, C1 local-inventory restrictions and
  Auto Nav's captured flight restrictions. No UI test may bypass those services.

The native lifecycle and object-hit-test integration follow code inspected in
[Blue Bottle Games' Ostranauts](https://bluebottlegames.com/games/ostranauts) 1.0.1.5 local installation. Proprietary decompiled
sources remain outside the repository. Existing research attribution, mod author
credits, material budgets and optional integration boundaries are unchanged.

Delivery checks on 26–27 September 2026 passed the affected build suites:
8,840 Framework checks, 824 Agriculture checks, 8,378 Shipbreaker checks,
9,973 native-definition checks, and the existing Auto Nav flight, docking,
sensor, fire and compiled-panel audits. The maintenance suite passed 59 Python
tests; synthetic installations passed 226 installer checks. These totals include
many parameterized assertions and do not measure in-game coverage. New checks
exercise isolated drafts, manual stops, stale navigation settings, candidate
admission and input-release policy. Five browser reference sizes/scales passed
scrolling, fixed-action, narrow-navigation and long-name checks.

### Follow-up to the owner's 27 September screenshots

The owner observed overflowing compact labels, repeated diagnostic paragraphs,
an absent minus glyph and unclear picker/control feedback in the first installed
redesign. The corrections above address those reports. The picker uses native
read-only hit-testing, with our own screen geometry rather than calling native
ShowInputSelector: that native controller also sets connection and crew-selection
state. The dim veil does not change object layers, lights or contents. Existing
original framing and locally loaded item pictures are reused; no new raster art
or game-derived files are exported.

Additional automated checks exercise per-line truncation (including combining
characters), overlapping/offscreen candidate openings, and compiled callback wiring:
diagnostics clicks cannot allocate new labels, both stock symbols are drawn, and
the picker cannot call connection, crew-selection or equipment-command methods.
These checks establish code and layout-policy properties. They do not establish
Unity font rendering, pointer routing, camera behaviour or in-game appearance;
those remain owner checks after installation. Manual command confirmations and
rejection reasons use the persistent notice area; Agriculture refresh does not
erase an Apply confirmation. Fixed captions also respect the number of lines
that fit vertically, including single-line footer notices.

Follow-up validation passed 18,932 Framework assertions (including pixel samples
for the picker mask), 824 Agriculture, 8,378 Shipbreaker, 9,973 native-definition
checks, the existing Auto Nav suites, 59 Python maintenance tests and 226 synthetic
installer checks. Compiled panel/input wiring and five browser reference sizes
also passed. Assertion counts include parameter sweeps; they are not gameplay
coverage measurements. No Unity session was run for this follow-up.

## Polaris readability update

The [Polaris interface refresh](development/polaris-interface-refresh.md) gives Flight Hub
six larger tabs, full warning details, an installed-group picker and reversible
volley adjustment. Industrial Control uses matching native button faces and
wrapped navigation/actions at narrow widths. Existing Edit/rescue handling,
settings drafts, ownership checks and emergency controls remain in place.

## Combat on Track (Auto Nav 0.22.0)

Fit working N2 and N3 in the same Polaris console. On Fire, select the tracked
fire target and choose **Use for aim** on the desired ready weapon. On Track,
choose **Enter Combat**. The previous flight is suspended; movement matches the
fire target at your navigation speed/separation. Hull clearance may raise the
effective separation shown in Track's scrollable instructions. **Engage** on Fire
is still a separate firing decision. **Cease Fire** leaves range matching active.
**Leave Combat** stops combat movement; **Resume** deliberately restores the old
flight. Leave Combat before editing movement settings. See the
[complete operating and validation notes](auto-nav-combat.md).
