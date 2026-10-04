using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Health;

/// <summary>Lets equipment lift the game's weightless wound-healing penalty for a patient in its care (Framework
/// 0.84.0, first used by Phobos Medical's Ward-3). The game's private <c>Wound.Run</c> multiplies a patient's wound
/// healing rate by a fixed 0.05 when the patient has <c>DcGrav01</c> (microgravity); the same rate also eases bleeding.
/// One transpiler replaces that constant with <see cref="Factor"/>, which returns the larger of the game's own factor
/// and the patient's <see cref="FactorStat"/>, capped at 1. Nobody carries the stat unless a mod gives it, so without
/// one the game behaves exactly as before.
///
/// The patch is applied by hand, not by attribute: if the game's code no longer has exactly one such constant after
/// the gravity test, the method is left untouched, <see cref="Available"/> stays false and the rest of Framework still
/// starts. Callers say plainly that weightless care is unavailable on that game version.</summary>
public static class WoundGravity
{
    /// <summary>The patient stat equipment sets to the share of normal healing it restores in weightlessness.</summary>
    public const string FactorStat = "StatPhobosWoundGravityFactor";
    public const string GravityCondition = "DcGrav01";
    /// <summary>The game's own factor, as inspected in Ostranauts 1.0.1.5 (<c>num2 *= 0.05</c>).</summary>
    public const double NativeFactor = 0.05;
    public static bool Available { get; private set; }

    /// <summary>The healing factor for a weightless patient: the game's own, or the stat equipment gave, at most 1.</summary>
    public static double Factor(CondOwner patient, double vanilla)
    {
        if (patient == null) return vanilla;
        double lifted = patient.GetCondAmount(FactorStat);
        return double.IsNaN(lifted) || lifted <= vanilla ? vanilla : Math.Min(1, lifted);
    }

    internal static void Apply(Harmony harmony, Action<string> log)
    {
        Available = false;
        try
        {
            var run = AccessTools.Method(typeof(Wound), "Run");
            if (run == null) { log("Weightless care unavailable: Wound.Run not found."); return; }
            harmony.Patch(run, transpiler: new HarmonyMethod(typeof(WoundGravity).GetMethod(nameof(Transpile), BindingFlags.NonPublic | BindingFlags.Static)));
            if (!Available) log("Weightless care unavailable: the game's wound-gravity constant was not found exactly once; Wound.Run left unchanged.");
        }
        catch (Exception ex) { Available = false; log("Weightless care unavailable: " + ex.Message); }
    }

    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> code)
    {
        var rewritten = Rewrite(code.ToList(), out int matches);
        Available = matches == 1;
        return rewritten;
    }

    /// <summary>The rewrite, pure: find <c>ldloc patient; ldstr "DcGrav01"</c> followed within a few instructions by
    /// <c>ldc.r8 0.05</c>, and put <c>ldloc patient; ldc.r8 0.05; call Factor</c> in place of the constant. Exactly
    /// one match rewrites; any other count returns the original list unchanged.</summary>
    public static List<CodeInstruction> Rewrite(List<CodeInstruction> code, out int matches)
    {
        matches = 0;
        int target = -1; CodeInstruction? patient = null;
        for (int i = 1; i < code.Count; i++)
        {
            if (code[i].opcode != OpCodes.Ldstr || !(code[i].operand is string s) || s != GravityCondition || !IsLoadLocal(code[i - 1].opcode)) continue;
            for (int j = i + 1; j < code.Count && j <= i + 8; j++)
            {
                if (code[j].opcode == OpCodes.Ldc_R8 && code[j].operand is double d && Math.Abs(d - NativeFactor) < 1e-12)
                {
                    matches++; target = j; patient = code[i - 1]; break;
                }
            }
        }
        if (matches != 1 || patient == null) return code;
        var factor = typeof(WoundGravity).GetMethod(nameof(Factor), BindingFlags.Public | BindingFlags.Static)!;
        // The constant's own instruction becomes the patient load, so any label or block pointing at it still does.
        var load = code[target];
        load.opcode = patient.opcode; load.operand = patient.operand;
        var result = new List<CodeInstruction>(code.Count + 2);
        result.AddRange(code.Take(target));
        result.Add(load);
        result.Add(new CodeInstruction(OpCodes.Ldc_R8, NativeFactor));
        result.Add(new CodeInstruction(OpCodes.Call, factor));
        result.AddRange(code.Skip(target + 1));
        return result;
    }

    private static bool IsLoadLocal(OpCode op) => op == OpCodes.Ldloc || op == OpCodes.Ldloc_S || op == OpCodes.Ldloc_0 || op == OpCodes.Ldloc_1 ||
        op == OpCodes.Ldloc_2 || op == OpCodes.Ldloc_3;
}
