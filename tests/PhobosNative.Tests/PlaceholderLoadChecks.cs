using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Phobos.Ostranauts.Framework.Registration;

internal static class PlaceholderLoadChecks
{
    internal static void Run(NativeDefinitions definitions, Action<bool, string> check)
    {
        var spawn = typeof(Ship).GetMethod("SpawnItems", BindingFlags.NonPublic | BindingFlags.Instance);
        check(spawn != null && spawn.ReturnType == typeof(void), "Native saved-grid hook resolves");
        var args = spawn!.GetParameters().ToDictionary(p => p.Name!);
        check(args["bTemplateOnly"].ParameterType == typeof(bool) && args["nLoad"].ParameterType == typeof(Ship.Loaded) &&
            args["dictPlaceholders"].ParameterType == typeof(Dictionary<string, CondOwner>).MakeByRefType(),
            "Harmony hook binds real load mode and spawned placeholder arguments");
        check(typeof(Ship).GetField("goTiles", BindingFlags.NonPublic | BindingFlags.Instance)?.FieldType == typeof(UnityEngine.GameObject),
            "Native padding receives the ship's own tile parent");
        var calls = Calls(typeof(Ship).GetMethod("InitShip")!);
        int spawnIndex = calls.FindLastIndex(m => Equals(m, spawn));
        int zonesIndex = calls.FindIndex(m => m.Name == "SetZoneData");
        int roomsIndex = calls.FindIndex(m => m.Name == "CreateRooms");
        int placeholdersIndex = calls.FindIndex(m => m.Name == "GetCOPlaceholder");
        check(spawnIndex >= 0 && zonesIndex > spawnIndex && roomsIndex > zonesIndex && placeholdersIndex > roomsIndex,
            "Inspected native loader restores zones/rooms after spawn but before full placeholder geometry");
        check(DataHandler.dictCOs["Placeholder"].strItemDef == "Blank" &&
            DataHandler.dictItemDefs["Blank"].nCols == 1 && DataHandler.dictItemDefs["Blank"].aSocketAdds.Length == 1,
            "Native construction marker initially has the one-tile geometry implicated by the report");
        foreach (string target in new[] { "PhobosShipbreakerInstalled", "PhobosHullChuteInstalled", "PhobosExteriorGrabberInstalled",
            "PhobosExteriorGrabberInstalledDmg" })
            check(definitions.Installables.Values.Any(j => j.strStartInstall == target),
                "Mitigation ownership can be derived from published installation targets: " + target);
    }

    // Inspect the installed assembly without starting Harmony's Unity/Mono runtime
    // machinery inside the standalone .NET test host.
    internal static List<MethodBase> Calls(MethodInfo method)
    {
        var codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
            .ToDictionary(c => unchecked((ushort)c.Value));
        byte[] il = method.GetMethodBody()!.GetILAsByteArray()!;
        var calls = new List<MethodBase>();
        for (int offset = 0; offset < il.Length;)
        {
            ushort code = il[offset++];
            if (code == 0xfe) code = (ushort)(0xfe00 | il[offset++]);
            var op = codes[code];
            if (op == OpCodes.Call || op == OpCodes.Callvirt)
                calls.Add(method.Module.ResolveMethod(BitConverter.ToInt32(il, offset))!);
            offset += op.OperandType switch {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, offset),
                _ => 4
            };
        }
        return calls;
    }
}
