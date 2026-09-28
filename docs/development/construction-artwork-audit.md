# Construction artwork audit — 28 September 2026

## Result

Construction is mechanically staged, but its appearance is not. D4, R4 and F6 share delivery, work, cancellation and completion handling; none has an explicit construction-stage image policy. The D4 owner report is consistent with the marker using the completed machine's image. All 18 D4 colour, normal and portrait files match the installed copies byte for byte.

The owner successfully assembled R4, observed image changes and automatic collection of the second section, then assembled D4 while seeing only its finished form. These are live observations. They do not identify which native object or effect changed during R4 construction. No recording of those transitions was inspected; this audit does not reclassify the R4 observation as a designed animation.

Audit only: no runtime, artwork, balance, save or installation changes.

**Subsequent artwork preparation, 28 September:** the owner then authorized the
four-image batch. [D4/R4 intermediate and F6 early/intermediate artwork](../../assets/construction-stages/README.md)
is now retained with registered exports and provenance. This closes the proposed
new-image requirement, not the missing code binding described below. No runtime
construction policy or installed package changed; Unity review remains pending.

## Evidence and scope

Reviewed all 16 intact placeable families in the [current native export](../item-reference-data.json), their definition/artwork paths, all three section assembly registrations, Framework's assembly hooks, native placeholder creation/update, existing appearance adapters and test coverage. Damaged and loose forms are considered with their families; internal compartments and portable stock are not separate construction projects.

