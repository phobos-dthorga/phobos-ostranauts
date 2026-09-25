using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Native PDA INSTALL tabs. Work-rate categories remain independent.</summary>
public static class InstallMenu
{
    public const string Hull = "HULL", Hvac = "HVAC", Power = "POWR", Sensors = "SENS",
        Controls = "CTRL", Furniture = "FURN", Appliances = "APPS", Miscellaneous = "MISC";

    public static void Validate(IEnumerable<JsonInstallable> definitions)
    {
        foreach (var job in definitions)
        {
            if (job.strJobType != "install" || job.bHeadless || job.bNoJobMenu) continue;
            switch (job.strBuildType)
            {
                case Hull: case Hvac: case Power: case Sensors: case Controls:
                case Furniture: case Appliances: case Miscellaneous: break;
                default: throw new InvalidOperationException("Unreachable INSTALL category for " + job.strName + ": " + job.strBuildType);
            }
            if (string.IsNullOrEmpty(job.strStartInstall))
                throw new InvalidOperationException("Missing INSTALL placement target for " + job.strName);
        }
    }
}
