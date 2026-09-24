using System;
using System.Linq;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal sealed partial class ProcessingService
{
    internal static CondOwner[] FindMachines(bool includeReclaimers = false)
    {
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) return Array.Empty<CondOwner>();
        return CrewSim.GetSelectedCrew()?.ship?.GetCOs(null, bSubObjects: false, bAllowDocked: false, bAllowLocked: true)
            .Where(c => c != null && !c.bDestroyed && (Content.IsMachine(c.strCODef) || includeReclaimers && ReclaimerRules.IsFamily(c.strCODef)))
            .OrderBy(c => c.strID, StringComparer.Ordinal).ToArray() ?? Array.Empty<CondOwner>();
    }

    internal bool RunCommand(Command command, out string response)
    {
        try
        {
            if (command.Action == CommandAction.Help) { response = Command.Help; return true; }
            if (command.Action == CommandAction.Dependencies)
            {
                response = Text.Get("ProcessingCommands.phobos_shipbreaker_these_are_startup_checks_not", Plugin.Version, Content.Status, Content.DependencyStatus);
                return true;
            }
            if (command.Action == CommandAction.Settings)
            {
                response = Text.Get("ProcessingCommands.active_settings_restart_after_config_edits_cycleseconds", options.CycleSeconds, options.ContinueQueue, options.WorkingKW, options.IdleKW, options.ControlsKey, options.TransferSeconds, options.CollectorSeconds, options.CollectorKW, options.CollectorContinue, Plugin.Id);
                return true;
            }
            if (command.Action == CommandAction.Invalid || command.Action == CommandAction.Foreign)
            { response = Text.Get("ProcessingCommands.unknown_command_or_extra_arguments_use_phobosshipbreaker"); return false; }
            var machines = FindMachines();
            if (command.TargetId != null)
                machines = machines.Where(m => string.Equals(m.strID, command.TargetId, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (command.Action == CommandAction.Status)
            {
                response = Text.Get("ProcessingCommands.phobos_shipbreaker", Plugin.Version, Content.Status, (machines.Length == 0 ? Text.Get("ProcessingCommands.no_matching_fixture_on_the_selected_crew") :
                        string.Join("\n\n", machines.Select(m => Text.Get("ProcessingCommands.control_check", m.strID, m.strNameFriendly, Describe(m), DescribeIntake(m), CollectorService.DescribeLink(m), CollectorService.LinkIds(m), (AccessProblem(m) ?? MachineProblem(m) ?? Text.Get("ProcessingCommands.ready")))))));
                return true;
            }
            if (machines.Length != 1)
            {
                response = machines.Length == 0 ? Text.Get("ProcessingCommands.no_matching_fixture_select_crew_aboard_its") :
                    Text.Get("ProcessingCommands.multiple_fixtures_use_phobosshipbreaker_status_then_supply");
                return false;
            }
            var machine = machines[0];
            bool success;
            switch (command.Action)
            {
                case CommandAction.Start: success = Start(machine); break;
                case CommandAction.Pause: success = Pause(machine, false); break;
                case CommandAction.Cancel: success = Pause(machine, true); break;
                case CommandAction.Feed: success = OpenInventory(machine, true); break;
                case CommandAction.Products: success = OpenInventory(machine, false); break;
                default: response = Text.Get("ProcessingCommands.use_phobosshipbreaker_help"); return false;
            }
            response = machine.strID + "\n" + Describe(machine) + "\n" + DescribeIntake(machine);
            return success;
        }
        catch (Exception ex)
        {
            log(ex.ToString());
            response = Text.Get("ProcessingCommands.shipbreaker_command_failed_see_the_bepinex_log", ex.GetType().Name);
            return false;
        }
    }
}
