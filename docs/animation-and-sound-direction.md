# Future animation and restrained sound cues

Owner direction, **25 September 2026**, across Phobos Ostranauts mods.
The subsequent authorized [suite-wide implementation](shared-completion-cues.md)
adds watched meals, crop readiness and approach/rendezvous arrival with one
Framework playback channel. PixelLab animation remains future work.
This records future possibilities and a selective retrofit direction. The owner
subsequently authorized the first trial: Shipbreaker 0.18.0 prepares one optional
[watched D4/R4 completion cue](shipbreaker-completion-cue.md), with reproducible
procedural audio. It is not installed or gameplay/listening-validated by this change.
Animation remains future work. Recommendations below are design proposals, not additional
requirements attributed to the owner or measured usability results.

## PixelLab animation: retained for future use

PixelLab's [official MCP guide](https://api.pixellab.ai/mcp/docs#create_vocal_animation),
checked 25 September 2026, documents `create_vocal_animation` for portrait mouth
shapes (visemes), `create_talking_gif` for sequencing them from dialogue text,
and `get_lip_sync` for a spritesheet and frame plan usable by game code.
These are **visual animation outputs**, not generated voice recordings.
Letter-derived timing is not evidence of alignment to an independently generated
voice track. Review actual speech timing separately if speech is ever needed.

Possible future uses include a brief speaking portrait in a communications or
service interface. No portrait feature is selected for implementation. First
identify a useful interface and inspect its native presentation support; do not
assume an exported GIF will play directly in Ostranauts. Prefer retained frames
and a small native-compatible presentation adapter if a concrete use warrants it.

Reuse approved portraits, keep a static alternative, avoid distracting idle
motion, and preserve registration and provenance under the
[asset-generation policy](asset-generation-policy.md) and
[resolution policy](artwork-resolution-policy.md). Recheck tool capabilities and
cost before generation. This future-use note does not start a paid job.

## Sound: owner requirements

- Consider adding sounds **retroactively to existing equipment and interactions**
  as well as future features, possibly through ChatGPT-assisted synthesis.
- Require **HIGH or MEDIUM-HIGH usefulness**. Decoration alone does not qualify.
- Keep the sound non-alarming, non-startling, non-distracting and quiet.
- Avoid taking attention away from other elements of the game without an
  exceptionally strong reason. This is not a general exception allowing louder
  samples or a new alarm system.
- Each sample must be **brief**.

## Proposed selection criteria

Call a candidate **HIGH** when it helps notice a consequential change requiring
action which is otherwise easy to miss. Call it **MEDIUM-HIGH** when it reliably
reduces repeated checking or uncertainty during a deliberate task. These ratings
are provisional judgments: downgrade or omit the cue if native feedback already
does the job, it sounds frequently, or its meaning needs repeated explanation.

| Possible retrofit | Provisional value | Benefit and conditions |
| --- | --- | --- |
| Unexpected suspension of attended navigation or a watched processing job | HIGH | One quiet indication that the requested operation needs intervention, with the exact reason in text. Exclude deliberate Stop, ordinary pauses and repeated reports of the same blockage. |
| Completion of a specifically watched finite industrial/cooking job | MEDIUM-HIGH | Reduce progress checking. Confirm actual completion; do not imply a hot furnace is safe to open or that pending outputs were physically delivered. Group simultaneous completions. |
| Arrival or completed docking after a player-requested manoeuvre | MEDIUM-HIGH, conditional | Mark the completed goal only if native feedback is insufficient. Keep Approach arrival and actual docking distinct; do not sound success at an intermediate phase. |
| Explicit remote command accepted/rejected where feedback is otherwise unclear | MEDIUM-HIGH, conditional | A subtle response may remove uncertainty. Prefer improving the visible response first; ordinary local buttons do not qualify automatically. |

Skip background machinery loops, pump/pipe ticks, per-item transfer noises,
per-plant growth/harvest pings, every weapon shot, routine tab clicks, startup
jingles and spoken status chatter. Do not duplicate Blue Bottle Games' native
alarms or imply that our sounds replace their warning system. Native cue coverage
and mixing still need inspection for each selected use; no new audio API or
playback behaviour is assumed here.

## Recommended listening and event behaviour

- Start with a restrained **100–350 ms** sample; consider up to **500 ms** only
  when two soft notes improve recognition. These are proposed authoring targets,
  not owner-specified numbers or research findings. Include a short fade at each
  end; avoid clicks, sharp attacks, piercing tones, bass impacts and siren shapes.
- Judge volume in the game's actual mix, not from a waveform peak alone. Offer
  Phobos cue volume and mute, respect native sound-effects volume/mute, and keep
  every meaning visible in text/status. The game must remain fully usable muted.
- Use one consistent meaning per sound. Begin with at most two related cues:
  completion and needs-attention. Confirmations must not resemble native danger
  signals; no celebratory flourishes or escalating repetition.
- Play once for a genuine transition. UI refresh, panel reopening, save loading,
  failed repeated polling and fast-forward must not replay old events. A cue that
  is still relevant can retain its visual message without repeating audio.
- Restrict playback to the attended console, nearby relevant equipment or an
  explicitly watched task. Do not announce every ship or machine globally.
- Coalesce bursts, prevent overlapping Phobos cues and drop stale/low-priority
  cues rather than queueing a delayed chorus. Native critical warnings take
  priority; use an established signal if one exists, not an invented alarm API.
- Let content services establish completed/blocked facts; presentation consumes
  them without changing jobs, flight state or inventory. A playback failure must
  never interrupt game controls or business logic. Shared mixing/deduplication
  belongs in Framework only when concrete consumers require it.

## Synthesis and a small first trial

For short nonverbal cues, the proposed first route is **ChatGPT-authored procedural
synthesis**: reproducible code creates and exports the waveform. Label that
provenance accurately; it is not an audio-model recording. It needs neither a
speech API nor a runtime connection to ChatGPT. Retain the synthesis settings,
seed if applicable, source/export hashes, duration, sample rate and channel count.
Choose the final encoding after inspecting the native loader.

OpenAI separately supports generated speech through its
[text-to-speech API](https://developers.openai.com/api/docs/guides/text-to-speech).
That is a different production route and is not selected here: speech generally
uses more time and attention than the proposed short nonverbal cues. Record
provider/model and applicable terms if that route is chosen for a concrete need.

Start with one qualifying event and one quiet sample. Compare with native
feedback, listen at ordinary volume on speakers and headphones, then evaluate
normal speed, fast-forward, reload, repeated panel opening, mute and concurrent
events. Remove it if it is annoying or does not reduce checking. Expand only
after the first cue proves useful in the owner's play; no numerical worthiness
score substitutes for that judgment.

When implementation occurs, update the owning changelog, Workshop draft,
localization and asset provenance together. Keep the first integration narrow;
do not build a speculative audio framework or revise unrelated equipment.
