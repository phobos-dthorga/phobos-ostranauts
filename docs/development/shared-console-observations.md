# Shared console observations

Prepared **25 September 2026**: Framework **0.13.0**, Shipbreaker **0.11.0**.
Built against Ostranauts **1.0.1.5** / BepInEx **5.4.23.5**. Code-level checks
are separate from the owner's pending in-game evaluation. This round installs
no files in the game, adds no equipment and changes no saved identifiers.

## What the player gets

Open the **Phobos' Asterel C1 Industrial Control Console**, then **Observations**.
The existing scrollable panel has groups for room alarms and built-in instruments.
Search accepts instrument names, full source IDs and compartment IDs. **Attention**
combines machinery problems with alarms and unavailable instruments. **Overview**
counts machinery and instrument readings separately; two cooling probes do not
count as two extra machines. The usual status filter also applies: instrument
rows use Ready for a current clear/numeric reading, Blocked for an active native
alarm and Unavailable for stale, unavailable or faulty evidence. Details distinguish
those validity states explicitly.

Each reading names its source, full object ID, host ship, monitored compartment,
capability and observation age in simulation seconds. A missing reading is
**Unknown**, never zero. Last-known readings are labelled as historical.
No installed instruments means no coverage, rather than an all-clear.

| Instrument | Reading supplied | Limit |
| --- | --- | --- |
| Native oxygen / nitrogen alarms | Native clear/alert output | No gas concentration or whole-ship safety claim |
| Native CO2 alarm | Native clear/warning/alert output where present | Does not invent a middle state for definitions without one |
| Native smoke alarm | Smoke/carbon-monoxide threshold output | Not a universal fire location service |
| Native contaminants alarm | Ammonia/sulphuric-acid threshold output | Not a universal chemical analyser |
| Native thermostat | Below range / in range / above range | No invented numeric temperature reading |
| R4 reclaimer built-in cooling probes | Room temperature in °C and pressure in kPa at its `use` point | Not core temperature, flow rate or measured electrical demand |

Reclaimer probes use the atmosphere already involved in its native cooling
calculation. They require installed, undamaged, enabled, powered machinery and
a valid own-ship compartment. They add no purchasable part or recipe dependency.
They do not alter heat accounting, cooling interlocks or the physical process.

Room alarms are informational. A distant room alarm does not become an invented
cause of a reclaimer stoppage. Existing configured demand remains labelled as
configured demand, not measured reactor output. This round adds no automatic
responses, new signal wires, PDA view, specialist sensors or furnace hardware.

## Stop evidence

Equipment details show the most recent recorded processing stop or collector
block/fault in this session. For a reclaimer, the record includes available
cooling-probe evidence with source, compartment, value, validity at stop and age.
Later live readings cannot overwrite that frozen evidence. Repeated reports of
the same continuing interlock do not keep replacing the original timestamp.

This is a small diagnostic record, not a complete event history or proof that an
unrelated alarm caused the stop. Existing current status still explains waiting,
blocked routes and start refusals. Processing Start clears the previous record;
reload/new game clears session diagnostics. Collector records may remain until
another recorded stop or reload. No new history is written into a save.

## Authority, freshness and cost

Panel and F3 use the same `IndustryObservations.TryRead` access boundary. The
operator, powered console and ownership are rechecked before every read. Losing
console access clears its displayed roster, without stopping autonomous machines.
Local reclaimer panels use the same probe provider under their existing local
access checks. Docked, towed and moored neighbours never enlarge the console's
host-ship roster, including other player-owned ships.

Native pressure sensing can include neighbouring geometry. The adapter rejects
sampling points containing foreign-ship objects, rather than assigning that
output to the host compartment. This is deliberately conservative: a reading
near an overlapping dock may be unavailable even when its host room exists.
Unknown scope or a changed compartment requires a fresh native evaluation.

Native `GasPressureSense.Run` and `Sensor.Run` are observed after their own work;
the adapter never calls them or changes their signals. Reloaded lamp state alone
cannot become a current observation. Native evaluations are tracked with the
panel closed, followed by a later-frame read of the resulting lamp state. Queued
native lamp transitions can still lag the environment: this is an alarm-output
adapter, not new gas telemetry. A five-simulation-second evidence limit marks
unrefreshed output stale. Paused simulation time does not age readings; power,
damage, switch state and scope are checked on each read regardless.

Framework retains weak per-object witnesses. Shipbreaker caches only the queried
host ship's supported instrument roster for one real second and revalidates each
source before reading it. It does not enumerate the whole world per frame.
Source removal, moving ship, containment and uninstalling exclude a cached entry
immediately; discovering newly installed hardware can take up to that interval.
Content reload, save load and new game discard observation/session caches.
No powered state, alarm signal, heat, material, job or provider-owned data is
modified to obtain a reading. Unrecognised instruments fail independently.

## F3 and owner checks

```text
phobosindustry consoles
phobosindustry observations <full-console-ID>
phobosindustry status <full-console-ID>
```

`observations` and the panel report the same provider output. `status` includes
the machinery details, live reclaimer probes and captured stop evidence. The
operator must remain at the authorized console for either command.

1. With an existing powered room alarm, open Observations. After a brief unpause,
   compare its reported output to the native alarm. Check its source/compartment
   and compare the F3 output. Confirm reclaimer readings explicitly say room air.
2. Turn off an alarm: its live reading must become unknown, with any available
   last value marked historical. Restore it and let its native sensor update.
3. While docked, confirm the console excludes the neighbouring ship's instruments.
   Change operator or leave the console: observation access must be revoked.
4. When an actual reclaimer cooling stop arises in play, compare its recorded
   reason/evidence with its current readings after conditions change. No need
   to manufacture an accident solely to check this display.
5. Reload: old diagnostics must not masquerade as current readings. Native alarms
   wait for reevaluation; jobs retain their existing paused-after-reload policy.

Offline coverage exercises the public observation contract, native adapter and
console access against narrow world doubles, plus installed native field/definition
contracts. It covers stale/unknown output, power, damage, changed compartments,
clock reversal, reload, docking boundaries, moving sources, revoked ownership,
operator changes, roster reuse, frozen evidence and unchanged native heat.
Native queue timing, panel layout at game resolutions and companion-mod overrides
remain owner-tested integration concerns.

## Framework and provenance

`Phobos.Ostranauts.Framework.Observations.Observation` is an immutable value
contract with source/ship/subject identity, kind, capability, units, numeric or
alarm value, observation time, validity and a machine-readable reason.
`Assess` expires evidence and removes values when scope or clock changes;
consumers must explicitly require `Current` before using a value for automation.
`NativeRoomAlarms.Read` supplies the narrow native adapter. Neither API grants
control authority: consumers must enforce their own checked access. See the
[author guide](framework-author-guide.md).

Framework owns those reusable mechanisms. Shipbreaker owns R4 probe semantics,
console presentation/localization, access integration and stop evidence. A generic
provider registration bus, saved observation history, signal-routing changes and
process-specific hazard machinery are deliberately not introduced here.

Evidence: local `GasPressureSense`, `Sensor`, `Ship.GetCOsAtWorldCoords1`,
`Ship.GetRoomAtWorldCoords1`, `Room`, native alarm definitions/property maps and
pressure tickers; assembly SHA-256
`91B50F45CACD64DE39B9BCC30EC7B4542F3E3976AC3BC5589B346976A262425E`.
Native code/data belongs to Blue Bottle Games; this repository contains original
adapters and tests, not decompiled source or extracted artwork. Existing approved
console artwork is reused. See [sensor research](sensor-integration-research.md)
and [furnace research](fusion-smelter-research.md) for the wider direction.
