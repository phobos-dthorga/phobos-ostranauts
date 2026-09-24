using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Observations;
using PhobosShipbreaker;
using UnityEngine;

int checks = 0;
void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
CondOwner Item(Ship ship, string id, string definition, params string[] conditions)
{
    var item = new CondOwner { ship = ship, strID = id, strCODef = definition, strNameFriendly = id };
    item.Conditions.UnionWith(conditions); ship.Objects.Add(item); CollectorService.Objects[id] = item; return item;
}
Ship Vessel(string id)
{
    var ship = new Ship { strRegID = id }; ship.Room.CO = new CondOwner { strID = id + "-room", ship = ship, GasContainer = new() }; return ship;
}
var ship = Vessel("home"); var foreign = Vessel("docked-station");
var console = Item(ship, "console", "ConsoleInstalled", "IsInstalled", "IsPowered");
CrewSim.Actor = Item(ship, "crew", "Crew");
var alarm = Item(ship, "alarm", "NativeAlarm", "IsInstalled", "IsPowered", "IsAlarmO2", "IsGreen");
var machine = Item(ship, "machine", "Reclaimer", "IsInstalled", "IsPowered");
ship.Room.CO.Values["StatGasTemp"] = 300; ship.Room.CO.Values["StatGasPressure"] = 90;
var otherAlarm = Item(foreign, "foreign-alarm", "NativeAlarm", "IsInstalled", "IsPowered", "IsAlarmSmoke", "IsRed");

Check(NativeRoomAlarms.Read(alarm).Validity == ObservationValidity.Unavailable, "A saved green lamp is unknown before a native evaluation");
NativeRoomAlarms.Evaluated(alarm, "RoomA");
Check(NativeRoomAlarms.Read(alarm).Validity == ObservationValidity.Unavailable, "Same-frame queued output is not prematurely trusted");
Time.frameCount++;
Check(NativeRoomAlarms.Read(alarm).Alarm == AlarmState.Clear, "Actual native evaluation qualifies an output");
StarSystem.fEpoch += 6;
Check(NativeRoomAlarms.Read(alarm).Validity == ObservationValidity.Stale, "No evaluation makes even a green lamp stale");
NativeRoomAlarms.Evaluated(alarm, "RoomA"); Time.frameCount++;
alarm.Conditions.Remove("IsGreen"); alarm.Conditions.Add("IsRed");
Check(NativeRoomAlarms.Read(alarm).Alarm == AlarmState.Alert, "Queued lamp transition is read after its evaluation frame");
alarm.Conditions.Remove("IsPowered");
var off = NativeRoomAlarms.Read(alarm);
Check(off.Validity == ObservationValidity.Unavailable && off.Alarm == AlarmState.Alert, "Power loss retains only historical output");
alarm.Conditions.Add("IsPowered");
Check(NativeRoomAlarms.Read(alarm).Validity == ObservationValidity.Unavailable, "Restored power must earn a fresh sample");
NativeRoomAlarms.Evaluated(alarm, "RoomA"); Time.frameCount++;
alarm.Conditions.Add("IsDamaged");
Check(NativeRoomAlarms.Read(alarm).Validity == ObservationValidity.Faulty, "Damage is a fault, never a zero or clear reading");
alarm.Conditions.Remove("IsDamaged");
NativeRoomAlarms.Evaluated(alarm, "RoomA"); Time.frameCount++;
ship.Neighbours.Add(otherAlarm);
Check(NativeRoomAlarms.Read(alarm).Reason == "room_scope", "Dock-inclusive sample geometry cannot masquerade as an own-room reading");
ship.Neighbours.Clear(); ship.Room.CO.strID = "rebuilt-room";
Check(NativeRoomAlarms.Read(alarm).ObservedAt == null, "Changing compartment identity invalidates evidence");
NativeRoomAlarms.Evaluated(alarm, "RoomA"); Time.frameCount++;
StarSystem.fEpoch -= 50;
Check(NativeRoomAlarms.Read(alarm).ObservedAt == null, "A reversed simulation clock invalidates evidence");
NativeRoomAlarms.Evaluated(alarm, "RoomA"); Time.frameCount++;
NativeRoomAlarms.Reset();
Check(NativeRoomAlarms.Read(alarm).ObservedAt == null, "Reload discards current authority without editing native lamp state");
for (int i = 0; i < 4; i++)
{
    StarSystem.fEpoch++; Time.frameCount++;
    NativeRoomAlarms.Evaluated(alarm, "RoomA");
}
Check(NativeRoomAlarms.Read(alarm).Validity == ObservationValidity.Current, "Continuous native evaluations with the panel closed do not starve output publication");
alarm.Conditions.Remove("IsInstalled");
Check(NativeRoomAlarms.Read(alarm).Validity == ObservationValidity.Unavailable, "Uninstalled sensor is not live");
alarm.Conditions.Add("IsInstalled");
Check(NativeRoomAlarms.Read(alarm).Validity == ObservationValidity.Unavailable, "Reinstalling must earn another native update");

