# Line lane art

Framework 0.56.0 lets different line families share a tile (owner decision,
30 September 2026). Each family draws its pipe in its own lane of the 16 x 16
tile so none hides another, and each has its own draw layer (`LineLayers` in
Framework: belts lowest, then process water, gas, acid, irrigation, coolant).

`scripts/export-line-art.py` draws every family procedurally and deterministically;
no provider is called and no game texture is used. A pipe is 3 pixels wide in lane
n (rows or columns 1+3n to 3+3n), lit from the upper left, with a 3 x 3 fitting where
arms meet and a darker collar every four pixels. Colours are the original irrigation
conduit's own palette (from the 25 September 2026 conduit master registered in
`assets/phobos-agriculture/irrigation-layers.json`), recoloured per family by a
recorded luminance ramp:

| Family | Lane | Colours | Runtime files |
| --- | --- | --- | --- |
| Irrigation (Agriculture) | 3 | original steel with a cyan fitting | `WaterPipe*` |
| Gas (Fennmark) | 1 | the propellant line's amber ramp, kept | `PropellantPipe*` |
| Coolant (Shipbreaker) | 4 | green (cooling water) | `FurnaceCoolantPipe*` |
| Process water (Framework 0.57.0) | 0 | blue | `ProcessWaterPipe*` |
| Acid (Lixivar, Manufacturing 0.24.0) | 2 | violet, the pipeline identification colour for acids and alkalis in BS 1710 (British Standards Institution) | `AcidPipe*` |

The Rivetline conveyor belt (Framework 0.61.0) is drawn by the same script beneath every lane:
a 12-pixel band of dark belting with raised cross-cleats every third pixel between steel
side rails, in the lowest layer, with the same joint mask; its loose icon is a straight run
(`ConveyorBelt*`, colours recorded under `belt` in the export record).

The loose icon is the full cross in the middle lane. Sheets follow the game's joint
mask order (N=8, W=4, E=2, S=1, bottom-left first) with flat normals.
`line-art-exports.json` records the ramps, lanes and export hashes;
`lanes-preview.png` is the native-scale pilot (x4) inspected before export.
The earlier full-width pipe art (irrigation master export, its byte copy for the
coolant line and the amber recolour for the gas line) is superseded; the conduit
master stays registered. In-game appearance awaits the owner's check.
