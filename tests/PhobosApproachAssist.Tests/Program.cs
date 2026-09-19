using System;
using PhobosApproachAssist.Core;

int passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    passed++;
}
void Near(double value, double expected, string name) => Check(Math.Abs(value - expected) < 1e-9, name);

foreach (double dt in new[] { 0.01, 0.1, 0.3, 0.7, 1.5, 2.0 })
{
    var plan = new BurnPlan();
    double integrated = 0;
    for (int i = 0; i < 1000 && plan.Active; i++) integrated += plan.Step(dt, 10) * dt;
    Check(!plan.Active, "Finite completion at dt " + dt);
    Near(integrated, 0.1, "Impulse cap despite fractional final step " + dt);
    Near(plan.Elapsed, 2, "Duration cap " + dt);
    Near(plan.Step(dt, 10), 0, "No thrust after completion " + dt);
}
var paused = new BurnPlan();
Near(paused.Step(0, 10), 0, "Pause returns zero");
Near(paused.Elapsed, 0, "Pause does not advance time");
Check(paused.Active, "Pause preserves armed state");
var weak = new BurnPlan();
Near(weak.Step(2, 0.001) * 2, 0.002, "Weak thrusters respected");
Check(!weak.Active, "Weak ship still ends at duration limit");
var interrupted = new BurnPlan();
interrupted.Step(0.5, 1);
interrupted.Cancel("Manual override");
Near(interrupted.Step(1, 1), 0, "No automatic resumption");
Check(interrupted.Status == "Manual override", "Override reason retained");
foreach (double invalid in new[] { -1.0, 2.1, double.NaN, double.PositiveInfinity })
{
    var p = new BurnPlan();
    Near(p.Step(invalid, 1), 0, "Bad timestep rejected");
    Check(!p.Active, "Bad timestep disarms");
}
foreach (double invalid in new[] { 0.0, -1.0, double.NaN, double.PositiveInfinity })
{
    var p = new BurnPlan();
    Near(p.Step(1, invalid), 0, "Bad acceleration rejected");
    Check(!p.Active, "Bad acceleration disarms");
}
foreach (string? name in new[] { null, "", "pg18", "PhobosApproachAssistTestReal", "autosave_bad_PhobosApproachAssistTest", "autosave_1_pg18", "xPhobosApproachAssistTest" })
    Check(!TestSavePolicy.Allows(name), "Reject non-test save " + name);
foreach (string name in new[] { "PhobosApproachAssistTest", "PhobosApproachAssistTest-P0", "autosave_12_PhobosApproachAssistTest-P0" })
    Check(TestSavePolicy.Allows(name), "Accept named test save " + name);
foreach (double rotation in new[] { 0.0, Math.PI / 4, Math.PI / 2, -Math.PI, 1.3 })
{
    Check(ThrustDirection.TryCreate(3, 4, rotation, out var direction), "Valid direction");
    Near(direction.X * Math.Cos(rotation) - direction.Y * Math.Sin(rotation), 0.6, "World X preserved");
    Near(direction.X * Math.Sin(rotation) + direction.Y * Math.Cos(rotation), 0.8, "World Y preserved");
    double acceleration = direction.AvailableAcceleration(0.02);
    Near((Math.Abs(direction.X) + Math.Abs(direction.Y)) * acceleration / 0.02, 0.1, "Aggregate throttle cap, including diagonals");
    Check(acceleration <= 0.002 + 1e-12, "Cannot gain acceleration by using two axes");
}
Check(!ThrustDirection.TryCreate(0, 0, 0, out _), "Reject zero separation");
Check(!ThrustDirection.TryCreate(double.NaN, 1, 0, out _), "Reject invalid position");
Check(!ThrustDirection.TryCreate(1, 0, double.PositiveInfinity, out _), "Reject invalid rotation");
Check(!ThrustDirection.TryCreate(double.MaxValue, 0, 0, out _), "Reject overflow");
Console.WriteLine($"{passed} controller and test-save checks passed. These are not in-game tests.");
