# Phobos control panels

Framework 0.26.0, Agriculture 0.13.0, Shipbreaker 0.26.0 and Auto Nav 0.20.0
prepare this interface update. Manufacturing remains a scaffold with no operational
panel or jobs. These are unpublished development candidates.

## Finding controls

Open a machine normally to start on that machine. C1 retains its ship equipment
overview and observations. The common frame keeps navigation and primary actions
outside the scrolling content. Equipment and detail lists scroll independently;
below 1,000 native content units, Back switches between list and detail pages.
Actual native panel bounds, including the game's UI scale, choose that layout.
Asterel uses slate, Verdemorrow muted green and Rivetline amber accents.

Crew operations has separate **Orders**, **Crew & Training** and **Time-skip** views.
Orders start disabled. Select a process, target stock and approved stores where
applicable, Apply, then Enable / Resume. The B2 form contains only its relevant
settings. Native AutoTask, duties, shifts and personal needs remain authoritative;
permission changes use their own Apply form. Training bars report saved progress.

Agriculture groups operations, supplies/connections and details, retaining its
live crop-stage artwork. Planting, harvesting, workup and draining remain explicit
service actions. C1 and Shipbreaker group equipment operation, routing, maintenance
and diagnostics. F9 and the old collector/reclaimer entry points open the same
native panel, retaining local-access checks. The F6 keeps its existing audited
native gauges and safety controls, with standard controls available as fallbacks.

Auto Nav stays in the approved native Polaris footprint with its existing art.
Flight, propulsion, fire and departure remain distinct. Navigation drafts show
Apply and Discard in the lower caption area while Stop and Cease remain available.
New flight commands wait until the navigation draft is resolved. The native panel's
placement controls and rescue view retain precedence.

## Applying settings

Configuration selections remain drafts. **Apply** validates the complete open form
against fresh state; **Discard** reloads the saved values. Leaving a dirty form
requires Apply, Discard or Keep editing. A failed or stale apply retains the draft
and explains the failure. Stop acts immediately, including with a draft open;
an independent stop makes an older draft stale rather than silently clearing it.
Applying a changed enabled standing order suspends it for explicit Resume.
Disabled and manually stopped orders retain their state.

Connection fields open focused Apply forms. Furnace heat, ramp and cooling settings
are validated and applied together. Navigation cruise, arrival speed/distance and
torch preference and departure intent are checked together. Start, Seal, Harvest, Drain, Undock, Fire
and other operational actions still run explicitly through the owning checked
service; applying configuration does not initiate production or a flight.

## Choosing storage, connections and names

**Change** opens a searchable candidate picker. Approved stores with relevant
contents sort first; Include empty / unsuitable makes the other eligible stores
visible. Missing saved selections are retained and labelled unavailable. Connection
pickers retain their content mod's candidate rules. Mission targets are limited
to the already bound mission or saved resumable flight; no new target is acquired.

**Locate** highlights the selected object on the ship. **Pick on ship** temporarily
exposes the ship beneath a full-screen input-capturing overlay. Marked candidates
use the native object hit test; overlapping candidates open a short choice list.
Back or Escape restores the panel and draft. The picker does not select the native
crew, issue movement or pickup, acquire targets or run machinery. Mouse/keyboard
world handlers are suppressed during picking and through the release frame.
Access and candidate membership are checked again when applying.

An optional display nickname is saved as protected Framework metadata, separate
from the native name and object identity. Normal lists use names and location;
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
Blue Bottle Games' local Ostranauts 1.0.1.5 installation. Proprietary decompiled
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
