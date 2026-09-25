# Updating the equipment and item references

Double-click `scripts/update-item-reference.cmd` in a source checkout. It uses
the installation path already saved for our installer. If none is available,
it asks for the game folder. PowerShell 7, .NET 10 and Python are required, as
for the existing build/audit tools. The game may remain open: this workflow
reads local definitions and never accesses a running scene, save or inventory.

For maintainers and automated use:

```powershell
./scripts/update-item-reference.ps1
# Override the local installation setting:
./scripts/update-item-reference.ps1 -OstranautsPath '<your game folder>'
# Read-only comparison against fresh native data; tracked files stay untouched:
./scripts/update-item-reference.ps1 -Check
```

## What one update does

1. Reuses the economic audit to refresh the vanilla comparison, Phobos equipment
   value report and Agriculture economic evidence, while running the native
   definition checks. Check mode runs the native checks without rewriting those
   older reports.
2. Builds a data-only snapshot from the actual prepared content definitions,
   native valuation, recipes and additive acquisition registrations. It records
   the game assembly hash, source fingerprints and current mod versions.
3. Matches every item definition to a reviewed explanation in
   `config/item-reference.json`. Unknown, duplicated or obsolete entries stop
   generation. Built-in inventories are explicitly covered as compartments;
   Framework's shared repair waste is documented once in its own guide.
4. Regenerates [the index](item-references.md) and all six mod references, with
   economic tables, native INSTALL categories, service bills, dismantle outputs,
   acquisition probabilities and table recipes. Functional and damaged forms
   are separate; identical installed/loose valuations are folded together.
5. Checks maintained constants, Workshop records and documentation links.

The updater deliberately does not invent use instructions for a new item.
Add its function, use, acquisition route, limits and operating-guide link to
the catalogue, then rerun. Update these explanations when behaviour changes,
even if the price remains the same. Prices, input bills and outputs belong in
their owning source definitions, never in hand-edited generated tables.

## Source ownership and verification

`docs/*-item-reference.md`, `docs/item-references.md` and
`docs/item-reference-data.json` are generated. The JSON snapshot contains
selected factual results, not proprietary source, game files or machine paths.
The export is in `tests/PhobosNative.Tests/ItemReferenceExport.cs`; the renderer
and coverage checks are in `scripts/update-item-reference.py`.

The snapshot fingerprints source, native mod JSON, translations and exporter
inputs. A source change requires a fresh local export; editing narrative alone
can be rendered using `python scripts/update-item-reference.py`. CI can verify
the saved evidence and generated text without access to the proprietary game:

```powershell
python scripts/update-item-reference.py --check
python -m unittest discover -s tests -p test_item_reference.py
```

CI checks freshness against the recorded inputs; it does not execute Unity or
independently reproduce native valuation. The full PowerShell `-Check` command
re-exports against the installed game. Source hashes ignore line endings so the
Windows export is verifiable on Linux CI. Use the constants updater for registered
versions/defaults before refreshing the references. Keep original IDs and old
job contracts intact.

Add a new content provider to the exporter and renderer's mod inventory when
creating a new mod. Extend acquisition labels and tool/material names when a
new source or service input appears; unknown mappings fail instead of exposing
an unexplained internal identifier to players. The existing prototype's native
inheritance and Manufacturing's empty scaffold are explicitly labelled.

## Delivery and limits

The shared packaging helper includes the references, index and this guide in
newly prepared packages. Updating documents does not rebuild those packages,
install mods, publish to Steam or change release status. Review the generated
diff, owning mod changelogs and Workshop drafts in the same change. If refreshing
the old audits succeeds but a later check fails, those audit files may already
have changed; fix the reported error and rerun. All guide content is validated
and rendered before guide files are replaced.

Economic figures are definition values, not actual merchant quotes. Acquisition
probabilities apply to eligible rolls, not entire ships. Construction times are
configured work, not measured crew duration. Native service timing is left to
the operating guides because wear, skills, tools and saved jobs affect it.
Live fluid, waste and part-used cartridge masses can differ from their template.
Research-backed explanations retain direct attribution in the operating guides;
authored prices, yields and simplified chemistry remain labelled as gameplay.
