using System.Collections.Generic;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    // Local to ONE synchronous presentation read. Never stored on the service,
    // reused across updates, or accepted by command/guidance entry points.
    internal sealed class PresentationRead
    {
        internal readonly CondOwner Console;
        internal readonly FlightSnapshot? Stored, Flight;
        internal readonly bool ValidRecord, ValidPreferences, Navigation, Pursuit;
        internal readonly FlightPreferences Preferences;
        internal readonly CondOwner? FireModule;
        internal readonly TargetRef? Target;
        private readonly Dictionary<string, ContactReading> contacts = new();
        private double? fuel;
        private bool hardwareRead;
        private string? hardware;
        internal PresentationRead(CondOwner co, bool ownsFlight)
        {
            Console = co;
            var state = Store(co).Read(out var fields);
            FlightSnapshot? decoded = null;
            ValidRecord = state == SavedStateStatus.Missing || state == SavedStateStatus.Ready &&
                FlightSnapshot.TryDecode(fields, out decoded) && decoded.ConsoleId == co.strID;
            Stored = ValidRecord ? decoded : null;
            Flight = Stored?.IsResumable == true ? Stored : null;
            ValidPreferences = ReadPreferences(co, out Preferences);
            foreach (var item in co.GetCOsSafe(true))
            {
                if (item.HasCond("IsDamaged")) continue;
                if (HasId(item, ModuleId) || HasId(item, PursuitId)) Navigation = true;
                if (HasId(item, PursuitId)) Pursuit = true;
                if (FireModule == null && HasId(item, FireControlId)) FireModule = item;
            }
            Target = ownsFlight ? AutoNavCore.EngagedTarget : Flight != null ? TargetRef.FromShipId(Flight.TargetId) :
                GUIOrbitDraw.IsOpen() && GUIOrbitDraw.CrossHairTarget?.Ship != null && GUIOrbitDraw.CrossHairTarget.Ship != co.ship
                    ? TargetRef.FromShipId(GUIOrbitDraw.CrossHairTarget.Ship.strRegID) :
                GUIOrbitDraw.IsOpen() && GUIOrbitDraw.CrossHairTarget?.Ship == null && GUIOrbitDraw.CrossHairTarget?.stellarObj != null
                    ? TargetRef.FromShipId(GUIOrbitDraw.CrossHairTarget.stellarObj.strID) : null;
        }
        internal ContactReading Contact(TargetRef? target)
        {
            string key = target?.ShipId ?? "";
            if (!contacts.TryGetValue(key, out var value)) contacts[key] = value = ReadContact(Console, target);
            return value;
        }
        internal double Fuel => fuel ??= Console.ship.GetRCSRemain();
        internal string? Hardware
        {
            get { if (!hardwareRead) { hardware = HardwareProblem(Console, this); hardwareRead = true; } return hardware; }
        }
    }
}
