using System;
using System.Linq;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class IndustryCommands
{
    internal static bool Handle(string input, out bool success, out string response)
    {
        success = false; response = "";
        var words = input.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || !words[0].Equals("phobosindustry", StringComparison.OrdinalIgnoreCase)) return false;
        response = Text.Get("Industry.help");
        if (words.Length < 2 || words[1] == "help") { success = true; return true; }
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading)
        { response = Text.Get("Industry.world_unavailable"); return true; }
        if (words[1] == "consoles")
        {
            var ship = CrewSim.GetSelectedCrew()?.ship;
            response = ship == null ? Text.Get("Industry.none") : string.Join("\n", ship.GetCOs(null, false, false, true)
                .Where(c => c != null && !c.bDestroyed && c.ship == ship && c.objCOParent == null && IndustrialRules.Console(c.strCODef) && c.HasCond("IsInstalled"))
                .Select(c => c.strNameFriendly + " — " + c.strID));
            success = true; return true;
        }
        if (words.Length < 3) return true;
        var console = CollectorService.Resolve(words[2]);
        if (console == null || !IndustrialRules.Console(console.strCODef)) { response = Text.Get("Industry.missing"); return true; }
        if (!ControlAuthority.TryBind(console, out var binding, out response)) return true;
        switch (words[1])
        {
            case "controls": success = IndustrialPanel.Open(console); response = Text.Get(success ? "Industry.connected" : "Industry.open_blocked"); break;
            case "status":
                response = string.Join("\n\n", IndustryService.SnapshotShip(console.ship).Select(card => card.Name + "\n" + card.Id + "\n" + card.Detail));
                success = true; break;
            case "observations":
                success = IndustryObservations.TryRead(binding!, out var observations, out response);
                if (success) response = observations.Length == 0 ? Text.Get("Observations.none") :
                    string.Join("\n\n", observations.Select(card => card.Detail));
                break;
            case "pause-all": response = IndustryService.PauseAll(binding!); success = true; break;
            default:
                if (words.Length >= 4) success = IndustryService.Run(binding, words[3], words[1], words.Length > 4 ? words[4] : null, out response);
                else response = Text.Get("Industry.help");
                break;
        }
        return true;
    }
}
