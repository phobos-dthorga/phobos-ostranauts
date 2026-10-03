using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Diagnostics;
using Phobos.Scope.Recording;

int checks = 0;
void Check(bool value, string description) { if (!value) throw new Exception(description); checks++; }
var directory = Path.Combine(Path.GetTempPath(), "phobos-performance-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
bool ready = true; string speed = "1"; int reads = 0, logs = 0;
var session = new PerformanceSession(directory, () => ready,
    () => new Dictionary<string, string> { ["game"] = "synthetic-adapter-check", ["framework_version"] = "test" }, _ => logs++);
Performance.Session = session;
Performance.RoomAlarmRead = Performance.RegisterOperation("test.room_alarm", "observations");
var operation = Performance.RegisterOperation("test.update", "test");
var candidateItems = Performance.RegisterIncrement("test.candidate_items", "test", "items");
Performance.RegisterContext("game.speed_multiplier", () => { reads++; return speed; });
bool Command(string suffix, out string result) => session.Command(("phobosframework perf " + suffix).Split(' '), out result);
JsonElement Capture()
{
    using var bytes = new MemoryStream(); session.Snapshot!.WriteJson(bytes);
    using var json = JsonDocument.Parse(bytes.ToArray()); return json.RootElement.Clone();
}
void SaveFixture(string name)
{
    if (args.Length > 0) session.Snapshot!.Export(Path.Combine(args[0], name + ".json"));
}

try
{
    session.Poll(); using (Performance.Measure(operation)) { }
    Performance.Increment(candidateItems, 9);
    Check(reads == 0 && session.Snapshot == null, "Disabled adapter invokes no context callbacks and creates no capture");
    foreach (var command in new[] { "start bogus", "start detailed 0", "start detailed -1", "start detailed 1.5", "start detailed 3601", "start detailed 10 20001", "stop extra", "export ../escape" })
        Check(!Command(command, out _) && !session.IsRecording, "Invalid command rejected without starting or accepting a path: " + command);
    ready = false; Check(!Command("start", out _), "No recording can start before a world is ready"); ready = true;
    Check(Command("start detailed 30 20", out _) && reads == 1, "Start captures initial context");
    Check(!Command("start", out _) && !Command("export", out _), "Active capture refuses replacement or live export");
    for (int i = 0; i < 100; i++) session.Poll();
    speed = "4"; session.Poll();
    using (Performance.Measure(operation)) { Thread.SpinWait(10_000); Performance.Increment(candidateItems, 7); }
    var open = Performance.Measure(operation);
    session.Stop(StopReason.WorldChange); open.Dispose();
    var data = Capture();
    Check(data.GetProperty("contexts").GetArrayLength() == 2, "Unchanged context is not duplicated; a change is timestamped");
    Check(data.GetProperty("stop_reason").GetString() == "world_change" && !session.IsRecording, "World boundary stops recording");
    var aggregate = data.GetProperty("aggregates")[1];
    Check(aggregate.GetProperty("calls").GetInt64() == 1 && aggregate.GetProperty("incomplete").GetInt64() == 1, "World boundary does not invent a completed duration");
    SaveFixture("adapter-detailed-world-change");
    int Files() => Directory.GetFiles(directory, "*.json").Length;

    // Rolling captures (Framework 0.75.0): a world change keeps recording on; the ended window is exported once, and
    // the next begins only when a world is ready again.
    Check(session.IsRolling && Files() == 0, "A world change keeps recording on and writes nothing by itself");
    ready = false;
    Check(Command("status", out var status) && status.Contains("world changed") && status.Contains("waiting"), "Status explains the world-change stop and the waiting recording");
    Check(Files() == 1 && session.AutoExports == 1 && !session.IsRecording, "The world-change window is exported, and nothing starts without a world");
    session.Poll();
    Check(Files() == 1, "An exported window is not exported again");
    Check(!Command("start", out _), "Start is refused while recording is on");
    ready = true; session.Poll();
    Check(session.IsRecording && session.CaptureSequence == 2, "The next window begins once a world is ready");
    Check(Command("stop", out var stopped) && !session.IsRolling && !session.IsRecording && Files() == 2 && stopped.Contains("Latest of 2"),
        "Stop ends recording and exports the partial window");
    Check(Command("export", out _) && Command("export", out _) && Files() == 4, "Manual export writes the latest window again under fresh names");

    Check(Command("start summary 30 1", out _), "New recording after stop is allowed");
    for (int i = 0; i < 5; i++) { using (Performance.Measure(operation)) Thread.SpinWait(100); }
    Performance.Increment(candidateItems, 3);
    ready = false; session.Poll();
    data = Capture();
    Check(!session.IsRecording && data.GetProperty("events").GetArrayLength() == 0 && data.GetProperty("aggregates")[1].GetProperty("calls").GetInt64() == 5, "Summary mode records complete counts and stops when world readiness is lost");
    Check(data.GetProperty("dropped_records").GetInt64() == 1, "Shared record cap reports dropped counter sample");
    SaveFixture("adapter-summary-world-change");
    ready = true;
    Check(Command("stop", out _) && Files() == 5, "Stop exports the summary window that the world change ended");

    // Full windows roll over: each is exported as it ends and the next begins at once, until stop.
    int before = Files();
    Check(Command("start summary 1 50", out _) && session.CaptureSequence == 4, "One-second windows start");
    Thread.Sleep(1100); session.Poll();
    Check(session.IsRecording && session.CaptureSequence == 5 && Files() == before + 1 && session.AutoExports == 1, "A full window is exported and the next begins at once");
    string newest = Directory.GetFiles(directory, "*.json").OrderBy(File.GetLastWriteTimeUtc).Last();
    using (var exported = JsonDocument.Parse(File.ReadAllText(newest)))
        Check(exported.RootElement.GetProperty("stop_reason").GetString() == "duration_limit", "The exported window ended at its time limit");
    Thread.Sleep(1100); using (Performance.Measure(operation)) { } session.Poll();
    Check(session.IsRecording && session.CaptureSequence == 6 && Files() == before + 2, "A window that ends inside a timing call is exported and followed too");
    Check(Command("stop", out _) && !session.IsRolling && Files() == before + 3 && session.AutoExports == 3, "Stop exports the last partial window");
    session.Poll();
    Check(!session.IsRecording && Files() == before + 3, "Nothing records or exports after stop");

    // A failed automatic export stops recording once and keeps the capture for a manual export.
    var blockedRoll = Path.Combine(directory, "blocked-roll"); File.WriteAllText(blockedRoll, "sentinel");
    int rollLogs = 0;
    var failing = new PerformanceSession(blockedRoll, () => true, () => new Dictionary<string, string>(), _ => rollLogs++);
    Check(failing.Command(new[] { "phobosframework", "perf", "start", "summary", "1" }, out _), "Failing-export fixture starts");
    Thread.Sleep(1100); failing.Poll(); failing.Poll();
    Check(!failing.IsRecording && !failing.IsRolling && rollLogs == 1 && failing.Snapshot != null && File.ReadAllText(blockedRoll) == "sentinel",
        "A failed automatic export stops recording once, keeps the window and writes nothing else");
    File.Delete(blockedRoll); Directory.CreateDirectory(blockedRoll);
    Check(failing.Command(new[] { "phobosframework", "perf", "export" }, out _) && Directory.GetFiles(blockedRoll).Length == 1, "The kept window exports once access is repaired");

    // Exercise the real console prefix with narrow native boundary doubles.
    var prefix = typeof(FrameworkConsole).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)!;
    (bool PassThrough, bool Success, string Output) Resolve(string command)
    {
        object[] arguments = { command, false };
        bool pass = (bool)prefix.Invoke(null, arguments)!;
        return (pass, (bool)arguments[1], (string)arguments[0]);
    }
    Check(Resolve("anothercommand perf start").PassThrough, "Other console owners retain their command");
    Check(Resolve("phobosframework recipes").Success && Resolve("phobosframework help").Success, "Existing Framework commands still work");
    Check(!Resolve("phobosframework status extra").Success, "Legacy short-command validation remains strict");
    var started = Resolve("PHOBOSFRAMEWORK PERF START detailed 10 20");
    Check(!started.PassThrough && started.Success, "Console hierarchy accepts case-insensitive start and bounded options");
    Check(Resolve("phobosframework perf stop").Success, "Console stop delegates to the capture service");
    Check(logs == 0, "Ordinary capture operations do not generate per-frame logs");

    var blocked = Path.Combine(directory, "blocked"); File.WriteAllText(blocked, "sentinel");
    var failed = new PerformanceSession(blocked, () => true, () => new Dictionary<string, string>(), _ => { });
    Check(failed.Command(new[] { "phobosframework", "perf", "start" }, out _), "Export-failure fixture starts");
    failed.Stop(StopReason.Manual); var retained = failed.Snapshot;
    Check(!failed.Command(new[] { "phobosframework", "perf", "export" }, out _) && ReferenceEquals(retained, failed.Snapshot) && File.ReadAllText(blocked) == "sentinel", "Export failure preserves stopped data and existing filesystem content");
    File.Delete(blocked); Directory.CreateDirectory(blocked);
    Check(failed.Command(new[] { "phobosframework", "perf", "export" }, out _), "Same capture exports after destination access is repaired");

    int faultLogs = 0;
    var fault = new PerformanceSession(directory, () => true, () => new Dictionary<string, string>(), _ => { faultLogs++; throw new IOException("log failure"); });
    fault.RegisterContext("fault.provider", () => throw new InvalidOperationException("provider failure"));
    Check(!fault.Command(new[] { "phobosframework", "perf", "start" }, out _) && !fault.IsRecording, "Context failure disables recording without escaping to gameplay");
    for (int i = 0; i < 100; i++) fault.Poll();
    Check(faultLogs == 1 && fault.Snapshot?.RejectedMeasurements == 1, "Diagnostic/log faults produce one attempt and retain a capture with a quality warning");
    session = new PerformanceSession(directory, () => true, () => new Dictionary<string, string>(), _ => { });
    int beforeExit = Directory.GetFiles(directory, "*.json").Length;
    session.Command(new[] { "phobosframework", "perf", "start" }, out _); session.Stop(StopReason.ApplicationExit); session.Poll();
    Check(session.Snapshot!.StopReason == "application_exit" && !session.IsRolling && Directory.GetFiles(directory, "*.json").Length == beforeExit,
        "Orderly exit stops recording without implicit file writes"); 
    FrameChecks.Run(Check, directory);
}
finally { Performance.Session = null; Directory.Delete(directory, recursive: true); }
Console.WriteLine($"{checks} performance adapter checks passed.");
