# Translation catalogs

Framework 0.7.0, Shipbreaker 0.7.0 and Auto Nav 0.3.0 introduce shared
localization for mod controls, equipment descriptions, construction text,
console responses and settings descriptions. Only English is supplied today.
Community translations can be partial; missing or invalid entries retain a
fallback. Native game text remains owned by the game.

## Player language and community overrides

After the first launch, set `Language` under `[Localization]` in
`BepInEx/config/phobosgekko.ostranauts.framework.cfg`. Use a language tag such as
`en`, `fr`, or `pt-BR`. The default `auto` reads the native game's language.
Restart the game after changing languages or catalogs; this is not a live editor.

Place UTF-8 JSON overrides under `BepInEx/config/PhobosTranslations/<owner>/`:

| Mod | Owner directory |
| --- | --- |
| Framework | `phobosgekko.ostranauts.framework` |
| Shipbreaker | `phobosgekko.ostranauts.shipbreaker` |
| Auto Nav | `phobosgekko.ostranauts.autonav` |

Name each file after its language, for example `fr.json`. Copy desired keys from
the repository's `translations/<Mod>/en.json` or the installed plugin's
`translations/en.json`. Keep keys unchanged and translate their values.
Do not edit managed plugin catalogs: updates replace those files. The installer
leaves the separate configuration overrides alone.

Resolution starts with embedded English, then loads `en`, the neutral language
and the selected regional language in that order. At each level the packaged
file loads before the user's override. Thus `pt-BR` can inherit missing entries
from `pt`. An invalid entry retains the previous valid fallback and logs a
diagnostic. Unknown keys are ignored.

Preserve numbered placeholders such as `{0}` and `{1:F1}`; they may be reordered
to suit grammar. Preserve native tokens such as `[us]`, `[them]`, `[crafts]` and
`[checks]`, and keep command names, configuration keys and identifiers unchanged.
Use `{{` and `}}` for literal braces. JSON requires escaped newlines (`\n`) and
quotes (`\"`) inside a value. Files contain one flat object of string values.

## Authoring

Framework owns lookup, fallback, validation and language selection. Content mods
own their catalogs and embed their English JSON as an assembly resource. Register
with `Translations.Register(owner, assembly, resourceName)`, retain the returned
`TranslationCatalog`, and call `Get(key, arguments)` for complete messages.
Construction recipes can supply `nameKey` and `descriptionKey`; the existing
English `name` and `description` remain fallbacks. Register the owner's catalog
before registering its recipes.

Keep keys stable when revising wording. Prefer complete sentences with numeric
arguments and format specifications over concatenated translated fragments.
Never compare translated display text to decide readiness, routing or job state.
Native definition IDs, save keys, commands and config keys are stable contracts.
Brand names, technical diagnostics and bootstrap configuration descriptions may
remain English. This does not promise translation of every loader or game message.

The owner's equipment-branding memorandum requires full equipment names to start
with the literal `Phobos'` prefix and use original in-world brands and model
families where appropriate. Preserve that prefix, brand and model across
translations; translate the functional equipment type and description. Registered
name entries now contain only the type/variant: Framework adds the maker/model
from the content-owned embedded `equipment-names.json`. Leave numbered
placeholders intact. Existing full-name overrides for these keys need conversion;
obsolete prefixes fall back to the English descriptor. See the
[equipment naming guide](equipment-branding.md) for the shared API and examples.
Translation keys and saved identifiers remain stable.

Use named constants for shared unit conversions, gameplay limits and meaningful
tolerances. Derive displayed capacities and yields from their authoritative
rules. Keep content balance in its owning mod; avoid turning every coordinate or
implementation detail into a setting. Historic recipe revisions remain immutable.

## Evidence and verification

Local inspection of the installed game established `Localisation.Get()` and its
English default. The inspected assembly SHA-256 was
`91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`;
the working game baseline is Ostranauts 1.0.1.4 with BepInEx 5.4.23.5.
The native JSON localization types alone did not establish a complete custom C#
panel translation path, so these catalogs belong to Framework. No game assets or
third-party translation text are redistributed.

The Framework checks validate catalog syntax, referenced keys, placeholder
contracts, native grammar tokens, fallback order and recipe/overlay fallbacks.
Native integration checks ensure translated construction readiness is independent
of its displayed wording. Run the normal build scripts to include these checks.
Installer tests cover catalog delivery and preservation of configuration overrides.

In-game font coverage, long translated labels and non-English native grammar
remain unverified. Test those with an actual contributed language before claiming
support for it. This release adds the translation mechanism, not completed
translations into other languages.
