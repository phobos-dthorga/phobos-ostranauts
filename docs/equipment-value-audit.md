# Phobos equipment value audit

Generated from current Phobos candidate definitions and Ostranauts 1.0.1.4's `DataCO.GetBasePrice` (2026-09-24). Includes every implemented equipment family, both functional and broken forms, and the assembly section. No game session or live merchant quote was sampled.

All dollar figures below are **whole-object values**, not prices per kilogram or shop purchase quotes. Recovered parts are valued at full condition without a retail pristine flag. Work, power and tool costs are excluded.

| Item/state | Whole base | Whole at maximum wear tier | All dismantling outputs | Output / whole base | VORB adverse comparison: whole / scrap |
|---|---:|---:|---:|---:|---:|
| Phobos Powered Dismantling Fixture | $12,000.00 | $3,000.00 | $514.10 | 4.28% | $1,200.00 / $257.05 |
| Phobos Powered Dismantling Fixture (Damaged) | $3,000.00 | $750.00 | $365.40 | 12.18% | $300.00 / $182.70 |
| Phobos Exterior Panel Grabber | $6,400.00 | $1,600.00 | $258.55 | 4.04% | $640.00 / $129.27 |
| Phobos Exterior Panel Grabber (Damaged) | $1,600.00 | $400.00 | $167.15 | 10.45% | $160.00 / $83.57 |
| Phobos Sealed Hull Chute | $1,800.00 | $450.00 | $150.15 | 8.34% | $180.00 / $75.07 |
| Phobos Sealed Hull Chute (Damaged) | $450.00 | $112.50 | $85.00 | 18.89% | $45.00 / $42.50 |
| Phobos Scrap Reclaimer | $14,800.00 | $3,700.00 | $637.60 | 4.31% | $1,480.00 / $318.80 |
| Phobos Scrap Reclaimer (Damaged) | $3,700.00 | $925.00 | $450.00 | 12.16% | $370.00 / $225.00 |
| Phobos Residue Collector | $2,400.00 | $600.00 | $88.50 | 3.69% | $240.00 / $44.25 |
| Phobos Residue Collector (Damaged) | $600.00 | $150.00 | $41.45 | 6.91% | $60.00 / $20.72 |
| Dismantling Fixture Assembly Section | $4,800.00 | $4,800.00 | $257.05 | 5.36% | $1,920.00 / $128.52 |
| Scrap reclaimer assembly section | $6,000.00 | $6,000.00 | $318.80 | 5.31% | $2,400.00 / $159.40 |
| Phobos Auto Nav | $3,600.00 | $900.00 | $0.01 | 0.00% | $360.00 / $0.00 |
| Phobos Auto Nav (Damaged) | $900.00 | $225.00 | $0.01 | 0.00% | $90.00 / $0.00 |

The VORB column compares the **lowest whole-item value at the lowest native buyer multiplier** against **fresh output at the highest buyer multiplier**. It excludes supply/demand, negotiation, travel and labour. An assembly section has no wear stat. Broken equipment has its own lower base price; additional wear can reduce that again.

This is a conservative vanilla baseline, not a guarantee across different regions, category shortages, blockades or mods which alter prices. VORB's broad buyer is checked against every item above. Other merchants can refuse a whole machine or its scrap entirely.

## Construction and repair material values

| Recipe output | Construction inputs, base value | Dismantling outputs, base value |
|---|---:|---:|
| Dismantling Fixture Assembly Section | $285.40 | $257.05 |
| Powered Dismantling Fixture | $9,600.00 | $514.10 |
| Sealed Hull Chute | $176.40 | $150.15 |
| Exterior Panel Grabber | $340.00 | $258.55 |
| Residue Collector | $106.60 | $88.50 |
| Scrap Reclaimer Assembly Section | $348.20 | $318.80 |
| Scrap Reclaimer | $12,000.00 | $637.60 |
| Phobos Auto Nav | $29.00 | $0.01 |

The processor's final assembly consumes two priced sections. Raw materials for both sections total $570.80; its $514.10 dismantling yield is also below that original raw-material bill. Construction creates a usable machine through labour; this is separate from the dismantling comparison.

| Repair target | Replacement inputs, base value |
|---|---:|
| Phobos Powered Dismantling Fixture | $94.60 |
| Phobos Exterior Panel Grabber | $57.30 |
| Phobos Sealed Hull Chute | $18.30 |
| Phobos Scrap Reclaimer | $104.60 |
| Phobos Residue Collector | $40.10 |
| Phobos Auto Nav | $29.00 |

Repair values exclude purchasing markups, reusable tools, work and subsequent Restore. Repair returns equal-mass spent material; it does not mint fresh valuable components.
