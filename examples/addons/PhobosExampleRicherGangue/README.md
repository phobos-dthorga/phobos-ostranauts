# Example add-on: Richer Gangue

A small, real add-on for Phobos Manufacturing, kept here as the worked example for
[publishing an add-on](../../../docs/publishing-an-add-on.md). The repository's
checks load it over the shipped packs, so the format it shows is the format that works.

What it does:

- Lowers the "tailings only" result of the gangue wash from 50 to 30.
- Adds one outcome of its own, a steel seam (4 scrap steel), at a weight of 10.

To try it, copy this folder into the game's `Ostranauts_Data/Mods` folder, enable it
in the MODS screen after the Phobos mods, restart, and type `phobosframework addons`
in the F3 console.
