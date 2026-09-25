# Manufacturing content registration

Scaffold 0.0.1 registers no construction or processing recipes. The native
`data/conditions` empty array preserves the data-directory convention required
by the game's mod loader.

The first-slice proposal is in `docs/manufacturing-research.md` in the repository
and `manufacturing-research.md` in the prepared package. Manufacturing will own
its machine, cold stock, tooling/service cartridge, finished sink and spent
cartridge identities. No proposed identity is registered or save-stable yet.

Add the equipment-name map and construction pack here with the actual content;
keep machining in the dedicated machine service. No OCF recipe directory or
legacy aliases are needed for this new mod.
