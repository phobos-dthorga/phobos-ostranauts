# Claude handoff: story chatter prints grammar tokens

Owner-reported gameplay evidence, 6 October 2026. **Fix the shared Framework
dialogue formatting; preserve the authored story line.** This handoff changes no
code and does not claim a repair.

## Observed and intended

The owner's attached screenshot shows this line in a live conversation:

```text
[us] [asks] [them]: "If your ship's air started as somebody's rock, at what point did it become home?"
```

The following log row says Jorge White replied. This confirms one story line
reached the live conversation; it does not validate all story channels or social
effects. The tokens were never intended to appear literally. The lead-in should
use the actual speaker, listener and verb agreement, for example `You ask Jorge
White: ...` when those are the participants.

Entry: `spacertales-oxsmith-rock-question`, moment `question`, in
[04-process-makers.json](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story/04-process-makers.json).
The entry contains only the quoted sentence. Its lead-in comes from
`Story.moment.question` in
[Framework's English catalogue](../../translations/PhobosFramework/en.json):
`[us] [asks] [them]: "{0}"`.

## Investigation and scope

Start with [StoryChatter.cs](../../src/PhobosFramework/Story/StoryChatter.cs).
`Choose` fills story placeholders, composes the translated lead-in, then calls
`GrammarUtils.GetInflectedString(..., interaction)`. Both `GenerateDescription`
postfixes return that replacement through `Describe`.

**Hypothesis, not a verified cause:** the replacement is missing part of the
game's actor/pronoun and verb-expansion pipeline. Inspect Blue Bottle Games'
installed grammar implementation and a normal social description before choosing
the correction. Installed plugin versions and Player.log were not inspected for
this handoff.

Apply the correction to the shared path for all nine moments. Keep native
interaction identities, effects and AI history unchanged, and retain the memoized
choice so the conversation screen and social log use the same line. Avoid
recursively re-entering the patched description generator. Do not work around the
fault by hard-coding names in story JSON or deleting the lead-in.

## Verification

- Add a focused formatting regression check using the actual game pipeline, with
  player and NPC speakers, appropriate verb agreement and no unresolved lead-in
  tokens. Check that repeated descriptions retain the selected line.
- Owner-run reproduction: F3 command
  `phobosframework story chatter spacertales-oxsmith-rock-question`, then trigger
  a question moment through the game's own conversation. The force command affects
  the next matching moment; it does not initiate a conversation.
- Review the conversation and social log, then another moment such as joke or
  complaint. Offline checks do not substitute for this gameplay review.

Use the normal Framework version, changelog, localization, build and installation
workflow for the eventual fix. The
[story authoring guide](../writing-story-content.md) documents the moments and
force command; no schema extension is needed.
