using System;
using System.Linq;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal sealed partial class ProcessingService
{
    internal static CondOwner[] FindMachines()
    {
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) return Array.Empty<CondOwner>();
        return CrewSim.GetSelectedCrew()?.ship?.GetCOs(null, bSubObjects: false, bAllowDocked: false, bAllowLocked: true)
            .Where(c => c != null && !c.bDestroyed && Content.IsMachine(c.strCODef))
            .OrderBy(c => c.strID, StringComparer.Ordinal).ToArray() ?? Array.Empty<CondOwner>();
    }

    internal bool RunCommand(Command command, out string response)
    {
        try
        {
            if (command.Action == CommandAction.Help) { response = Command.Help; return true; }
            if (command.Action == CommandAction.Dependencies)
            {
                response = "Phobos Shipbreaker " + Plugin.Version + ": " + Content.Status + "\n" + Content.DependencyStatus
                    + "\nThese are startup checks, not missing-dependency save recovery. Keep required content installed.";
                return true;
            }
            if (command.Action == CommandAction.Settings)
            {
                response = "Active settings (restart after config edits):\n"
                    + "CycleSeconds=" + options.CycleSeconds + "; ContinueQueue=" + options.ContinueQueue
                    + "\nWorkingKilowatts=" + options.WorkingKW + "; IdleKilowatts=" + options.IdleKW
                    + "; WindowKey=" + options.ControlsKey
                    + "\nBepInEx/config/" + Plugin.Id + ".cfg\nStarted panels retain their saved cycle duration.";
                return true;
            }
            if (command.Action == CommandAction.Invalid || command.Action == CommandAction.Foreign)
            { response = "Unknown command or extra arguments. Use phobosshipbreaker help."; return false; }
            var machines = FindMachines();
            if (command.TargetId != null)
                machines = machines.Where(m => string.Equals(m.strID, command.TargetId, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (command.Action == CommandAction.Status)
            {
                response = "Phobos Shipbreaker " + Plugin.Version + ": " + Content.Status + "\n"
                    + (machines.Length == 0 ? "No matching fixture on the selected crew member's loaded ship." :
                        string.Join("\n\n", machines.Select(m => m.strID + " - " + m.strNameFriendly + "\n" + Describe(m)
                            + "\nControl check: " + (AccessProblem(m) ?? MachineProblem(m) ?? "ready"))));
                return true;
            }
            if (machines.Length != 1)
            {
                response = machines.Length == 0 ? "No matching fixture. Select crew aboard its ship and use phobosshipbreaker status." :
                    "Multiple fixtures. Use phobosshipbreaker status, then supply the exact fixture ID.";
                return false;
            }
            var machine = machines[0];
            bool success;
            switch (command.Action)
            {
                case CommandAction.Start: success = Start(machine); break;
                case CommandAction.Pause: success = Pause(machine, false); break;
                case CommandAction.Cancel: success = Pause(machine, true); break;
                default: response = "Use phobosshipbreaker help."; return false;
            }
            response = machine.strID + "\n" + Describe(machine);
            return success;
        }
        catch (Exception ex)
        {
            log(ex.ToString());
            response = "Shipbreaker command failed: " + ex.GetType().Name + ". See the BepInEx log.";
            return false;
        }
    }
}
