using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosAgriculture.Core;

namespace PhobosAgriculture;

/// <summary>Misting a rack's crop in a room too hot for it (Agriculture 0.59.0; owner request, 6 October 2026). A per-rack
/// switch, off until the player turns it on. While on, a planted, sound, powered rack in a room with air mists its crop
/// from its own reservoir whenever the room is above the crop's ceiling (<see cref="Misting.Plan"/>). The water leaves as
/// vapour: the room's air loses its latent heat, and the water goes to a linked water tank (<see cref="VapourReturn"/>)
/// or is lost, as transpired vapour a full rack cannot condense already is. It never helps with cold or pressure.</summary>
internal static partial class Service
{
    private static ObjectStateStore MistingStore(CondOwner co) => new(co.mapGUIPropMaps, "AgricultureMisting", Plugin.Id, 1);
    internal static bool IsRack(CondOwner co) => Definitions.Machine(co) && !Definitions.IsCooker(co) && !IrrigationDefinitions.IsSupply(co) && !WorkupDefinitions.IsBench(co);

    /// <summary>A missing record means off; an unreadable one protects the machine, as the water mode does.</summary>
    private static void ReadMisting(Session s)
    {
        var status = MistingStore(s.Object).Read(out var fields);
        if (status == SavedStateStatus.Missing) return;
        if (status != SavedStateStatus.Ready || fields.Count != 1 || !fields.TryGetValue("mode", out var mode) || (mode != "on" && mode != "off"))
        { s.Protected = true; return; }
        s.Misting = mode == "on";
    }
    private static void SetMisting(Session s, bool on)
    {
        if (!MistingStore(s.Object).TryWrite(new Dictionary<string, string> { ["mode"] = on ? "on" : "off" }))
            throw new InvalidOperationException("Protected misting setting.");
        s.Misting = on; if (!on) { s.MistLast = MistPlan.None; s.MistExcess = 0; s.MistKgPerHour = 0; }
    }

    /// <summary>One step's misting, taken from the reservoir before the crop grows. <paramref name="excessC"/> is how far
    /// the room is above the crop's ceiling; <paramref name="applicable"/> is false when the rack is unsound or the room's
    /// pressure is wrong, which misting cannot help.</summary>
    private static MistPlan MistStep(Session s, double excessC, double hours, double received, bool applicable)
    {
        s.MistExcess = 0; s.MistKgPerHour = 0; s.MistLast = MistPlan.None;
        if (!s.Misting || s.State.CropId.Length == 0 || !applicable || received <= 0 || !(excessC > 0) || !(hours > 0)) return MistPlan.None;
        var plan = Misting.Plan(excessC, Growth.Mist(s.State.CropId), hours, s.State.Water);
        s.State.Water = Math.Max(0, s.State.Water - plan.WaterKg);
        s.MistExcess = excessC; s.MistKgPerHour = plan.WaterKg / hours; s.MistLast = plan;
        return plan;
    }

    /// <summary>The misting line on a rack's status.</summary>
    private static string MistLine(Session s)
    {
        if (!s.Misting) return Text.Get("mist_off");
        if (s.MistExcess <= 0) return Text.Get("mist_idle");
        if (s.MistLast.WaterKg <= 0) return Text.Get("mist_dry", Growth.Mist(s.State.CropId).ReserveKg);
        return s.MistLast.Covered ? Text.Get("mist_holding", s.MistLast.CoolingC, s.MistKgPerHour) : Text.Get("mist_beyond", s.MistKgPerHour);
    }

    /// <summary>The Next line for a room that is too hot and nothing else (Agriculture 0.59.0), or null when misting
    /// covers it.</summary>
    private static string? HotRoomNeed(Session s, string? cropId, double tempC, double maxC)
    {
        var rules = Growth.Mist(cropId);
        if (rules.MaxCoolingC <= 0) return null;
        if (!s.Misting) return Text.Get("advice_room_mist", tempC, maxC, rules.MaxCoolingC);
        if (s.State.Water <= rules.ReserveKg + 1e-9) return Text.Get("advice_mist_water", rules.ReserveKg);
        if (tempC - maxC > rules.MaxCoolingC + 1e-9) return Text.Get("advice_mist_beyond", tempC, maxC, rules.MaxCoolingC);
        return "";
    }
}
