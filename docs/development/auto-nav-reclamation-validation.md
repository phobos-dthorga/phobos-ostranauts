# Departure and reclamation validation

Prepared 26 September 2026 against locally installed Ostranauts 1.0.1.5.
This record covers automated calculations, native-definition/API checks and
service harnesses. It does not claim a Unity scene run, measured fuel savings,
owner acceptance or Workshop publication.

| Checkpoint | Automated coverage |
| --- | --- |
| Shared avoidance | Bounded visibility planning, static detours, retained passing side, blocked endpoints, crossing traffic, swept acceleration, full burn plus braking, multi-leg exterior traversal and live contact-loss suspension. Existing Fly, pursuit, docking, torch and fire suites remain in the builders. |
| Explicit departure | Station/ship/mooring paths, other station attachments retained, missing clearance, open boundary, missing crew, tow, extra attachments, ground station, reserve/hardware rejection, native detachment exceptions and explicit reload recovery. Continuation destination is captured before release. |
| Paid acquisition | All cardinal mouth orientations, protected supports, exact 24 kg eligibility, saved duration/power, partial energy, thermal budget boundaries, separate uninstall/transfer journals, native object replacement/relocation, full G4 and exactly-once recovery after placement failure. |
| Automatic traversal | Multiple windows, retreat before transit, recapture before cutting, updated candidate selection, manual takeover, contact loss, retained capture on Stop and no silent restart. |

The reclamation service harness executes the real mission and transfer adapter,
with doubles at native capture, geometry and power boundaries. Numerical and
native-definition suites check the corresponding rules separately. This separation
is deliberate: synthetic attachment and power observations are not proof that
every native scene or third-party-mod combination behaves the same way.

Reproduce with `scripts/build-shipbreaker.ps1 -OstranautsPath <game>`; it builds
Framework and Auto Nav first and runs their required suites. Use
`scripts/update-item-reference.ps1` for source-derived prices, service data and
operating modes. Run the constants, publication-record, Python tooling and installer
fixture checks alongside it. Installation is separately verified with the existing
installer's `-WhatIf` and `-VerifyOnly` modes and game-closed guard.

Owner evaluation should observe native deck recapture and room rebuilding,
partially supplied cutter electricity and heat, busy docking areas, fast-forward,
and save interruption at actual native transitions. These remain gameplay
evaluation, not a prerequisite for preparing the implementation.
