using System;
using System.Linq;
using HarmonyLib;
using PhobosShipbreaker.Core;
using UnityEngine;

namespace PhobosShipbreaker;

// Presentation only. All inventory, work and routing changes use existing services.
internal sealed class ReclaimerPanel
{
    internal void Reset() { }
    internal bool Show(CondOwner machine)=>ReclaimerRules.IsFamily(machine.strCODef)&&ProcessingService.AccessProblem(machine)==null&&IndustrialPanel.Open(machine);
    internal void Draw() { }
    internal static bool Command(string input, out bool result, out string response)
    {
        result = false; response = "";
        var words = input.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || !words[0].Equals("phobosreclaimer", StringComparison.OrdinalIgnoreCase)) return false;
        var action = words.Length > 1 ? words[1].ToLowerInvariant() : "help";
        if (action == "help" && words.Length <= 2) { result = true; response = Text.Get("Reclaimer.help"); return true; }
        if (words.Length > 3 || !new[] { "status", "start", "pause", "cancel", "feed", "products", "controls" }.Contains(action))
        { response = Text.Get("Reclaimer.help"); return true; }
        var machines = ProcessingService.FindMachines(true).Where(m => ReclaimerRules.IsFamily(m.strCODef) &&
            (words.Length < 3 || m.strID.Equals(words[2], StringComparison.OrdinalIgnoreCase))).ToArray();
        if (action == "status")
        {
            response = machines.Length == 0 ? Text.Get("Reclaimer.none") : string.Join("\n\n", machines.Select(m => m.strID + "\n" + Plugin.Service.Describe(m)));
            result = true; return true;
        }
        if (machines.Length != 1) { response = Text.Get("Reclaimer.select_one"); return true; }
        var machine = machines[0];
        switch (action)
        {
            case "start": result = Plugin.Service.Start(machine); break;
            case "pause": result = Plugin.Service.Pause(machine, false); break;
            case "cancel": result = Plugin.Service.Pause(machine, true); break;
            case "feed": result = Plugin.Service.OpenInventory(machine, true); break;
            case "products": result = Plugin.Service.OpenInventory(machine, false); break;
            case "controls": result = Plugin.ReclaimerControls.Show(machine); break;
        }
        response = Plugin.Service.Describe(machine); return true;
    }
}

[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class ReclaimerControlsPatch
{
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (!isCancelIa && __instance.strName == ReclaimerRules.Controls && __instance.objUs == CrewSim.GetSelectedCrew() && __instance.objThem != null)
            Plugin.ReclaimerControls.Show(__instance.objThem);
    }
}
