using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

// Native socket requirements are AND-only. Select the floor alternative for this
// CheckFit call, so native range, collision, dock/zone and preview checks still run.
// Never change the shared definition or leave a preview's sockets on a saved item.
[HarmonyPatch(typeof(Item), nameof(Item.CheckFit))]
internal static class CollectorPlacement
{
    internal static bool Installed(string id) => id == CollectorRules.Installed || id == CollectorRules.Installed + "Dmg";
    private static void Prefix(Item __instance, Vector3 vCenter, Ship objShip, out (List<Loot>? Reqs, List<Loot>? Forbids) __state)
    {
        __state = (null, null);
        if (!Installed(__instance.jid?.strName ?? "") || objShip == null ||
            !CollectorRoute.FloorSupport(objShip, vCenter, __instance.TF.eulerAngles.z) ||
            !CollectorRoute.ServiceClear(objShip, vCenter, __instance.TF.eulerAngles.z)) return;
        var reqs = __instance.aSocketReqs.Select(l => l.strName == "TILWall" ? DataHandler.GetLoot("TILFloor") : l).ToList();
        var forbids = __instance.aSocketForbids.Select(l => l.strName == "PhobosHullChuteForbids" ? DataHandler.GetLoot("TILObstruction") : l).ToList();
        __state = (__instance.aSocketReqs, __instance.aSocketForbids);
        __instance.aSocketReqs = reqs; __instance.aSocketForbids = forbids;
    }
    private static Exception? Finalizer(Item __instance, (List<Loot>? Reqs, List<Loot>? Forbids) __state, Exception? __exception)
    {
        if (__state.Reqs != null) __instance.aSocketReqs = __state.Reqs;
        if (__state.Forbids != null) __instance.aSocketForbids = __state.Forbids;
        return __exception;
    }
}
