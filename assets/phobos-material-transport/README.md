# Underfloor transport port concepts

Original Phobos concept artwork generated with ChatGPT's built-in Imagegen tool
on 24 September 2026. The owner requested graphics work while gameplay testing
continues. These explore the underfloor transport proposal, with a provisional
2 x 2 tile body for each parts terminal, corresponding to a 32 x 32 world texture.
They do not establish storage capacity, transport speed, power demand or the
complete installation/access envelope. Full wall-panel intakes need separate sizing.

## Design

The sending port has a short captive belt entering a dark mouth beneath a cream
service hood. Grey guide rails, blue framing and restrained ochre accents relate
it to the existing Shipbreaker. The receiving companion keeps that housing and
exposes a collection pocket with retaining stops and an outward direction marker.
The hoods imply enclosed underfloor service channels; no playable basement is
depicted. Electrical sockets remain unconnected, without painted conduit loops.

These concepts preserve the existing Shipbreaker graphics. Their large generated
masters are not native-resolution sprites; nearest-neighbour previews show the
actual proposed pixel scale. Source alpha is retained for review. Final opacity,
normal maps, damage/loose states and any moving belt frames follow a settled design.
No transport behaviour or in-game item definition is included in this art stage.

Both generated masters and their 32-pixel previews were visually reviewed. The
hood, dark mouth and guided bed remain identifiable beside the existing machine;
the receiver's wider pocket and ochre stops provide the clearest difference.
The tiny direction triangles do not remain reliably legible at this scale, so
production should use larger direction markings or clear runtime indicators.
Fine ribs and hardware simplify strongly, as intended. The comparison is an
asset-scale preview, not evidence of installed appearance or exact gameplay size.

## Files and reproduction

- [Sending master](source/PhobosTransportSending-concept-v1.png)
- [Receiving master](source/PhobosTransportReceiving-concept-v1.png)
- [Same-scale comparison with Shipbreaker](previews/ports-v1-scale-comparison.png)
- [Source filenames and SHA-256 hashes](sources.json)
- [Exact prompts and reference roles](prompts.md)

Run `scripts/export-transport-concepts.ps1` from the repository root. It verifies
the two master hashes, makes 32-pixel previews and 8x enlargements, and compares
them with the existing 64-pixel Shipbreaker at the same 8x zoom. It writes only
concept previews and never changes installed graphics or mod packages.

The original project artwork and mechanical derivatives are within the repository's
MIT scope. No exclusive rights in generated imagery or rights over Ostranauts art
are claimed. The masters and prompts remain available for further revisions.

Related: [transport design discussion](../../docs/underfloor-material-transport.md).
