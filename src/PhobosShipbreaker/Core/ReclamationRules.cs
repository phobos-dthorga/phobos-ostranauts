using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosShipbreaker.Core;

internal enum ReclamationPhase { Seeking, Approaching, Cutting, UninstallPending, TransferPending, Feeding, Egress, Transit, Exhausted, Remnants }
internal sealed class ReclamationRecord
{
    internal const string StoreName = "Shipbreaker.Reclamation";
    internal readonly Dictionary<string,string> Fields = new(StringComparer.Ordinal);
    internal string this[string key] { get => Fields.TryGetValue(key,out var v)?v:""; set => Fields[key]=value; }
    internal ReclamationPhase Phase { get => Enum.Parse<ReclamationPhase>(this["phase"]); set => this["phase"]=value.ToString(); }
    internal double Number(string key) => double.TryParse(this[key],NumberStyles.Float,CultureInfo.InvariantCulture,out var n)?n:double.NaN;
    internal void Number(string key,double n) => this[key]=n.ToString("R",CultureInfo.InvariantCulture);
    internal bool Paid => Number("progress") >= Number("seconds");
    internal bool Valid => new[]{"owner","ship","target","console","module","g4","chute","processor","permission","phase"}.All(k=>ObjectStateStore.SafeValue(this[k])) &&
        this["ship"]!=this["target"] && Enum.TryParse<ReclamationPhase>(this["phase"],out var phase) && Enum.IsDefined(typeof(ReclamationPhase),phase) && phase.ToString()==this["phase"] &&
        ReclamationRules.Finite(Number("completed")) && Number("completed")>=0 && Number("completed")%1==0 &&
        (!Fields.ContainsKey("wall") || ObjectStateStore.SafeValue(this["wall"]) && ReclamationRules.ValidWork(Number("progress"),Number("seconds"),Number("kw"))) &&
        (phase!=ReclamationPhase.Cutting&&phase!=ReclamationPhase.UninstallPending&&phase!=ReclamationPhase.TransferPending&&phase!=ReclamationPhase.Feeding ||
            new[]{"wall","ownPort","targetPort","support","floor"}.All(k=>ObjectStateStore.SafeValue(this[k]))) &&
        (phase!=ReclamationPhase.UninstallPending&&phase!=ReclamationPhase.TransferPending&&phase!=ReclamationPhase.Feeding || Paid);
    internal static bool Read(IReadOnlyDictionary<string,string> fields,out ReclamationRecord r)
    { r=new ReclamationRecord(); foreach(var p in fields) r.Fields[p.Key]=p.Value; return r.Valid; }
    internal void Begin(string wall)
    { this["wall"]=wall; Number("seconds",ReclamationRules.CuttingSeconds); Number("kw",ReclamationRules.CuttingKW); Number("progress",0); Phase=ReclamationPhase.Cutting; }
    internal void Credit(double suppliedKWh)
    {
        if(Phase!=ReclamationPhase.Cutting || !Valid || !ReclamationRules.Finite(suppliedKWh) || suppliedKWh<0) throw new InvalidOperationException("Invalid cutter receipt.");
        Number("progress",Math.Min(Number("seconds"),Number("progress")+suppliedKWh*3600/Number("kw")));
    }
    internal void CompleteTransfer()
    {
        if(Phase!=ReclamationPhase.TransferPending || !Paid) throw new InvalidOperationException("No paid pending transfer.");
        Number("completed",Number("completed")+1); Phase=ReclamationPhase.Feeding;
    }
}
internal static class ReclamationRules
{
    // Authored gameplay balance. Each started job captures these values.
    internal const double CuttingSeconds = 120;
    internal const double CuttingKW = 12;
    internal const double MaximumTargetPressureKPa = .1;
    internal const double StagingHullGapM = 1200;
    internal const int MaximumWindows = 64;
    internal static bool Finite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);
    internal static bool ValidWork(double progress,double seconds,double kw) => Finite(progress)&&Finite(seconds)&&Finite(kw)&&progress>=0&&seconds>0&&seconds<=3600&&progress<=seconds&&kw>0&&kw<=1000;
    internal static bool CanCut(double mass,bool installed,bool damaged,bool empty,bool stacked,bool exposed,bool floor,bool protectedSupport) =>
        ProcessRules.MassMatches(mass,ProcessRules.InputKg)&&installed&&!damaged&&empty&&!stacked&&exposed&&floor&&!protectedSupport;
}
