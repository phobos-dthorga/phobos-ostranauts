# Polaris opening failure — 25 September 2026

Auto Nav **0.12.1 is a correction candidate**, not an installed or gameplay-verified
fix. The owner reports that Polaris opens normally after the immediate preceding
installed builds were restored, including Auto Nav **0.11.0**. The rollback is
retained during diagnosis. No save files were edited or migrated.

## Evidence and limits

- The owner's screenshot and preserved local Unity log show repeated
  `InvalidOperationException: Sequence contains no matching element` while
  `CrewSim.RaiseUI` opens the navigation station. The stack does not identify the
  original throwing line inside the plugin.
- The captured failing session loaded Auto Nav **0.12.0**, Framework **0.20.0**,
  Shipbreaker **0.17.0** and Agriculture **0.7.0**. It also reports Auto Nav's native
  package disabled and its artwork unavailable. The owner explicitly confirms
  disabling it **after** the first failure, while investigating migration. These
  later missing-content messages do not establish the original cause.
- Inspection of the exact backed-up 0.12.0 DLL finds all **43** intended layout
  regions in its embedded JSON, identical to the authored source. This rules out
  an omitted or stale embedded layout in that delivered binary.
- The new `HubLayout` uses `JsonUtility.FromJson` and an unchecked LINQ `First`
  region lookup during panel construction. That lookup can produce the observed
  exception if deserialization leaves no matching region. Unlike flight capture,
  it executes merely on opening Polaris, without a navigation command.
- **Leading diagnosis, not a reproduced Unity fault:** the new runtime layout
  deserialization/lookup path. Reverting the entire update also restored the
  preceding Framework, Shipbreaker and Agriculture binaries; this was not an
  isolated Auto Nav A/B test. No evidence presently requires a save migration.

Unity's [Unity 6.3 JsonUtility documentation](https://docs.unity.com/engine/6000.3/script-reference/unityengine/jsonutility)
states that it uses the Unity serializer. Unity's
[JSON serialization manual](https://docs.unity.com/en-us/engine/6000.3/manual/scripting/compilation-and-code-reload/script-serialization/json-serialization)
also describes using a general-purpose .NET JSON library where needed. Unity
staff's [runtime-loaded assembly explanation](https://discussions.unity.com/t/advanced-use-case-assetbundles-and-dynamically-loaded-assemblies/756537)
identifies limits on Unity serialization for assemblies loaded outside its normal
script-loading path. That older discussion concerns asset bundles and is supporting
context, not a reproduction of this Ostranauts failure or proof of a Unity 6 defect.
The older linked issue-tracker entry was unavailable when checked.

Blue Bottle Games' locally inspected Ostranauts **1.0.1.5** `GUIOrbitDraw.LoadModules`
looks for a named child first, then a native prefab. Our prefix creates the child
before that loader runs. An exception escaping this prefix can interrupt station
initialization. Game source stays local; see Blue Bottle Games'
[Ostranauts product page](https://bluebottlegames.com/games/ostranauts) for attribution,
not as documentation of this private implementation detail.

## Candidate changes

- Use the game's existing Newtonsoft.Json dependency, already used by Framework,
  to read the actual embedded resource independently of Unity's script serializer.
- Reject missing fields, null/empty regions, duplicate names, nonfinite dimensions
  and invalid canvas bounds. Missing lookups name their region in the diagnostic.
- Catch only our hub's construction failure, deactivate/detach its partial root,
  and log the full exception while allowing the original native loader to proceed.
  This does not suppress unrelated native errors or guarantee the hub is usable
  when a required resource is faulty.
- Skip hub creation when the native Auto Nav package is disabled or missing.
  Check that prerequisite before publishing equipment, stock or salvage records.
  This is failure containment, not support for removing a content provider from
  a save which uses it.
- Preserve equipment/save identities, placement, flight rules and artwork.

## Verification and owner follow-up

`scripts/build-autonav.ps1` now includes the native integration checks. They read
all 43 regions from the compiled plugin, compare their geometry with source,
exercise malformed-resource rejection, and check disabled-package registration.
They do not substitute a browser preview for the runtime reader.

The isolated 0.12.1 candidate build passed with zero warnings/errors: **7,177**
native-definition/registration checks (including the embedded layout and disabled
package cases), **852,299** flight, **48,335** torch, **437,246** docking, **160**
sensor/hub and **31** fire-control assertions. Framework passed **2,481** checks
and its performance adapter passed **35**. Constant consistency, Workshop records
and local document links also passed. The restored installation was rechecked:
all **201** files still matched the rollback packages.

Verification used an isolated checkout of the 0.12.0 checkpoint plus this
correction, because unrelated Auto Nav feature work was changing the shared
checkout during diagnosis. The first shared-folder build stopped at an unfinished
new equipment translation; that failure was not attributed to the startup fix.

Actual Unity panel construction and the candidate's effect on the owner's save remain untested.
When the owner chooses to test the candidate, enable its matching native package,
open Polaris, verify the ordinary instruments and hub, and reopen the station.
If it fails, preserve the fresh Player log: the new hub boundary reports the
original exception rather than letting it become only a repeated RaiseUI failure.
