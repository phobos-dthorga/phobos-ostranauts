using System;
using System.Globalization;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Native work actions; balance, input bills and salvage yields belong to the caller.</summary>
public static class MaintenanceDefinitions
{
    public static void SetStat(JsonCondOwner item, string stat, double value) => item.aStartingConds =
        (item.aStartingConds ?? Array.Empty<string>()).Where(s => !s.StartsWith(stat + "=", StringComparison.Ordinal))
        .Concat(new[] { stat + "=1.0x" + value.ToString(CultureInfo.InvariantCulture) }).ToArray();

    public const string SpentParts = "PhobosMaintenanceSpentParts";
    public static void LegacyFinish(string savedDefinition, string oldAction, string newAction) =>
        MaintenanceSafety.LegacyFinishes[(savedDefinition, oldAction)] = newAction;
    public static void ReturnRepairMaterials(NativeDefinitions d, JsonInstallable repair)
    {
        if (!d.Objects.ContainsKey(SpentParts)) Remainder(d, SpentParts, "Spent service parts (0.5 kg)", .5);
        MaintenanceSafety.Repairs["MS" + repair.strName] = repair.aLootCOs[0];
        // Material quantity comes from the real repair lot, also for a job begun
        // under an older material bill. The machine itself keeps its saved ID.
        repair.aLootCOs = new[] { repair.aLootCOs[0] };
    }

    public static void Restore(NativeDefinitions d, string source, string skill = "MISC")
    {
        var job = Work(source + "Restore", source, "ACTUndamageTEMP", "TIsUndamageableNotContained", skill);
        job.strJobType = "repair";
        job.strAllowLootCTsThem = "CONDUndamageProgress";
        job.strProgressStat = "StatDamage";
        // Restore subtracts wear in place. Never add another destruction handler
        // to StatDamage or replace the machine/cargo when wear reaches zero.
        job.bNoDestructable = true;
        d.Installables.Add(job.strName, job);
    }

    public static void Dismantle(NativeDefinitions d, string source, double progress, string[] products, string skill = "MISC", string? emptyInternalBin = null)
    {
        SetStat(d.Objects[source], "StatDismantleProgressMax", progress);
        var job = Work(source + "Dismantle", source, "ACTDismantleTEMP", "TCanBeDismantled", skill);
        job.strJobType = "dismantle";
        job.strAllowLootCTsThem = "CONDDismantleProgress";
        job.strProgressStat = "StatDismantleProgress";
        job.aLootCOs = products;
        d.Installables.Add(job.strName, job);
        MaintenanceSafety.Register(job.strName, emptyInternalBin);
    }

    public static JsonInstallable Work(string id, string source, string template, string target, string skill) =>
        new JsonInstallable { strName = id, strActionCO = source, strActionGroup = "Work", strInteractionTemplate = template,
            CTThem = target, aInputs = Array.Empty<string>(), aToolCTsUse = new[] { "TIsToolMortorq", "TIsToolSoldering" },
            fDuration = .001f, fTargetPointRange = 2, strAllowLootCTsUs = "CTWorkProgress" + skill,
            strCTThemMultCondUs = "StatInstallRate" + skill, strCTThemMultCondTools = "IsToolMortorq" };

    /// <summary>A non-sortable remainder, with native runtime art. Does not pretend to be a full native scrap unit.</summary>
    public static void Remainder(NativeDefinitions d, string id, string title, double mass)
    {
        var item = NativeDefinitions.Clone(DataHandler.dictCOs["ItmScrapTrash"]);
        item.strName = id; item.strNameFriendly = item.strNameShort = title;
        item.strDesc = "Retained mixed material. No refining recipe yet; not a native parts bundle or sortable trash.";
        item.nStackLimit = 1;
        item.aStartingConds = new[] { "IsSolid=1x1", "IsCategoryTrash=1x1", "StatMass=1x" + mass.ToString(CultureInfo.InvariantCulture), "StatBasePrice=1x0.01" };
        item.aUpdateCommands = Array.Empty<string>();
        d.Objects.Add(id, item);
    }
}
