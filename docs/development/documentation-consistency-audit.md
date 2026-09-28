# Documentation consistency audit — 28 September 2026

## Findings and corrections

The owner's furnace-guide report exposed incomplete updates across linked guides.
This was a documentation fault: recent feature pages were accurate in isolation,
while older instructions and the reference generator still described earlier behavior.
No gameplay, save format, balance, translation keys or module versions change here.

| Finding | Corrected surfaces | Implementation evidence |
|---|---|---|
| Final assembly still described as table work or unavailable in INSTALL | Main player guide; generated D4/R4/F6 machine and section placement; furnace walkthrough | [SectionAssembly](../../src/PhobosFramework/Construction/SectionAssembly.cs), [assembly bills](../../src/PhobosShipbreaker/AssemblyDefinitions.cs), exported native jobs |
| Furnace operating guide omitted practical coolant service and hidden-cargo recovery | Furnace assembly checklist, piped/optional coolant distinction, local fill/drain, waste space, maintenance and recovery | [CoolantService](../../src/PhobosShipbreaker/CoolantService.cs), [CoolingCargo](../../src/PhobosShipbreaker/CoolingCargo.cs), [FurnaceOperations](../../src/PhobosShipbreaker/FurnaceOperations.cs) |
| Agriculture and saved-flight guides gave outdated current dependencies | Current-version links replace unmaintained numerical copies; introduction versions labelled historical | [Dependency catalogue](../../config/mod-dependency-minimums.json), [maintained constants](../../config/maintained-constants.json) |
| Fire-control guide retained an obsolete 60% retail offer | Link current generated offer probabilities/quantities instead | [Auto Nav reference](../auto-nav-item-reference.md#n3), [merchant policy](merchant-stock.md) |
| Old hub layout and console proposals read as current instructions | Point to compact scrolling controls; label old layout, missing-gauge and no-cutting statements as historical | [Control guide](../control-panel-guide.md), [reclamation](../shipbreaker-reclamation.md), [Combat](../auto-nav-combat.md) |
| Library links and handovers blurred delivered features and research | Current operating links, repaired punctuation and explicit historical context | [Manufacturing status](manufacturing-implementation.md), [furnace routing](../furnace-material-routing.md) |

## Language review

Followed [Writing for the crew](player-language.md) and AGENTS.md. The furnace
now opens with its useful output, required supplies and assembly choices. The
reader gets actions and consequences before implementation details. Control names,
brands, material quantities, temperatures, pressure limits and research credit stay
intact. Original technical evidence and historical release records are preserved.

The site-assembly wording is repaired in the maintained generator, not hand-edited
into its output. Tests cover all three machine/section pairs and retain the legacy
saved-job labels. No new translation strings are needed for this documentation fix.

## Coverage and limits

The inventory below covers the existing language-audit document set, plus its
language-rule and audit reports. Every listed document received a stale-instruction
and status screen. Source comparisons concentrate on the reported furnace chain,
assembly/handling, dependencies, current control layouts and linked operating guides.
“Retain after screen” does not mean every scientific claim or gameplay path has
been revalidated. Original research citations, historical numerical evidence and
release histories were not rewritten. This is not a live Unity test, a fresh
research review, or proof that all documentation can never become stale.

Reviewed sources include the current maintained item export, native generated
construction jobs, current services and English control labels. Blue Bottle Games'
[Ostranauts](https://bluebottlegames.com/games/ostranauts) is the underlying game;
its proprietary implementation evidence remains local. Research attribution is
retained beside the existing research claims rather than recast as game validation.

Checks for this change: fresh native item export; item-reference generation and
placement regressions; documentation links; language-ledger coverage/fingerprints;
constants and Workshop records. Versions and dependency minima remain unchanged.
Prepared package documentation is refreshed separately from installed gameplay files.

## Document inventory

| Document | Review disposition |
|---|---|
| [README.md](../../README.md) | Retain after current-instruction/status screen; no related correction identified |
| [SUPPORT.md](../../SUPPORT.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/README.md](../README.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/development/agriculture-bulk-storage-art.md](agriculture-bulk-storage-art.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/agriculture-bulk-storage-calculations.md](agriculture-bulk-storage-calculations.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/agriculture-bulk-storage-design.md](agriculture-bulk-storage-design.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/agriculture-bulk-storage-research.md](agriculture-bulk-storage-research.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/agriculture-bulk-storage.md](../agriculture-bulk-storage.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/agriculture-economy-evidence.md](agriculture-economy-evidence.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/agriculture-economy-review.md](agriculture-economy-review.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/agriculture-first-slice.md](agriculture-first-slice.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/agriculture-implementation.md](agriculture-implementation.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/agriculture-item-reference.md](../agriculture-item-reference.md) | Generated reference; retained or regenerated from maintained inputs |
| [docs/development/agriculture-living-visuals.md](agriculture-living-visuals.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/agriculture-loot.md](agriculture-loot.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/agriculture-nutrient-production.md](../agriculture-nutrient-production.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/agriculture-nutrient-recovery.md](agriculture-nutrient-recovery.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/agriculture-nutrient-solutions.md](../agriculture-nutrient-solutions.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/agriculture-player-guide.md](../agriculture-player-guide.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/development/agriculture-research.md](agriculture-research.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/agriculture-roadmap.md](agriculture-roadmap.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/agriculture-seed-production.md](../agriculture-seed-production.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/agriculture-treatment-economy.md](agriculture-treatment-economy.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/agriculture-water-conduits.md](../agriculture-water-conduits.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/animation-and-sound-direction.md](animation-and-sound-direction.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/artwork-completion.md](artwork-completion.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/artwork-resolution-policy.md](artwork-resolution-policy.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/asset-generation-policy.md](asset-generation-policy.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/asteroid-life-support-research.md](asteroid-life-support-research.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/auto-nav-combat.md](../auto-nav-combat.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/auto-nav-departure.md](../auto-nav-departure.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/auto-nav-docking.md](../auto-nav-docking.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/auto-nav-economy.md](../auto-nav-economy.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/auto-nav-fire-control.md](../auto-nav-fire-control.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/auto-nav-flight-profiles.md](../auto-nav-flight-profiles.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/auto-nav-hub-validation.md](auto-nav-hub-validation.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/auto-nav-instruments.md](auto-nav-instruments.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/auto-nav-item-reference.md](../auto-nav-item-reference.md) | Generated reference; retained or regenerated from maintained inputs |
| [docs/development/auto-nav-outstanding-audit.md](auto-nav-outstanding-audit.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/auto-nav-panel-layout-audit.md](auto-nav-panel-layout-audit.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/auto-nav-persistence.md](../auto-nav-persistence.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/development/auto-nav-polaris-startup.md](auto-nav-polaris-startup.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/auto-nav-pursuit.md](../auto-nav-pursuit.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/development/auto-nav-reclamation-validation.md](auto-nav-reclamation-validation.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/auto-nav-sensors.md](../auto-nav-sensors.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/auto-nav-torch.md](../auto-nav-torch.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/auto-nav-towing.md](../auto-nav-towing.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/auto-navigate-adaptation.md](auto-navigate-adaptation.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/development/auto-navigate-reuse-review.md](auto-navigate-reuse-review.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/automatic-material-routing.md](../automatic-material-routing.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/building.md](building.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/chemical-storage-and-process-fluids.md](chemical-storage-and-process-fluids.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/control-panel-guide.md](../control-panel-guide.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/crew-automation.md](../crew-automation.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/dependency-contingencies.md](dependency-contingencies.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/english-language-audit.md](english-language-audit.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/equipment-branding.md](equipment-branding.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/equipment-economy.md](../equipment-economy.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/development/equipment-value-audit.md](equipment-value-audit.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/first-furniture-experiment.md](first-furniture-experiment.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/fluid-conduits-and-irrigation-research.md](fluid-conduits-and-irrigation-research.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/fluid-network-operations.md](../fluid-network-operations.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/framework-author-guide.md](framework-author-guide.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/framework-bulk-storage.md](framework-bulk-storage.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/framework-item-reference.md](../framework-item-reference.md) | Generated reference; retained or regenerated from maintained inputs |
| [docs/development/furnace-connections-and-instruments.md](furnace-connections-and-instruments.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/furnace-coolant-conduits.md](../furnace-coolant-conduits.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/development/furnace-electrical-direction.md](furnace-electrical-direction.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/furnace-first-cycle.md](furnace-first-cycle.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/furnace-material-routing.md](../furnace-material-routing.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/furnace-player-guide.md](../furnace-player-guide.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/development/furnace-repair-castings.md](furnace-repair-castings.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/furnace-ui-and-art.md](furnace-ui-and-art.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/fusion-industry-roadmap.md](fusion-industry-roadmap.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/fusion-smelter-research.md](fusion-smelter-research.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/getting-started.md](../getting-started.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/health-chronic-and-traits.md](../health-chronic-and-traits.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/health-evidence-and-gaps.md](health-evidence-and-gaps.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/health-reference.md](../health-reference.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/health-treatments-and-drugs.md](../health-treatments-and-drugs.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/industrial-console-player-guide.md](../industrial-console-player-guide.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/development/industrial-control-console.md](industrial-control-console.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/development/industrial-control-mockups.md](industrial-control-mockups.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/install-catalogue.md](install-catalogue.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/installing-mods.md](../installing-mods.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/item-handling-audit.md](item-handling-audit.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/item-reference-maintenance.md](item-reference-maintenance.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/item-references.md](../item-references.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/limited-autopilot.md](limited-autopilot.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/localization.md](localization.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/locator-next-steps.md](locator-next-steps.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/locator-research.md](locator-research.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/manufacturing-handover.md](manufacturing-handover.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/development/manufacturing-implementation.md](manufacturing-implementation.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/manufacturing-item-reference.md](../manufacturing-item-reference.md) | Generated reference; retained or regenerated from maintained inputs |
| [docs/development/manufacturing-research.md](manufacturing-research.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/material-disposal-port-research.md](material-disposal-port-research.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/material-port-pairing.md](../material-port-pairing.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/medical-next-steps.md](medical-next-steps.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/medical-research.md](medical-research.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/medical-runtime-findings.md](medical-runtime-findings.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/medical-system-vision.md](medical-system-vision.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/merchant-stock.md](merchant-stock.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/mod-extension-survey.md](mod-extension-survey.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/modding-notes.md](modding-notes.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/pda-cartridge-ideas.md](pda-cartridge-ideas.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/performance-audit.md](performance-audit.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/performance-captures.md](../performance-captures.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/phobos-framework.md](phobos-framework.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/player-guide.md](../player-guide.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/development/player-language.md](player-language.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/polaris-interface-refresh.md](polaris-interface-refresh.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/powered-shipbreaking-design-findings.md](powered-shipbreaking-design-findings.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/powered-shipbreaking-research.md](powered-shipbreaking-research.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/processing-job-compatibility.md](processing-job-compatibility.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/project-direction.md](project-direction.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/public-release-readiness.md](public-release-readiness.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/residue-collector.md](../residue-collector.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/residue-material-contract.md](residue-material-contract.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/scrap-reclaimer.md](../scrap-reclaimer.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/section-assembly-and-maintenance.md](../section-assembly-and-maintenance.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/sensor-integration-research.md](sensor-integration-research.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/shared-completion-cues.md](../shared-completion-cues.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/shared-console-observations.md](shared-console-observations.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/ship-equipment-art-study.md](ship-equipment-art-study.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/shipbreaker-autopilot-handover.md](shipbreaker-autopilot-handover.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/shipbreaker-autopilot-research.md](shipbreaker-autopilot-research.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/shipbreaker-capture.md](shipbreaker-capture.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/shipbreaker-close-work-geometry.md](shipbreaker-close-work-geometry.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/shipbreaker-completion-cue.md](shipbreaker-completion-cue.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/shipbreaker-first-build.md](shipbreaker-first-build.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/shipbreaker-hull-intake.md](../shipbreaker-hull-intake.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/shipbreaker-hull-mounting.md](shipbreaker-hull-mounting.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/shipbreaker-item-reference.md](../shipbreaker-item-reference.md) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [docs/shipbreaker-material-uses.md](../shipbreaker-material-uses.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/shipbreaker-reclamation.md](../shipbreaker-reclamation.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/shipbreaker-room-load-mitigation.md](shipbreaker-room-load-mitigation.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/shipbreaking-material-processing-research.md](shipbreaking-material-processing-research.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/solar-system-economy-evidence.md](solar-system-economy-evidence.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/solar-system-economy.md](../solar-system-economy.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/terminal-social-network.md](terminal-social-network.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/underfloor-material-transport.md](underfloor-material-transport.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/updating-constants.md](updating-constants.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/vanilla-economy-audit.md](vanilla-economy-audit.md) | Retain after screen; dated evidence/design, not an operating guarantee |
| [docs/development/workshop-publication.md](workshop-publication.md) | Retain after current-instruction/status screen; no related correction identified |
| [docs/development/workshop-upload-preparation.md](workshop-upload-preparation.md) | Retain after current-instruction/status screen; no related correction identified |
| [workshop/PhobosAgriculture/page.bbcode](../../workshop/PhobosAgriculture/page.bbcode) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [workshop/PhobosAutoNav/page.bbcode](../../workshop/PhobosAutoNav/page.bbcode) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [workshop/PhobosFramework/page.bbcode](../../workshop/PhobosFramework/page.bbcode) | Corrected current guidance or historical/current distinction; reviewed against evidence above |
| [workshop/PhobosManufacturing/page.bbcode](../../workshop/PhobosManufacturing/page.bbcode) | Retain after current-instruction/status screen; no related correction identified |
| [workshop/PhobosShipbreaker/page.bbcode](../../workshop/PhobosShipbreaker/page.bbcode) | Corrected current guidance or historical/current distinction; reviewed against evidence above |

Housing reference entries also linked to the proposed heat-sink study as operating instructions. Both now link to the furnace guide’s implemented casting and table-finishing steps.

## Audience separation

Following the owner’s direction, 96 contributor, research, design and audit documents moved to `docs/development/`. The 49 existing player documents remain directly in `docs/`. A separate development index and this new audit extend the reviewed inventory; source history and research findings were not rewritten. Links, generation paths, language coverage and package copies follow the new layout. Packages retain offline guides and direct links to source evidence when source files are not included. No installed runtime changes are required.

## Completed verification

- 84 Python tests passed, including generator placement and repeatable documentation packaging.
- Fresh native export passed 12,695 definition/registration checks; 118 item definitions regenerated consistently.
- 1,505 repository file links and 6,155 links within the five packaged guide sets resolve. External URLs and fragments are not checked by these file-link checks.
- Language coverage passed for 1,791 unchanged catalogue entries, 151 documents and 14 additional surfaces. Constants and 98 generated release records passed consistency checks.
- All four installed runtime DLLs match the normal package DLLs byte for byte. Only offline documentation packages changed; the running game was not modified. Manufacturing remains uninstalled.
