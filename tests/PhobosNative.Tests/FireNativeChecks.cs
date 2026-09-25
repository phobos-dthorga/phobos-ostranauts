using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ostranauts.Ships;
using Ostranauts.ShipGUIs.NavStation;
using PhobosAutoNav;

internal static class FireNativeChecks
{
    internal static void Run(Action<bool,string> check)
    {
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var panel = typeof(NavModWeaponsControl);
        check(panel.GetField("_weaponsToFire", all)?.FieldType == typeof(List<CondOwner>), "Native queue field matches Harmony injection");
        foreach (string name in new[]{"TargetLockHandler", "KeyHandler"})
            check(panel.GetMethod(name, all)?.GetParameters().Length == 0, "Native queue hook signature: " + name);
        var auto = typeof(WeaponsSystem).GetMethod("ShootAuto", all)!;
        check(auto.ReturnType == typeof(bool) && auto.GetParameters().Select(p=>p.ParameterType).SequenceEqual(new[]{typeof(List<CondOwner>),typeof(ShipSitu)}),
            "Native automatic batch signature retains successful-batch result");
        var patched = new[]{typeof(PursuitWeaponFirePatch),typeof(PursuitWeaponAimPatch),typeof(PursuitWeaponQueuePatch)};
        check(patched.All(t=>t.GetCustomAttributesData().Any(a=>a.AttributeType.FullName=="HarmonyLib.HarmonyPatch")), "Native ownership interception hooks remain declared");
        check(patched.SelectMany(t=>t.GetCustomAttributesData()).SelectMany(a=>a.ConstructorArguments)
            .All(a=>!Equals(a.Value,"ShootManual")), "Ownership interception never patches native deliberate manual shots");
    }
}
