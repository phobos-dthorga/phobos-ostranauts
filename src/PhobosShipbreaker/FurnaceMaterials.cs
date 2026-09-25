using System.Linq;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static partial class FurnaceService
{
    // No command, observation or inventory read advances the batch or grants heat permission.
    internal static string? MaterialProblem(CondOwner co, bool input)
    {
        if (!Content.Ready) return Content.Status;
        if (!ControlsReady(co)) return Text.Get("Furnace.install");
        if (CrewSim.coPlayer == null || CrewSim.system?.GetShipOwner(co.ship.strRegID) != CrewSim.coPlayer.strID)
            return Text.Get("Furnace.owned_ship");
        var s = Get(co); var b = s.State.Batch;
        if (!FurnaceMaterialRules.Accessible(b.Phase, b.SafeOpen, s.Protected, s.State.NativeMutation) || b.Armed)
            return Text.Get("Routing.furnace_cold");
        if (co.objContainer == null || co.objContainer.Locked) return Text.Get("Routing.source_locked");
        if (!input) return null;
        var bin = Feed(co);
        if (bin?.objContainer == null || bin.HasCond("IsLocked") || bin.objContainer.Locked || bin.objContainer.ContainedCOs.Any(i => !ValidFeed(i)))
            return Text.Get("Routing.furnace_bin");
        var endpoint = CoolingEndpoint(co);
        if (endpoint == null || endpoint.HasCond("IsDamaged") || Get(endpoint).Protected || !ProbeValid(co) || Flight(co.ship) || !ChargeReady(s))
            return Text.Get("Routing.furnace_interlock");
        if (Get(endpoint).SinkKJ >= FurnaceRules.SinkCapacity * (FurnaceRules.SinkMaxK - FurnaceRules.ReferenceK))
            return Text.Get("Routing.furnace_interlock");
        return null;
    }
}
