using PhobosAutoNav.Core;

namespace PhobosAutoNav;

// 29 September 2026 pass (FF5): facts every controller re-read within one native physics step. The guard, the
// guidance tick, docking and combat validation each asked the console's hardware verdict, the flight binding and
// the RCS reserve again; each is now read once per step. A step is open only inside BeforeNavigationPhysics, so
// commands, panels and the test suites that call the controllers directly always read fresh.
internal sealed partial class NavigationService
{
    private static bool stepOpen;
    private static CondOwner? stepHardwareConsole;
    private static string? stepHardware;
    private static FlightSnapshot? stepBindingFlight;
    private static CondOwner? stepBindingConsole;
    private static bool stepBinding;
    private static Ship? stepRcsShip;
    private static double stepRcs;
    internal static bool StepOpen => stepOpen;

    private static void BeginStep()
    {
        stepOpen = true; ForgetStep();
        NativeContactReader.BeginStep(); TowFlight.BeginStep();
    }
    private static void EndStep()
    {
        stepOpen = false; ForgetStep();
        NativeContactReader.EndStep(); TowFlight.EndStep();
    }
    private static void ForgetStep() { stepHardwareConsole = null; stepHardware = null; stepBindingFlight = null; stepBindingConsole = null; stepRcsShip = null; }

    /// <summary>The console's hardware verdict, once per step.</summary>
    private string? HardwareProblemNow(CondOwner? co)
    {
        if (!stepOpen || co == null) return HardwareProblem(co);
        if (stepHardwareConsole != co) { stepHardware = HardwareProblem(co); stepHardwareConsole = co; }
        return stepHardware;
    }

    /// <summary>The saved flight's binding to this console, once per step for the same flight record.</summary>
    private bool FlightBindingValidNow()
    {
        if (!stepOpen || savedFlight == null) return FlightBindingValid();
        if (!ReferenceEquals(stepBindingFlight, savedFlight) || stepBindingConsole != console)
        { stepBinding = FlightBindingValid(); stepBindingFlight = savedFlight; stepBindingConsole = console; }
        return stepBinding;
    }

    /// <summary>The ship's remaining RCS reserve, once per step.</summary>
    private static double RcsRemainNow(Ship ship)
    {
        if (!stepOpen) return ship.GetRCSRemain();
        if (stepRcsShip != ship) { stepRcs = ship.GetRCSRemain(); stepRcsShip = ship; }
        return stepRcs;
    }
}
