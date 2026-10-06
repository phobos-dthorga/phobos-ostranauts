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

## Resolution (Framework 0.121.0, Claude, 6 October 2026)

**Cause, observed in the decompiled game code, not yet in play.**
`GrammarUtils.GetInflectedString` returns its input unchanged unless that exact string is a
key of `GrammarUtils.inflectedStrings`. The game fills that table once, at load, through
the private `DataHandler.PrepareInflectedString(object, string)` for every interaction
and condition description. `StoryChatter.Choose` composed a new string (lead-in plus line)
that the game had never prepared, so its tokens came back as written.

**Fix.**
- The new shared `Social.Grammar.Inflect`
  ([Grammar.cs](../../src/PhobosFramework/Social/Grammar.cs)) prepares the moment's
  lead-in once through the game's own method, found by name and parameter types, with a
  placeholder where the line goes.
- It then inflects the lead-in for the interaction's speakers and only afterwards puts
  the line in. The table therefore gains nine lead-ins per language, and brackets in a
  story line are never read as tokens.
- The memo, the native interaction, its effects and AI history are unchanged.
  `GenerateDescription` is never re-entered.
- If the method is missing in a later game version, or a token stays unexpanded, the
  game's own line is kept and the log says so once.

**Checked offline.** The native suite loads the game's own `tokens/` files, unpacks them
with the game's `UnpackTokens`, and confirms that every token of all nine lead-ins
prepares. Full inflection needs live speakers, so the owner reproduction above remains
the gameplay check.

Use the normal Framework version, changelog, localization, build and installation
workflow for the eventual fix. The
[story authoring guide](../writing-story-content.md) documents the moments and
force command; no schema extension is needed.
