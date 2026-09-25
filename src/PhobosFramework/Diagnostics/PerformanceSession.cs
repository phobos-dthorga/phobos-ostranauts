using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Phobos.Scope.Recording;

namespace Phobos.Ostranauts.Framework.Diagnostics;

/// <summary>Capture service independent of native game types. Commands delegate here.</summary>
internal sealed class PerformanceSession
{
    internal const int DefaultSeconds = 60, DefaultRecords = 20_000;
    private readonly Recorder recorder = new();
    private readonly string directory;
    private readonly Func<bool> worldReady;
    private readonly Func<IReadOnlyDictionary<string, string>> metadata;
    private readonly Action<string> log;
    private readonly List<ContextProvider> contexts = new();
    private bool faulted;
    private string? exportedCapture;
    internal bool IsRecording => !faulted && recorder.IsRecording;
    internal CaptureSnapshot? Snapshot => recorder.LastCapture;

    internal PerformanceSession(string directory, Func<bool> worldReady,
        Func<IReadOnlyDictionary<string, string>> metadata, Action<string> log)
    { this.directory = Path.GetFullPath(directory); this.worldReady = worldReady; this.metadata = metadata; this.log = log; }

    internal PerformanceMetric RegisterOperation(string name, string category) => new(this, recorder.RegisterOperation(name, category));
    internal PerformanceMetric RegisterIncrement(string name, string category, string unit) => new(this, recorder.RegisterCounter(name, category, unit, CounterKind.Increment));
    internal void RegisterContext(string name, Func<string> read) => contexts.Add(new ContextProvider(recorder.RegisterContext(name, "context"), read));
    internal PerformanceScope Measure(PerformanceMetric operation)
    {
        if (!IsRecording) return default;
        try { return new PerformanceScope(this, recorder.Measure(operation.Metric)); }
        catch (Exception ex) { Fault(ex); return default; }
    }
    internal void Increment(PerformanceMetric counter, double value)
    {
        if (!IsRecording) return;
        try { recorder.Sample(counter.Metric, value); }
        catch (Exception ex) { Fault(ex); }
    }
    internal void Poll()
    {
        if (!IsRecording) return;
        try
        {
            if (!worldReady()) { Stop(StopReason.WorldChange); return; }
            recorder.Poll();
            if (!recorder.IsRecording) return;
            foreach (var context in contexts)
            {
                string value = context.Read();
                if (!string.Equals(value, context.Last, StringComparison.Ordinal))
                { recorder.Context(context.Metric, value); context.Last = value; }
            }
        }
        catch (Exception ex) { Fault(ex); }
    }
    internal void Stop(StopReason reason)
    {
        try { recorder.Stop(reason); }
        catch (Exception ex) { Fault(ex); }
    }
    internal void Fault(Exception ex)
    {
        if (faulted) return;
        faulted = true;
        try { recorder.StopAfterDiagnosticFailure(); } catch { }
        // Never replace a gameplay exception, and never produce per-frame failure spam.
        try { log(Text.Get("Performance.fault", ex.GetType().Name)); } catch { }
    }

    internal bool Command(string[] words, out string response)
    {
        response = Text.Get("Performance.usage");
        if (words.Length < 3) return false;
        string command = words[2].ToLowerInvariant();
        if (command != "start" && words.Length != 3) return false;
        try
        {
            switch (command)
            {
                case "help": return true;
                case "start":
                    if (!TryOptions(words, out var options)) return false;
                    if (faulted) { response = Text.Get("Performance.unavailable"); return false; }
                    if (recorder.IsRecording) { response = Text.Get("Performance.already_active"); return false; }
                    if (Snapshot != null && exportedCapture != Snapshot.CaptureId)
                    { response = Text.Get("Performance.export_previous"); return false; }
                    if (!worldReady()) { response = Text.Get("Performance.world_required"); return false; }
                    options.Metadata = metadata();
                    foreach (var context in contexts) context.Last = null;
                    recorder.Start(options); Poll();
                    response = Describe(); return IsRecording;
                case "status": Poll(); response = Describe(); return true;
                case "stop": Stop(StopReason.Manual); response = Describe(); return true;
                case "export":
                    if (recorder.IsRecording) { response = Text.Get("Performance.stop_first"); return false; }
                    var snapshot = Snapshot;
                    if (snapshot == null) { response = Text.Get("Performance.empty"); return false; }
                    string path = Path.Combine(directory, "phobos-scope-" + snapshot.CaptureId + "-" + Guid.NewGuid().ToString("N") + ".json");
                    snapshot.Export(path); exportedCapture = snapshot.CaptureId;
                    response = Text.Get("Performance.exported", path); return true;
                default: return false;
            }
        }
        catch (Exception ex)
        {
            // Explicit commands may report errors; failed export always keeps the snapshot.
            response = Text.Get(command == "export" ? "Performance.export_failed" : "Performance.command_failed", ex.GetType().Name);
            return false;
        }
    }

    private string Describe()
    {
        var status = recorder.GetStatus();
        if (status == null) return Text.Get(faulted ? "Performance.unavailable" : "Performance.empty");
        string state = Text.Get(faulted ? "Performance.state.fault" : status.IsRecording ? "Performance.state.recording" : "Performance.state.stopped");
        string result = Text.Get("Performance.status", status.CaptureId, state, Text.Get("Performance.mode." + status.Mode),
            status.ElapsedSeconds, status.MaximumSeconds, status.RetainedRecords, status.MaximumRecords,
            status.CompletedScopes, status.OpenScopes, status.IncompleteScopes, status.DroppedRecords, status.RejectedMeasurements);
        if (!status.IsRecording) result += "\n" + Text.Get("Performance.stopped", Text.Get("Performance.reason." + status.StopReason));
        return result;
    }
    internal static bool TryOptions(string[] words, out CaptureOptions options)
    {
        options = new CaptureOptions { MaxDuration = TimeSpan.FromSeconds(DefaultSeconds), MaxRecords = DefaultRecords };
        if (words.Length < 3 || words.Length > 6) return false;
        if (words.Length >= 4)
        {
            if (words[3].Equals("summary", StringComparison.OrdinalIgnoreCase)) options.Mode = CaptureMode.Summary;
            else if (!words[3].Equals("detailed", StringComparison.OrdinalIgnoreCase)) return false;
        }
        if (words.Length >= 5)
        {
            if (!int.TryParse(words[4], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) || seconds < 1 || seconds > Recorder.MaxDurationSeconds) return false;
            options.MaxDuration = TimeSpan.FromSeconds(seconds);
        }
        if (words.Length == 6)
        {
            if (!int.TryParse(words[5], NumberStyles.None, CultureInfo.InvariantCulture, out var records) || records < 1 || records > Recorder.MaxRecordLimit) return false;
            options.MaxRecords = records;
        }
        return true;
    }
    private sealed class ContextProvider
    {
        internal readonly Metric Metric;
        internal readonly Func<string> Read;
        internal string? Last;
        internal ContextProvider(Metric metric, Func<string> read) { Metric = metric; Read = read; }
    }
}
