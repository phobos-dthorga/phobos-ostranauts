# Steam Workshop publication drafts

Maintained page copy and generated per-version change notes. **Nothing in this
folder establishes that a Workshop item has been published.** Follow the
[publication workflow](../docs/workshop-publication.md).

| Mod | Main changelog | Steam page draft | Per-version Steam notes |
| --- | --- | --- | --- |
| Framework | [Changelog](../mods/PhobosFramework/CHANGELOG.md) | [Page](PhobosFramework/page.bbcode) | [Versions](PhobosFramework/releases) |
| Shipbreaker | [Changelog](../mods/PhobosShipbreaker/CHANGELOG.md) | [Page](PhobosShipbreaker/page.bbcode) | [Versions](PhobosShipbreaker/releases) |
| Auto Nav — provenance hold | [Changelog](../mods/PhobosAutoNav/CHANGELOG.md) | [Page](PhobosAutoNav/page.bbcode) | [Versions](PhobosAutoNav/releases) |
| Agriculture | [Changelog](../mods/PhobosAgriculture/CHANGELOG.md) | [Page](PhobosAgriculture/page.bbcode) | [Versions](PhobosAgriculture/releases) |
| Manufacturing — scaffold hold | [Changelog](../mods/PhobosManufacturing/CHANGELOG.md) | [Page](PhobosManufacturing/page.bbcode) | [Versions](PhobosManufacturing/releases) |

Edit each mod's main changelog and page description. Regenerate release notes
with `python scripts/workshop-release-notes.py --write`; never edit generated
version files independently. Baseline entries are explicitly unpublished drafts.
