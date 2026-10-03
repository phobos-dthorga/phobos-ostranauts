using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>Crew jobs for what the ML-2 leaves behind (Shipbreaker 0.71.0; owner decision, 4 October 2026). The laser
/// gathers nothing and never touches an opened ore deposit. With these two settings on, each finished cut paints the
/// game's own jobs on its result, exactly as the player's PDA would: Haul on the freed panel or the ore and gangue,
/// and Mine on a deposit it opened. The crew, their duties, tools, haul zone and the ore itself stay the game's.
/// Both are off until chosen, and a fault here never reaches the cut.</summary>
internal static partial class LaserService
{
    /// <summary>One saved choice in the laser's settings record, leaving the others as they are.</summary>
    private static bool WriteChoice(CondOwner co, string key, string value)
    {
        var store = FilterStore(co);
        var status = store.Read(out var saved);
        if (status != SavedStateStatus.Ready && status != SavedStateStatus.Missing) return false;
        var fields = status == SavedStateStatus.Ready ? new Dictionary<string, string>(saved, StringComparer.Ordinal) : new Dictionary<string, string>(StringComparer.Ordinal);
        fields[key] = value;
        return store.TryWrite(fields);
    }
    private static bool Choice(CondOwner co, string key) =>
        FilterStore(co).Read(out var fields) == SavedStateStatus.Ready && fields.TryGetValue(key, out var value) && LaserRules.ParseSwitch(value, out bool on) && on;
    internal static bool HaulJobs(CondOwner co) => Choice(co, LaserRules.HaulJobsKey);
    internal static bool DepositJobs(CondOwner co) => Choice(co, LaserRules.DepositJobsKey);
    internal static string SwitchLabel(bool on) => Text.Get(on ? "Laser.jobs_on" : "Laser.jobs_off");

    internal static bool SetJobs(CondOwner co, ConsoleBinding? binding, string key, string id, out string message)
    {
        message = ProcessingService.AccessProblem(co, binding) ?? "";
        if (message.Length != 0) return false;
        if (!LaserRules.ParseSwitch(id, out bool on)) { message = Text.Get("Industry.unsupported_action"); return false; }
        if (!WriteChoice(co, key, id)) { message = Text.Get("Laser.save"); return false; }
        bool haul = key == LaserRules.HaulJobsKey;
        message = !on ? Text.Get(haul ? "Laser.haul_jobs_off" : "Laser.deposit_jobs_off")
            : haul ? Text.Get("Laser.haul_jobs_on") + (NativeJobs.HasStockpile(co.ship) ? "" : " " + Text.Get("Laser.haul_no_zone"))
            : Text.Get("Laser.deposit_jobs_on");
        return true;
    }

    private static bool LaserOutput(CondOwner co)
    {
        if (WallIdentity.OrdinaryLooseWall(co)) return true;
        var mined = NativeDefinitions.Trigger(BinRules.NativeMiningOutput);
        return mined != null && mined.Triggered(co);
    }

    /// <summary>Paints the game's jobs on what one finished cut left. Called before the record forgets the object.
    /// The cut object keeps its id through the game's mode switch, so the freed panel or the first thing a rock
    /// became is found by that id; anything else the rock dropped lies within two tiles.</summary>
    private static void QueueJobs(CondOwner laser, Session? s, Ship target, LaserRecord r, CondOwner? found, Vector3? at)
    {
        try
        {
            bool haul = HaulJobs(laser), deposits = DepositJobs(laser);
            if (!haul && !deposits) return;
            if (deposits && r["kind"] == LaserRules.Rock && NativeJobs.MineDeposit(found) && s != null) s.DepositJobs++;
            if (!haul) return;
            int queued = found != null && LaserOutput(found) && NativeJobs.Haul(found) ? 1 : 0;
            Vector2? point = found != null ? found.GetPos() : at.HasValue ? (Vector2)at.Value : (Vector2?)null;
            if (point.HasValue) queued += NativeJobs.HaulNear(target, point.Value, LaserRules.JobSearchTiles, LaserOutput);
            if (s != null) s.HaulJobs += queued;
            if (queued > 0 && (s == null || !s.ZoneWarned) && !NativeJobs.HasStockpile(laser.ship))
            {
                if (s != null) s.ZoneWarned = true;
                PlayerNotices.Post(laser.ship, "shipbreaker.laser.zone." + laser.strID, NoticeLevel.Caution, Text.Get("Laser.haul_no_zone"));
            }
        }
        catch (Exception ex) { Plugin.Log(ex.Message); }
    }
}
