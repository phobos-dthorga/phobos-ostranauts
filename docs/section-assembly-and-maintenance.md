# Installing machines and maintenance

The D4, R4 and F6 come whole: buy one from a trader or find one in salvage, then
install it where it will work. For current versions and dependencies, see the
[player guide](player-guide.md). Manufacturing remains held and uninstalled.

## Install a D4, R4 or F6

| Machine | Whole loose machine | Footprint |
|---|---|---|
| D4 dismantling fixture | 160 kg | 4 × 4 |
| R4 scrap reclaimer | 180 kg | 4 × 4 |
| F6 electric furnace | 240 kg | 6 × 6 |

1. Get the whole machine. Traders that carry Rivetline gear sell it, and salvage
   occasionally turns one up. See [where to buy](equipment-economy.md).
2. Choose **Install** on the loose machine, or pick it in **INSTALL > APPS**.
   Place and rotate its outline on suitable flooring, leaving room for crew and
   bulky deliveries. Installation needs a Mortorq.
3. A crew member hauls the machine to the site in the drag slot and fits it.
   You do not need to carry it there yourself.
4. Once it is installed, connect power and any supporting equipment it needs,
   then open **Control Panel**.

While a crew member works, the site shows the machine part-built: an early stage
until the machine arrives and work starts, then a half-fitted stage. The finished
machine appears only when installation completes. Pausing keeps the
current stage. The picture is worked out again from the site after a reload; it
never changes what the job needs.

A damaged machine keeps its own placement and repair path.

## Old assembly sections in your save

Earlier versions built these machines from two D4-S, two R4-S or three F6-S
assembly sections. That is gone: sections are no longer made, sold or found.
Saves that still hold sections are sorted out automatically aboard your own
ships, and the crew log reports what happened.

- **Complete sets become whole machines.** Any two D4-S, two R4-S or three F6-S
  aboard the same ship turn into one loose D4, R4 or F6, on the deck where the
  first of them lay. The machine weighs exactly what its sections did.
- **Leftover sections go back to materials.** A section without a full set
  becomes the scrap and parts its recipe used, on the deck where it lay:

  | Section | Steel scrap | Aluminium scrap | Small mechanisms | Small electronics |
  |---|---|---|---|---|
  | D4-S, 80 kg | 50 | 24 | 10 | 2 |
  | R4-S, 90 kg | 56 | 26 | 12 | 4 |
  | F6-S, 80 kg | 48 | 22 | 12 | 8 |

  A section made by the old cast-housing route returns the same bill; the
  housing's share comes back as aluminium scrap.
- **Build sites.** A section build site that already has every section delivered
  stays as it is: the crew can still finish it, and its saved work is kept. A site
  that is still waiting for sections can never finish now, so it is cancelled the
  way the game cancels any build site. Its delivered sections drop beside it and
  are then sorted out as above.
- **Not aboard your ship.** Sections in a trader's stock or on another ship are
  left alone. If you buy or bring one aboard, it is sorted out within about
  15 seconds.

A section someone is carrying is sorted out too, and the result lands at their
feet. An old table order for a whole machine can no longer find its sections;
cancel it from the table. An old order for a single section still finishes, and
that section is then sorted out like any other, so cancel it to save the work.

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

Owner direction, 1 October 2026: building a machine from several identical
sections was arbitrary and confusing, so the D4, R4 and F6 are bought or found
whole and saved sections convert automatically (Framework 0.67.0, Shipbreaker
0.60.0). Framework `LegacyItemConversions` runs the conversion on the live
objects of the player's own loaded ships; Framework `SectionAssembly` keeps the
old section jobs only for saved sites, hands INSTALL back to each machine's own
Install job and shows the construction stages on it. Shipbreaker owns the
section counts, and the leftover bill comes from its registered section recipes.

Primary implementation evidence is **Blue Bottle Games' Ostranauts 1.0.1.5**,
inspected locally: `Installables.Create` creates input lots and work actions;
`Placeholder.Cancel(CondOwner)` releases staged lots through the game's own drop
and destroys the marker; `Ship.DropCO(CondOwner, Vector2)` places items on nearby
free tiles, stacking where it can, and returns anything that did not fit; native
placeholder records save the exact source, action and installed identities.
[Blue Bottle Games' game page](https://bluebottlegames.com/games/ostranauts)
identifies the game, not a published copy of those internal methods. Proprietary
code and save files remain outside the repository.

The historical table integration retains its attribution to
[Ostranauts Crafting Framework](../mods/PhobosFramework/licenses/CraftingFramework-MIT.md).
The construction stages use four original PixelLab images with retained Phobos
frames and the existing D4-S and R4-S section sprites; see the
[artwork record](../assets/construction-stages/README.md). No new artwork was made
for this change.

Offline coverage: production conversion code runs against doubled native ships,
sites and spawns (complete sets, leftovers, mass balance, container and deck
anchors, overflow placement, missing or changed definitions, failed placement
rollback, complete and incomplete saved sites, other ships, idempotent repeats,
the poll's cadence and ownership). The native suite checks INSTALL, APPS against
the game's own `Installables.Create`, the retired table offers, the binding of the
game's cancellation and drop, and the conversion bills against the live
definitions and registered recipes. None of this runs Unity, native crew
pathfinding or the owner's save.

## Owner check

- Load a save that holds loose D4-S, R4-S or F6-S sections and, if possible, a
  partly built section site. Read the crew-log notice; check that complete sets
  became whole machines and leftovers became scrap and parts where they lay.
- Check that a section site with every section delivered can still be finished.
- Choose a D4, R4 or F6 in INSTALL > APPS: it should ask for the whole loose
  machine. Watch the early and part-built stages while the crew installs it, and
  save/reload partway through.
- On the affected F6-P, choose Recover stored cargo. Check quantities and stacks,
  unload everything, and confirm new deposits fail. Reopen Maintenance information
  to see any remaining coolant, temperature or paired-equipment restriction.
- Check a conduit stack and an agricultural appliance with contents. Confirm the
  explanation and normal maintenance after safely resolving the blocker.
