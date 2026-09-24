using System;
using System.Linq;
using PhobosAutoNav;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Persistence;

internal static class InstrumentChecks
{
    internal static void Run(Action<bool, string> check)
    {
        foreach (double start in new[] { .1, .125, .25, .3, .5, .75, 1, 2, 5, 10, 25, 50, 99, 100 })
        {
            check(InstrumentRules.StepArrival(start, 1) >= start, "Arrival dial increments custom distances without rounding down");
            check(InstrumentRules.StepArrival(start, -1) <= start, "Arrival dial decrements custom distances without rounding up");
            check(ApproachRules.ValidArrival(InstrumentRules.StepArrival(start, 1)), "Dial preserves arrival limits");
        }
        check(InstrumentRules.StepArrival(100, 1) == 100 && InstrumentRules.StepArrival(.1, -1) == .1,
            "No endpoint wrap can unexpectedly request a close approach");
        check(InstrumentRules.StepArrival(.75, 0) == .75, "Zero wheel input retains exact setting");
        check(InstrumentRules.ArrivalAngle(.1) == 120 && InstrumentRules.ArrivalAngle(100) == -120, "Pointer covers full legal range");

        AutoNavCore.ResetStatics(); TargetRef.Available = true; Plugin.Enabled.Value = true;
        Plugin.DefaultArriveKM.Value = .75f; Plugin.PreferTorch.Value = true;
        var ship = new Ship { strRegID = "ship" };
        CrewSim.coPlayer = new CondOwner { ship = ship };
        CrewSim.objInstance = new CrewSim { FinishedLoading = true };
        var co = new CondOwner { strID = "console", ship = ship };
        var service = new NavigationService(); service.BindForTest(co);
        GUIOrbitDraw.CrossHairTarget = new GUIOrbitDraw.Contact { Ship = new Ship { strRegID = "target" } };
        int saves = Plugin.DefaultArriveKM.ConfigFile.Saves;
        var view = service.ReadInstruments(co);
        check(view.ArrivalKM == .75 && view.CanAdjustArrival && view.CanFly, "Idle instrument keeps custom arrival value and ready target");
        check(co.mapGUIPropMaps.Count == 0 && Plugin.DefaultArriveKM.ConfigFile.Saves == saves, "Reading a panel creates no save or config changes");
        service.StepPanelArrival(co, 1);
        check(Plugin.DefaultArriveKM.Value == 1 && Plugin.DefaultArriveKM.ConfigFile.Saves == saves + 1, "Dial saves the next legal default");
        var snapshot = new FlightSnapshot { ConsoleId = co.strID, ModuleId = "module", ShipId = "ship", PlayerId = "player",
            TargetId = "saved-target", CruiseMS = 100, ArrivalKM = .75, Coast = new CoastSettings(3, 10, .75, 2), Mode = SavedFlightMode.Suspended };
        var store = new ObjectStateStore(co.mapGUIPropMaps, FlightSnapshot.StoreName, co.strID, 1);
        store.TryWrite(snapshot.Encode()); store.Read(out var before);
        view = service.ReadInstruments(co);
        check(view.Resumable && !view.CanAdjustArrival && view.ArrivalKM == .75 && !view.CanAdjustPropulsion,
            "Suspended RCS flight presents its captured profile instead of defaults");
        check(view.Target == "saved-target", "Saved destination takes precedence over crosshair selection");
        service.StepPanelArrival(co, 1); service.SetPanelTorch(co, true);
        check(Plugin.DefaultArriveKM.Value == 1, "Suspended arrival is protected from dial input");
        store.Read(out var after); check(before.OrderBy(k => k.Key).SequenceEqual(after.OrderBy(k => k.Key)), "Panel interaction never rewrites saved intent");
        AutoNavCore.Engaged = true; AutoNavCore.EngagedPlayer = ship; AutoNavCore.FlightPrefersTorch = true;
        AutoNavCore.CurrentPhase = AutoNavCore.Phase.Coast;
        service.SetPanelTorch(co, false);
        check(service.Torch.Cuts == 1 && AutoNavCore.Engaged && !Plugin.PreferTorch.Value, "RCS selector cuts torch without cancelling active guidance");
        view = service.ReadInstruments(co);
        check(view.Heading == "Instruments.phase.Coast" && view.Notice == "Instruments.coast_hint", "Coasting reports idle translation, not a burning engine");
        var other = new CondOwner { strID = "other", ship = ship };
        service.SetPanelTorch(other, true); service.StepPanelArrival(other, 1);
        view = service.ReadInstruments(other);
        check(!Plugin.PreferTorch.Value && !view.CanFly && !view.CanStop && !view.CanAdjustArrival && !view.CanAdjustPropulsion,
            "Another panel cannot commandeer a running flight");
        service.SetPanelTorch(co, true); check(Plugin.PreferTorch.Value, "Torch-authorized flight can restore AUTO after RCS inhibition");
        AutoNavCore.FlightPrefersTorch = false; service.SetPanelTorch(co, false); service.SetPanelTorch(co, true);
        check(!Plugin.PreferTorch.Value, "RCS-only flight cannot acquire unsaved torch authority from a dial");
        AutoNavCore.ResetStatics(); store.Clear();
        co.ship = new Ship { strRegID = "docked-neighbour" };
        service.SetPanelTorch(co, true); service.StepPanelArrival(co, 1);
        check(!Plugin.PreferTorch.Value && !service.ReadInstruments(co).CanFly, "Docked neighbour is outside local instrument authority");
        co.ship = ship; co.mapGUIPropMaps["PhobosState.AutoNav.Flight"] = new() { ["schema"] = "99" };
        view = service.ReadInstruments(co); check(!view.CanFly && view.Warning, "Unknown saved state blocks Fly and remains intact");
        check(co.mapGUIPropMaps["PhobosState.AutoNav.Flight"]["schema"] == "99", "Diagnostic reads preserve future records");
        GUIOrbitDraw.CrossHairTarget = null; AutoNavCore.ResetStatics();
    }
}
