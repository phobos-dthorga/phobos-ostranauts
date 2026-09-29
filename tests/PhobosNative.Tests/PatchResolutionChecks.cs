using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

/// <summary>Every attribute-declared Harmony patch in every Phobos plugin must resolve to exactly one game method.
/// An ambiguous or missing target throws inside PatchAll and aborts that plugin's whole start-up (29 September 2026:
/// Framework 0.45.0 patched the overloaded Ship.AddCO by name alone). Patches that name their target in code
/// (TargetMethod/TargetMethods) are resolved by that code and are not covered here.</summary>
internal static class PatchResolutionChecks
{
    internal static void Run(Action<bool, string> check)
    {
        // Harmony's own resolver needs the game's Mono runtime; this applies its rule directly: exactly one method
        // declared on the target type with that name, narrowed by the argument types when the attribute gives them.
        var declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var assemblies = new[] { typeof(Phobos.Ostranauts.Framework.FrameworkPlugin), typeof(PhobosShipbreaker.Plugin), typeof(PhobosAutoNav.Plugin),
            typeof(PhobosAgriculture.Plugin), typeof(PhobosManufacturing.Plugin), typeof(PhobosWarDeclared.Plugin) }.Select(t => t.Assembly).Distinct().ToArray();
        int resolved = 0;
        foreach (var assembly in assemblies)
        {
            IEnumerable<Type> types;
            try { types = assembly.GetTypes(); } catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null)!; }
            foreach (var type in types)
            {
                var own = type.GetCustomAttributes<HarmonyPatch>(false).Select(a => a.info).ToList();
                var methods = type.GetMethods(declared).Where(m => m.GetCustomAttributes<HarmonyPatch>(false).Any()).ToArray();
                if (own.Count == 0 && methods.Length == 0) continue;
                if (type.GetMethods(declared).Any(m => m.Name == "TargetMethod" || m.Name == "TargetMethods")) continue;
                var infos = new List<List<HarmonyMethod>>();
                if (methods.Length == 0) infos.Add(own);
                foreach (var method in methods) infos.Add(own.Concat(method.GetCustomAttributes<HarmonyPatch>(false).Select(a => a.info)).ToList());
                foreach (var group in infos)
                {
                    // Later attributes refine earlier ones, as Harmony merges them.
                    Type? declaring = null; string? name = null; Type[]? arguments = null; MethodType? kind = null;
                    foreach (var info in group)
                    {
                        declaring = info.declaringType ?? declaring; name = info.methodName ?? name; arguments = info.argumentTypes ?? arguments; kind = info.methodType ?? kind;
                    }
                    if (declaring == null || kind == MethodType.Constructor || kind == MethodType.StaticConstructor || string.IsNullOrEmpty(name)) continue;
                    string label = $"{type.FullName} -> {declaring.Name}.{name}";
                    var candidates = (kind == MethodType.Getter || kind == MethodType.Setter
                        ? declaring.GetProperties(declared).Where(pr => pr.Name == name).Select(pr => kind == MethodType.Getter ? pr.GetMethod : pr.SetMethod).Where(m => m != null)
                        : declaring.GetMethods(declared).Where(m => m.Name == name)).Cast<MethodBase>().ToList();
                    if (arguments != null)
                        candidates = candidates.Where(m => m.GetParameters().Select(pr => pr.ParameterType).SequenceEqual(arguments)).ToList();
                    check(candidates.Count == 1, "Harmony patch resolves to exactly one game method (" + candidates.Count + "): " + label);
                    resolved++;
                }
            }
        }
        check(resolved > 40, "The patch sweep covered the plugins' declared patches (" + resolved + ")");
    }
}