Primary game evidence is **Blue Bottle Games' installed Ostranauts 1.0.1.5 assembly**, SHA-256 `91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`, matching the export. `DataHandler`, `Placeholder` and `Item` were freshly inspected with the existing local ILSpy tool. No new tool was installed. Proprietary decompilation remains local. [Blue Bottle Games' official game page](https://store.steampowered.com/app/1022980/Ostranauts/) identifies the game; it is not a public source for its internal methods.

## Confirmed findings

| Priority | Finding and evidence | Consequence / proposed repair |
|---|---|---|
| High | [SectionAssembly](../../src/PhobosFramework/Construction/SectionAssembly.cs) records material, output, count, mass and work. Initialize only sets the work maximum. No artwork fields or stage selector exist. | Add a small, opt-in presentation contract for section assembly. Leave construction validation unchanged. |
| High | Native `DataHandler.GetCOPlaceholder` sets the marker Item from the installed cursor's definition. [AssemblyDefinitions](../../src/PhobosShipbreaker/AssemblyDefinitions.cs) registers each completed Installed identity as `strStartInstall`. | Apply unfinished artwork to the existing marker without changing its target, item definition, sockets or collision geometry. |
| High | Shared hooks cover creation, selection, completion and pooled interaction reset. Native `Placeholder.Update` requeues work, not progress pictures. Existing save/footprint repairs do not select construction images either. | Define visual updates for creation, delivery/removal, work changes, reload, completion, cancellation and object reuse. Suppress unchanged updates. |
| Medium | F6 section art is 64×64 / 4×4 tiles; the completed furnace is 96×96 / 6×6. D4 and R4 section/completed images are each 64×64. | Review an explicitly registered 6×6 composition or suitable existing image. Do not blindly stretch a component to fill the furnace. |
| Medium | No per-delivered-section or progress-stage list exists for D4/R4/F6. Current variants describe installed, loose, damaged and component states. | Existing unfinished art supports a first unfinished-to-complete change. More detailed stages need defined visual meaning and asset review. |
| Medium | [Assembly tests](../../tests/PhobosAssembly.Tests/Program.cs) use [doubles](../../tests/PhobosAssembly.Tests/NativeDoubles.cs) with no Item/renderer. [Native checks](../../tests/PhobosNative.Tests/AssemblyNativeChecks.cs) verify work contracts, not rendered stages. | Add stage-policy tests, native binding checks and owner-run visual acceptance. Delivery tests are not artwork validation. |
| Low | The dated [furnace art record](../../assets/phobos-furnace/README.md) describes an earlier reused D4 section and native unfinished-equipment display. Current [definitions](../../src/PhobosShipbreaker/FurnaceDefinitions.cs) use a dedicated F6 section. | Preserve history; future construction-art documentation must identify the actual stage contract and current provenance. |

## Family coverage

| Family | Current path | Decision |
|---|---|---|
| D4 dismantling fixture | Two D4-S sections; dedicated installed, loose, damaged and section art | Missing section-site appearance binding; existing section art is a candidate. |
| R4 scrap reclaimer | Two R4-S sections; dedicated section and equipment-state art | Same missing binding. Owner-reported visual transitions remain unexplained. |
| F6 electric furnace | Three F6-S sections; section and equipment-state art | Same missing binding plus footprint/registration review. |
| H4 hull chute | Complete loose item installation; state art | No section bill; do not invent multi-part construction. |
| G4 exterior grabber | Complete loose item installation; state art | Same; preserve exterior mounting. |
| C2 residue collector | Complete loose item installation; state art | Same; cargo and pairing are separate concerns. |
| C1 industrial console | Complete loose item installation; dedicated state art | No progress animation; panel artwork is unrelated. |
| F6-R radiator | Complete loose item installation; state/coupling art | No section-site stages; preserve attachment points. |
| F6-P thermal exhaust port | Complete loose item installation; connection/orientation art | [Connection view](../../src/PhobosShipbreaker/FurnaceConnectionView.cs) reflects connections, not construction. |
| F6-C coolant conduit | Native placement/connectivity art | Connectivity is not assembly progress. |
| Firstlight-4 rack | Complete loose item installation; state/crop art | [Agriculture Artwork](../../src/PhobosAgriculture/Artwork.cs) reflects crops and equipment state, not construction. |
| Hearth-2 cooker | Complete loose item installation; state art | No section bill or construction-stage selector. |
| Groundwork W2 supply | Complete loose item installation; state art | No section bill or construction-stage selector. |
| Groundwork B2 workup bench | Complete loose item installation; state art | No section bill or construction-stage selector. |
| Groundwork R3 reservoir | Complete loose item installation; [explicit state images](../../src/PhobosAgriculture/BulkDefinitions.cs) | No section bill or construction-stage selector. |
| Irrigation conduit | Native placement/connectivity art | No staged assembly requirement. |

Auto Nav has slot modules rather than placeable machinery. Framework supplies shared services and spent parts, but no placeable family. Manufacturing remains a scaffold with no implemented machine. None needs speculative stage sprites for this fix.

## Important distinctions

- [D4 Content](../../src/PhobosShipbreaker/Content.cs) explicitly assigns its five equipment images. Its absence from the later generic `ApplyStateArtwork` list is **not** missing state registration: D4 uses a different filename convention (`InstalledDmg` rather than `Damaged`). Adding it blindly would reference nonexistent names.
- [ReclaimerDefinitions](../../src/PhobosShipbreaker/ReclaimerDefinitions.cs) initially assigns one base image, then Content applies loose/damaged images through [ApplianceDefinitions](../../src/PhobosFramework/Registration/ApplianceDefinitions.cs). Include that second pass when auditing the final definition.
- A section sprite belongs to a material item. Its presence does not make the construction marker use it. A carried component changing to an installed machine is not a progress animation.
- The completion gate already checks the exact full material bill. A finished-looking marker does not prove that the machine can operate early.
- No Phobos construction renderer or container alternate-image map was found that explains a deliberate R4-only stage sequence. Native effects, separate material objects and final replacement remain possible explanations, not confirmed causes.

## Recommended first implementation

1. Add an optional appearance descriptor to Framework's existing section-assembly registration; keep image choices in Shipbreaker.
2. Start with placement preview, unfinished construction and completed machine. Display delivered/required parts and work progress separately where useful. Do not imply usability before completion succeeds.
3. Reuse D4/R4 unfinished art after checking registration, rotations and normal maps. Review F6 at its actual 6×6 footprint before choosing existing art or requesting new assets.
4. Derive appearance from the current registered marker, delivered materials and native work values. Save no separate visual-stage state. Rendering must not consume parts, advance work, change identity, power machinery or authorize completion.
5. Prefer existing lifecycle changes or a bounded active-site refresh; no global per-frame scan or repeated material assignments. Preserve marker highlighting, lighting, selection and geometry. `Item.SetAlt` is a candidate, not yet proven to retain every native placeholder effect.
6. Cover 0/1/2 deliveries (and 3 for F6), work boundaries, invalid/removed material, cancellation, completion, reload, damage, missing artwork, foreign markers, whole-machine installation, rotation and object reuse. Verify unchanged-image suppression and safe fallback alongside existing material/tool/completion tests.

## Verification limits

The existing production assembly/recovery suite was rerun for this audit. It checks functional rules, not Unity rendering. Fresh native inspection and file/hash checks support the findings; no construction was replayed in the owner's game and no save was modified. Live D4/R4/F6 visual checks remain necessary after implementation.
