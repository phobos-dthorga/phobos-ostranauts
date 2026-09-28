# Section assembly and maintenance

Prepared for Framework 0.31.0, Shipbreaker 0.31.0 and Agriculture 0.16.1.
These changes have offline checks; native hauling, menus and inventory handling
still need owner playtesting. Manufacturing remains held and uninstalled.

## Assemble where the machine will operate

The D4-S in the screenshot is a section, not a working dismantling fixture.
Choose **Assembly information** on it for the bill and instructions. Final
assembly now uses a native construction site, so a crew member can deliver the
heavy sections one at a time using the drag slot.

| Machine | Matching sections | Result | Unmodified site work, including mounting |
|---|---|---|---|
| D4 dismantling fixture | 2 × 80 kg D4-S | 160 kg, 4 × 4 installed fixture | 48 minutes |
| R4 scrap reclaimer | 2 × 90 kg R4-S | 180 kg, 4 × 4 installed reclaimer | 66.6 minutes |
| F6 electric furnace | 3 × 80 kg F6-S | 240 kg, 6 × 6 installed furnace | about 68.8 minutes |

1. Obtain matching sections and reusable Mortorq and soldering tools. Sections
   can still be made at a supported Bar/Dining Table or workbench, bought or found.
2. Choose **Install** on a section, or select the completed machine in
   **INSTALL > APPS**. Place and rotate its outline on suitable flooring, leaving
   room for crew and bulky deliveries.
3. Let native construction/hauling bring the sections to the site and complete
   the work. You do not need to carry all sections together or assemble the final
   machine on a table. Materials and tools must remain reachable.
4. Once installed, connect power and any required supporting equipment, then
   open **Control Panel**. Dismantling a section produces salvage; it does not
   assemble the machine.

A complete loose machine keeps its ordinary direct **Install** action. Damaged
machinery keeps its existing placement and repair path. Final assembly uses the
same section quantities and masses as before. The work target combines the old
final-assembly duration with mounting, using native five-unit, 0.001-hour work
ticks. Skills, tool condition, hauling and interruptions affect elapsed time.
These are authored gameplay work budgets, not engineering labour estimates.

Native sites retain delivered materials and progress across saves. Use the
site's native cancellation action to recover its delivered parts. Merely
stopping a worker is not the same as cancelling the construction site. Existing
table action IDs and their old physical contracts remain for saved queues, but
new final-assembly offers are removed from tables. Cancel an old blocked table
order and start a construction site; no save is rewritten to convert it.

## Recover cargo from older cooling units

F6-P and F6-R accidentally inherited a container while hiding ordinary Inventory.
The owner's inspected save contains nine direct cargo records, representing
22 units with their stacks, inside an F6-P. This explains why removal was blocked
even while cold. It is separate from the earlier cumbersome-section correction.

Stand beside the affected unit and choose **Recover stored cargo**. Move the
items out through the native inventory window. The action appears only while
cargo remains; it works on installed, loose and damaged cooling forms. New
deposits are rejected. Empty new hardware has no usable storage.

A recovery-only native container is deliberately retained internally so the
game can restore old child records. Removing that component outright would risk
stranding saved cargo. Opening recovery does not move, duplicate or destroy
anything. It clears stale construction-lot markers only after checking that the
equipment and cargo are not in actual lots or running operations. Real reserved
materials stay protected: finish or cancel their native work first. Heat,
protected state, local reach and locks still apply.

Native damage/repair transitions may transfer only the exact captured existing
cargo into the replacement form of the same cooling family. That temporary
permission ends even if the native transition throws; it cannot admit new cargo.
Identifiers, stacks, coolant and thermal records are preserved. The source save
files are never edited.

## Understand a missing maintenance action

**Maintenance information** on Shipbreaker and Agriculture serviceable items
explains the native work types and current blockers. Split a conduit stack
before dismantling one piece. Empty cargo/feed compartments and resolve work
lots first. Furnace explanations distinguish coolant, batch state, temperature,
feed, products, protected records and an unsafe connected cooling unit.
Agriculture distinguishes tank contents, tank links, retained contents/solution,
protected transfers and active cooking/workup jobs.

Repair is for a broken form; Restore treats wear on an intact item and is absent
at zero wear. Native tools, materials, reach, crew capability and containment
still affect availability. Information is a snapshot, not permission to work:
all existing offer and completion checks remain authoritative.

## Implementation evidence and checks

Primary implementation evidence is **Blue Bottle Games' Ostranauts 1.0.1.5**,
inspected locally: native `FusionReactorCore01Install` requires four physical
sections; `Installables.Create` creates input lots and work actions;
`Placeholder.Cancel` releases staged lots; native placeholder records save the
exact source/action/installed identities. [Blue Bottle Games' game page](https://bluebottlegames.com/games/ostranauts)
identifies the game, not a published copy of those internal methods. Proprietary
code and save files remain outside the repository.

Framework owns the reusable section contract, completion guard and read-only
information panel. Shipbreaker owns section quantities, native work budgets and
cooling recovery. Agriculture owns its removal explanations. The historical
table integration retains its attribution to [Ostranauts Crafting Framework](../mods/PhobosFramework/licenses/CraftingFramework-MIT.md).
No new artwork, industrial process, merchant refresh or save schema is introduced.

Offline coverage includes real native action generation, input/output and tool
contracts, menu selection, foreign-entry preservation, rejection of new cooling
deposits and native load/cancel signatures. Production adapter tests cover staged
partial/full bills, wrong/missing/destroyed/contained sections, duplicate references,
cancellation, retained progress, reload-equivalent objects, completion replay,
recovery access/heat/lot restrictions, stack identity and scoped damage transfers.
They do not run Unity physics, native crew pathfinding or the owner's save live.

## Short owner check

- On the existing D4-S, open Assembly information and place its construction site.
  Supply the second section; watch separate hauling, interrupt a worker, then
  resume. Confirm the finished fixture has Control Panel and the expected mass.
- Save and reload a partly delivered site. Check the same parts and progress.
  Cancel one spare site and check that its delivered sections return without loss.
- Use direct Install on an existing whole loose machine. Confirm that path still
  works and does not demand sections.
- On the affected F6-P, choose Recover stored cargo. Check quantities and stacks,
  unload everything, and confirm new deposits fail. Reopen Maintenance information
  to see any remaining coolant, temperature or paired-equipment restriction.
- Check a conduit stack and an agricultural appliance with contents. Confirm the
  explanation and normal maintenance after safely resolving the blocker.
