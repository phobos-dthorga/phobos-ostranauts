using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

internal static class InstallMenuChecks
{
    internal static void Run(NativeDefinitions farm, NativeDefinitions industry, Action<bool,string> check, Action<Action,string> throws)
    {
        // Exercise the actual native registration, not a replica of the menu algorithm.
        var foreign = new JsonInstallable { strName = "ForeignInstall", strJobType = "install",
            strStartInstall = "ForeignFixture", strBuildType = "APPS" };
        Installables.dictJobBuildOptionsListed["APPS"]["ForeignFixture"] = foreign;
        farm.Publish();
        foreach (var job in farm.Installables.Values) Installables.Create(job);
        foreach (var definitions in new[] { farm, industry })
        {
            InstallMenu.Validate(definitions.Installables.Values);
            foreach (var co in definitions.Objects.Values.Where(c => c.aStartingConds?.Any(s => s.Split('=')[0] == "IsInstalled") == true))
            {
                string expected = co.strName.StartsWith(FurnaceRules.Radiator) || co.strName.StartsWith(FurnaceRules.ThermalPort) || co.strName.StartsWith(FurnaceCooling.Conduit) ? "HVAC" :
                    co.strName.StartsWith(IndustrialRules.Prefix) ? "CTRL" :
                    co.strName.StartsWith(PhobosAgriculture.IrrigationDefinitions.Pipe) ? "MISC" : "APPS";
                check(Installables.dictJobBuildOptionsListed.TryGetValue(expected, out var tab) && tab.ContainsKey(co.strName), "Native INSTALL tab covers intact/damaged fixture: " + co.strName);
                var job = Installables.dictJobBuildOptionsListed[expected][co.strName];
                check(job.aInputs?.Length > 0 && job.aLootCOs.Contains(co.strName), "Placement retains physical input and correct output: " + co.strName);
                check(Installables.dictJobBuildOptionsListed.Values.Sum(t => t.ContainsKey(co.strName) ? 1 : 0) == 1, "Exactly one menu entry per fixture: " + co.strName);
                check(DataHandler.dictCOs.ContainsKey(job.strActionCO) && DataHandler.dictItemDefs.ContainsKey(co.strItemDef), "Menu source and placement sprite resolve");
            }
        }
        check(ReferenceEquals(foreign, Installables.dictJobBuildOptionsListed["APPS"]["ForeignFixture"]), "Other providers' menu entries survive validation");
        foreach (string invalid in new[] { "MIS", "", "CUSTOM" })
            throws(() => InstallMenu.Validate(new[] { new JsonInstallable { strName = "Bad", strJobType = "install", strBuildType = invalid, strStartInstall = "Fixture" } }), "Unreachable native category rejected: " + invalid);
        throws(() => InstallMenu.Validate(new[] { new JsonInstallable { strName = "Bad", strJobType = "install", strBuildType = "APPS" } }), "Missing placement target rejected");
        check(!PhobosAutoNav.EquipmentContent.Prepare().Installables.Values.Any(j => j.strJobType == "install"), "Polaris module boards retain slot insertion rather than floor installation");
    }
}
