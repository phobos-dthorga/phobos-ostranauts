# Manufacturing checks

`dotnet run --project tests/PhobosManufacturing.Tests -c Release` compiles the
mod's `Core/*.cs` sources directly (no game references) and checks the pure
rules: every refinery charge conserves mass and lists exactly its products;
`Match` accepts only exact charge multisets (the steel charge only with
Shipbreaker's stock); durations, energies and room-heat shares; adjacency;
electrolysis balance and oxygen moles; `Advance` never runs two cycles in one
step and refuses NaN; state records round-trip and reject bad payloads; the
hydrogen spec, leak rate, `Burn` (min of hydrogen and an eighth of the oxygen,
8 kg O2 per kg H2, never more than the room holds), deflagration sizes and the
ignition rule; spoilage conserves mass; off-gas shares sum to the charge's gas
and `OffGasDueKg` never over-emits; price rules (four ingots below one iron
block, five carbon plus water below one carbide block).

`tests/PhobosNative.Tests/ManufacturingNativeChecks.cs` runs with the local
game: forms, sizes, masses, Fennmark names, power info, feed-trigger admission
and refusals at the game level, native ore masses and stacking, `Prepare(true)`
against `Prepare(false)`, the clay chunk once in each amended mining table and
in no shop, the O2 canister capacity formula, the hydrogen vessel registration,
economy parity with the S3/T2 offer sets, a native buyer for every retail
identity, APPS placement, artwork bindings, and the deflagration entries loaded
through the game's own `DataHandler` with the `Explosion` command.

Not covered, by design: live Unity layout, crew hauling, in-game fire and
explosion behaviour, canister pressure display and shop restocking. Those are
the owner's gameplay checks listed in the player guide.