Check(ControlAuthority.TryBind(console, out var binding, out _), "Own console permits observation access");
Check(IndustryObservations.TryRead(binding!, out var cards, out _) && cards.Length == 3, "One native alarm and two built-in probes share a console service");
Check(cards.All(c => !c.Id.Contains("foreign")), "Docked sensors do not enlarge the console roster");
int scans = ship.DiscoveryCalls;
IndustryObservations.TryRead(binding!, out _, out _);
Check(ship.DiscoveryCalls == scans, "Repeated reads reuse the bounded ship roster");
machine.ship = foreign;
IndustryObservations.TryRead(binding!, out cards, out _);
Check(cards.All(c => !c.Id.StartsWith("machine/")), "Moved machinery is removed immediately even during the discovery interval");
machine.ship = ship;
console.Conditions.Remove("IsPowered"); scans = ship.DiscoveryCalls;
Check(!IndustryObservations.TryRead(binding!, out cards, out _) && cards.Length == 0 && scans == ship.DiscoveryCalls, "Powerless console cannot read or refresh instruments");
console.Conditions.Add("IsPowered");
CrewSim.system.Owner = "buyer";
Check(!IndustryObservations.TryRead(binding!, out _, out _), "Selling the host ship revokes reads immediately");
CrewSim.system.Owner = "player";
TileUtils.Distance = 20;
Check(!IndustryObservations.TryRead(binding!, out _, out _), "Walking away revokes reads"); TileUtils.Distance = 0;
CrewSim.Actor = Item(ship, "other-crew", "Crew");
Check(!IndustryObservations.TryRead(binding!, out _, out _), "Changing operator invalidates captured observation authority");
CrewSim.Actor = CollectorService.Resolve("crew")!;
Check(!IndustryObservations.TryRead(binding!, out _, out _), "Switching back does not revive old console authority");

var values = IndustryObservations.ReadProbes(machine);
Check(values.All(o => o.Validity == ObservationValidity.Current) && Math.Abs(values[0].Value!.Value - 26.85) < .001, "Built-in probes report actual room temperature and pressure with explicit units");
IndustryObservations.RecordStop(machine, "cooling-interlock");
string captured = IndustryObservations.ExplainStop(machine);
ship.Room.CO.Values["StatGasTemp"] = 400;
IndustryObservations.ReadProbes(machine);
Check(IndustryObservations.ExplainStop(machine) == captured, "Changing live readings cannot rewrite evidence captured at a stop");
machine.Conditions.Remove("IsPowered");
Check(IndustryObservations.ReadProbes(machine).All(o => o.Validity == ObservationValidity.Unavailable && o.Value.HasValue), "An unpowered probe offers only its last-known reading");
Check(ship.Room.CO.GasContainer!.StoredHeat == 500 && ship.Room.CO.GetCondAmount("StatGasTemp") == 400, "Instrument loss never erases heat or alters native room state");
ship.Room.CO.strID = "another-room";
Check(IndustryObservations.ReadProbes(machine).All(o => o.Value == null), "A historical probe value cannot move to a new compartment");
IndustryObservations.Reset();
Check(IndustryObservations.ExplainStop(machine) == "" && IndustryObservations.ReadProbes(machine).All(o => o.ObservedAt == null), "Reload clears session diagnostics and historical readings");
Console.WriteLine($"PASS: {checks} production observation-adapter/access/evidence checks against world doubles; not an in-game test.");
