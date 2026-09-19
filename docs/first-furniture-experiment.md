# First furniture experiment

Prepared 2026-09-20. **Proposal only: no feature selected or implemented.**

The owner subsequently clarified that the intended product is a broad collection
of medical devices. The [medical-system vision](medical-system-vision.md) now
provides the design frame. This earlier comparison is retained as technical
research; its standalone blood-loss station recommendation has not been adopted.

One placeable object with one interaction in a separate test save remains a
technical checkpoint within that larger direction. The following choices use
the current installation's data as research evidence; none is a proven
implementation recipe. See the dated
[environment and native-data findings](modding-notes.md#environment-recheck-2026-09-20).

## Three useful starting points

| Experiment | Player action and result | Resources and tradeoff | Main question to prove |
| --- | --- | --- | --- |
| Blood-loss assessment station | Perform one timed self-check; receive a concise report of blood-loss severity and whether loss is ongoing. | Ship space, electricity and time. Start with the acting character, without patient selection. | Can native interactions read the appropriate patient state and produce a useful report at completion? |
| Salvage sorting bench | Process one trash item into one defined, small scrap output. | An input item and labour; proposed output and balance must be checked against actual item mass/value. | Can a native interaction consume exactly the intended input and produce output once, including interruption and inventory edge cases? |
| Recovery chair | Take a timed rest session that provides a temporary benefit to one existing crew need. | Ship space and time away from duties; the effect must add something to ordinary sitting/relaxation. | Can the benefit begin only after completion, expire correctly and avoid stacking through repeat use? |

All three can begin with a manual action and placeholder artwork. Automatic crew
use is a separate feature: existing AI does not automatically understand an
arbitrary new interaction. Native JSON is the first implementation hypothesis,
with a small C# extension only if the chosen behaviour requires it.

## Earlier recommendation: blood-loss assessment station

This is the closest fit to the owner's interest in physiology and medical
equipment. It gives us a small way to learn how furniture, patient state, power
and interaction timing meet before designing new drugs or treatment rules.

Its limitation is real: the game already exposes blood-loss severity and some
bleeding conditions. A recap of those labels proves the furniture interaction,
but is not yet a compelling diagnostic feature on its own. The useful follow-on
is a measured loss/recovery trend or richer assessment of underlying state.
Check the existing in-game health display before deciding which extra information
actually helps the player.

Proposed first slice:

1. Place a uniquely named Phobos station in a dedicated test scene/save, initially
   using a core sprite reference or an original placeholder. Test acquisition can
   use development facilities; shop stock and derelict distribution are later work.
2. Offer one `Check blood loss` action to the acting character. Require access,
   installation, a working device and electrical power.
3. Run a short, finite interaction. Its duration is a design parameter to choose
   during implementation, not an estimate of development effort.
4. At completion, report the character's existing severity and the bleeding signal
   that investigation shows is reliable. No automatic healing or medication is
   part of this proposed action.
5. Use the native interaction log first if it can present the result clearly.
   A custom panel is justified only by information that the existing display
   cannot express adequately.

The immediate technical uncertainties are signal ownership (patient versus wound),
when conditions are evaluated, branch selection, power-loss handling and whether
completion output can be suppressed after cancellation. Inspect those narrowly
once this option is selected. If native data cannot support a useful report,
reassess a small C# diagnostic service before building any wider infrastructure.

For later trend reporting, compare actual observations or correctly understood
loss and recovery behaviour. `StatBloodRate` alone is not a net change. Do not
invent clinical units, blood pressure, oxygen saturation, numerical precision or
disease diagnoses that the game's data has not established.

## What would count as a successful first round

- The package loads and the new object places with the right footprint, access
  point and interaction, using a separate test save.
- A completed action produces the intended, observable result once. For a
  diagnostic, test at least a normal and an affected character and compare the
  report with independently inspected game state.
- Cancellation gives no completed report or reward and leaves no occupied/busy
  state behind. Resource-consuming alternatives additionally need a clearly
  verified consumption/refund rule and no item duplication.
- For powered equipment, test starting without power and losing power during
  use. The proposed scan should abort without a successful result.
- Save/reload the placed object and test an interrupted/in-progress action.
  The eventual behaviour must be explicit; it must not silently award completion.
- Check normal speed and accelerated time. Do not assume these checks establish
  unattended or unloaded-ship processing; that is outside the manual first slice.
- Record game and plugin versions, actual results and remaining failures.

Parsing JSON, checking identifiers or compiling code are useful preparation,
but none substitutes for these in-game observations. No build system, plugin
dependency, shared framework, final artwork or development-time commitment is
selected by this proposal.
