# Auto Nav 0.22.1 towing repair

The owner reported blocked navigation with the towing brace engaged, both with
NAV WEAK and NAV OK. The latest relevant save was inspected read-only: Forgotten
Virtue and Terrible Mushroom had reciprocal docking records and a secured native
brace assigned to their connected port. The log confirmed Auto Nav 0.22.0 loaded.
There was no Auto Nav exception establishing this as a runtime crash.

## What changes

Ordinary Approach, Rendezvous and Follow now admit one reciprocal, securely braced
pair in free space. Select a different, tracked destination. Both RCS and permitted
torch manoeuvres remain available. Native RCS acceleration and delta-v already
include attached mass; the mod does not add another mass or fuel charge.

An enclosing radius around the pilot ship includes the tow's offset and hull.
Arrival clearance, braking, avoidance and projected no-wake checks use this
extra space. The attached partner is not treated as independent traffic. Brace
updates, unsecured connections, missing peers, mooring, stationary/grounded
attachments, chains and conflicting controls aboard the tow prevent guidance.
The mod neither changes braces nor releases any connection.

Release the tow before Dock or Approach & Dock, FCS/Combat or industrial close
work. Those paths retain their single-ship attachment, firing and positioning
requirements. This patch does not extend native port fitting to a combined hull.
Sensor contact requirements remain unchanged; NAV OK cannot override an unsafe
connection. Navigation warnings now take precedence over idle FCS faults.

## Native evidence and limits

Primary evidence: **Blue Bottle Games' installed Ostranauts 1.0.1.5 code**, inspected
locally, not redistributed. `Ship.IsDocked` includes braced connections;
`TowBraceSecured` recognizes a brace at either end. `RCSAccelMax` includes all
attached mass; `Maneuver` and `SetThrust` send input through `DockGroup.SyncShipInput`.
`ShipSitu.GetRadiusAU` covers an individual hull, hence our extra enclosing radius.
Torch force/acceleration semantics remain those of the native game, not a new
physical towing model. [Blue Bottle Games' official game description](https://store.steampowered.com/app/1022980/Ostranauts/).

Offline regressions cover the production service's entry and lost-brace stop,
reciprocal membership, unavailable/extra peers, stationary/moored/grounded cases,
competing torch/waypoint controls, envelope size and navigation-warning priority.
Two mirrored tow approaches execute the production core and avoidance/update
sequence against native boundary doubles. Existing 128 coupled docking scenarios
remain in the suite. Torch admission is checked separately. These tests do not
execute Unity's complete dock-group physics, heat, fuel or rendering.

## Short owner check

With the brace secured and the other ship's controls idle, select a tracked third
ship/station and choose Approach. Check that the combined hull stops clear and
that neither the brace nor ports change. Check RCS first, then permitted torch
operation outside no-wake areas. Confirm an idle FCS fault no longer hides a
navigation warning. Live handling remains unverified until owner feedback.
