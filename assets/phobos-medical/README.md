# Halewright Ward-3 nanomedical artwork concept

4 October 2026. Original text-generated artwork from OpenAI's built-in Imagegen,
retained for owner review. The owner requested a 2075 infirmary bed in the style
of Blue Bottle Games' Ostranauts, fictionally able to repair moderate to somewhat
severe wounds through nanomachines.

[Untouched source](source/ward3-nanomedical-chatgpt.png),
[exact requests and provenance](requests.json),
[48 x 80 preview with a 4x enlargement](ward3-nanomedical-preview.png).
The source is 971 x 1619; the preview uses a 24-colour median-cut palette with no
dithering, followed by nearest-neighbour reduction of the whole original canvas.
White margins are retained. This is not an aligned runtime export.

Local vanilla artwork informed the style observation only: grey structural
rails, segmented padding and a functional overhead arrangement. No game image
was copied into the project or supplied to a generator. Original Phobos palette
colours were the only image input supplied to PixelLab for the superseded fitting.

The first hospital-bed base, fitting and their palette/preview were superseded
when the owner changed direction. Five exact binaries were verified and retained
on the existing `codex/rejected-artwork` branch, local commit
`fd98de450ee10d346a041cfea4a928737b848ffc`. The archive commit has not been pushed;
the [archive inventory](../rejected-artwork-archive.json) records hashes.
The bed requests used two built-in image calls and one included PixelLab generation.
PixelLab allowance changed from 1671 to 1670; credits remained at USD 0. Built-in
Imagegen does not disclose a monetary cost in its response.

Nanomachine wound repair is fictional design intent, not a scientific claim or an
implemented treatment. No care rules changed.

## Selected and exported (Phobos Medical 0.1.1)

The owner selected this concept on 4 October 2026. [register-ward3.py](register-ward3.py)
registers it mechanically as the 96 x 160 working master
[ward3-medical-bed.png](../artwork-completion/source/ward3-medical-bed.png): the white
margin removed by flood fill from the canvas edges, the silhouette widened about a fifth
to 44 native px across (the game's own medical bed is opaque over 44 x 80), a 96-colour
palette with no dithering so the teal cartridge caps and side plates survive (32, 48 and
64 colours lost them), centred with a 4 px side and 2 px top and bottom margin. The
completion exporter then writes the 48 x 80 native image and a neutral normal map;
`--check` on either script verifies the files byte for byte. Damaged forms take the
game's damage tint over the same image; no separate damaged art was generated. No
further provider calls were made for the registration. Owner in-game review pending.
No gameplay or institutional endorsement is implied.

## Companion styling and Vigil-2 pilot

Owner clarification, 4 October 2026: keep companion Medical equipment styled like
the selected Ward-3. Its off-white enamel, mid-grey trim, dark mechanical recesses,
muted blue-teal accents, folded service mechanisms and restrained pixel treatment
are the Halewright visual baseline. Pale mint-grey upholstery belongs on suitable
patient surfaces. Technical requirements and each device's function still apply.

The [monitor original](source/vigil1-monitor-chatgpt.png) was generated with that
Ward-3 source as its only image reference, using the handoff's exact prompt.
The unchanged 1254 x 1254 output and its prompt/hash are retained. One built-in
image call was used, with no PixelLab generation. The original one-tile, 16 x 16
plan was revised after the owner requested a more suitable size. The agent chose
a 2 x 2 cart, now named Vigil-2 to follow Halewright's width-based model convention.
The exact initial Vigil-1 prompt and original filename are retained as history.
[register-vigil2.py](register-vigil2.py)
and the shared [registration helper](registration.py) prepare the
[128 x 128 master](vigil2-monitor-master.png),
[32 x 32 native pilot](vigil2-monitor-native.png), neutral normal and
[native/8x review](vigil2-monitor-preview.png). Edge-connected white is removed,
the body fills 112 x 112 at (8,8), and a 96-colour palette reduction matches the bed
before nearest-neighbour native export. The Ward-3 registration remains byte
identical after sharing the helper. Use either registration script with `--check`.

The monitor is prepared artwork for a future feature, with no runtime binding
or healing change; owner review is pending. The request record identifies the
prepared source separately from the owner-selected and delivered Ward-3. The
superseded one-tile derivatives are retained in the rejected-artwork archive;
the request record and archive inventory identify the exact commit and hashes.

Provider terms references are separate from code licensing:
[OpenAI Terms of Use](https://openai.com/policies/terms-of-use/) and
[PixelLab Terms of Service](https://www.pixellab.ai/termsofservice).
These links record provenance; this pass makes no new legal determination.

Medical 0.3.0 binds the Vigil-2: the completion manifest exports its 32 x 32 image at sample
phase (1, 1), the same phase `register-vigil2.py` uses for the native pilot.
