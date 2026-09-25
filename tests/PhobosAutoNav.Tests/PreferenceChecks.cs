using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAutoNav;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Persistence;

internal static class PreferenceChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var profile = new FlightPreferences(200, .2, .75);
        check(FlightPreferences.TryDecode(profile.Encode(), out var restored) && restored.ArrivalMS == .2 && restored.ArrivalKM == .75,
            "Console profile round-trips exact custom settings");
        foreach (var invalid in new[] { new FlightPreferences(9, 0, 1), new FlightPreferences(5001, 0, 1),
            new FlightPreferences(100, 101, 1), new FlightPreferences(100, 0, .099), new FlightPreferences(double.NaN, 0, 1) })
            check(!invalid.Valid, "Malformed or inconsistent profile is rejected");
        var futureFields = profile.Encode(); futureFields["future"] = "1";
        check(!FlightPreferences.TryDecode(futureFields, out _), "Unknown preference fields are preserved, not silently dropped");
        check(InstrumentRules.StepSpeed(.2, -1, true, 100) == .1 && InstrumentRules.StepSpeed(.1, -1, true, 100) == 0 &&
            InstrumentRules.StepSpeed(0, -1, true, 100) == 0, "Arrival controls reach zero without wrapping");
        check(InstrumentRules.StepSpeed(20, 1, true, 23) == 23 && InstrumentRules.StepSpeed(23, 1, true, 23) == 23,
            "Arrival controls honour a custom cruise ceiling");

        AutoNavCore.ResetStatics(); CrewSim.objInstance = new CrewSim { FinishedLoading = true };
        Plugin.DefaultCruiseMS.Value = 100; Plugin.DefaultArriveSpeedMS.Value = 0; Plugin.DefaultArriveKM.Value = .75f;
        var ship = new Ship { strRegID = "ship" }; CrewSim.coPlayer = new CondOwner { strID = "player", ship = ship };
        CondOwner Console(string id)
        {
            var co = new CondOwner { strID = id, ship = ship };
            co.Items.Add(new CondOwner { strID = id + "-module", Kind = NavigationService.ModuleId, ship = ship });
            return co;
        }
        var one = Console("one"); var two = Console("two"); var service = new NavigationService();
        service.BindForTest(one); GUIOrbitDraw.CrossHairTarget = null;
        check(service.ReadInstruments(one).CruiseMS == 100 && one.mapGUIPropMaps.Count == 0, "Unconfigured reads inherit seeds without creating save state");
        check(service.SetFlightSetting(one, FlightSetting.Cruise, 200) && service.SetFlightSetting(one, FlightSetting.ArrivalSpeed, 20), "Service writes bounded defaults");
        check(service.ReadInstruments(one).CruiseMS == 200 && service.ReadInstruments(two).CruiseMS == 100 && Plugin.DefaultCruiseMS.Value == 100,
            "One console cannot change another or shared configuration");
        check(service.SetFlightSetting(one, FlightSetting.Cruise, 10) && service.ReadInstruments(one).ArrivalMS == 10,
            "Lowering cruise clamps its own arrival speed coherently");
        check(!service.SetFlightSetting(one, FlightSetting.ArrivalSpeed, 11), "Explicit arrival above cruise is rejected");
        check(!service.SetFlightSetting(one, FlightSetting.Cruise, double.NaN), "Invalid F3/service input cannot enter saved defaults");
        var flight = new FlightSnapshot { ConsoleId = "one", ModuleId = "one-module", ShipId = "ship", PlayerId = "player", TargetId = "target",
            CruiseMS = 80, ArrivalMS = .2, ArrivalKM = .5, Coast = new CoastSettings(3,10,.75,2), Mode = SavedFlightMode.Suspended };
        var flightStore = new ObjectStateStore(one.mapGUIPropMaps, FlightSnapshot.StoreName, "one", 1);
        flightStore.TryWrite(flight.Encode()); flightStore.Read(out var before);
        check(!service.SetFlightSetting(one, FlightSetting.Cruise, 300) && service.ReadInstruments(one).CruiseMS == 80,
            "Suspended flight retains captured settings instead of defaults");
        flightStore.Read(out var after); check(before.SequenceEqual(after), "Rejected settings do not rewrite flight intent");
        flightStore.Clear();
        var protectedMap = new Dictionary<string,string> { ["schema"] = "99", ["owner"] = "one", ["data.future"] = "keep" };
        one.mapGUIPropMaps["PhobosState." + FlightPreferences.StoreName] = protectedMap;
        check(!service.SetFlightSetting(one, FlightSetting.Cruise, 100) && !service.ReadInstruments(one).CanAdjustArrival &&
            ReferenceEquals(protectedMap, one.mapGUIPropMaps["PhobosState." + FlightPreferences.StoreName]), "Unknown preference schema remains protected");
        protectedMap["schema"] = "1";
        check(!service.SetFlightSetting(one, FlightSetting.Cruise, 100) && protectedMap.ContainsKey("data.future"), "Malformed known schema remains protected");
        one.ship = new Ship { strRegID = "foreign" };
        check(!service.SetFlightSetting(one, FlightSetting.Cruise, 300), "Foreign/docked console cannot be configured");
        two.Items.Clear(); check(!service.SetFlightSetting(two, FlightSetting.Cruise, 200), "Missing module blocks service-side settings");
    }
}
