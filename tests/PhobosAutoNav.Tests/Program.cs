using System;
using PhobosAutoNav.Core;

int checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
void Near(double value, double expected, string name) => Check(Math.Abs(value - expected) < 1e-8, name);

Check(ArrivalBrake.NeedsBrake(4900, 5000, 100, 0, 0.5), "Crossing arrival ring at speed must still brake");
Check(ArrivalBrake.NeedsBrake(5100, 5000, 4, 0, 0.5), "Do not inherit upstream's looser zero-speed completion");
Check(ArrivalBrake.NeedsBrake(4900, 5000, 100, 20, 0.5), "Nonzero arrival request still requires braking");
Check(!ArrivalBrake.NeedsBrake(4900, 5000, 20.4, 20, 0.5), "Accept configured tolerance");
Check(!ArrivalBrake.NeedsBrake(6000, 5000, 100, 0, 0.5), "Normal approach stays with inherited guidance");

foreach (double rotation in new[] { 0, 0.2, Math.PI / 4, Math.PI / 2, -1.6 })
foreach (double dt in new[] { 0.01, 0.5, 2, 10, 60 })
foreach (double arrival in new[] { 0d, 20d })
{
    double vx = 60, vy = 80, full = 2, throttle = 0.25;
    for (int i = 0; i < 200000 && Math.Sqrt(vx * vx + vy * vy) > arrival + 1e-7; i++)
    {
        double before = Math.Sqrt(vx * vx + vy * vy);
        Check(ArrivalBrake.TryCommand(vx, vy, rotation, full, throttle, arrival, dt, out var command), "Valid braking inputs");
        Check(Math.Abs(command.X) + Math.Abs(command.Y) <= throttle + 1e-10, "Aggregate throttle bound");
        double ax = (command.X * Math.Cos(rotation) - command.Y * Math.Sin(rotation)) * full;
        double ay = (command.X * Math.Sin(rotation) + command.Y * Math.Cos(rotation)) * full;
        vx += ax * dt; vy += ay * dt;
        double after = Math.Sqrt(vx * vx + vy * vy);
        Check(after <= before + 1e-9 && after >= arrival - 1e-8, "Braking is monotonic and cannot reverse velocity");
    }
    Near(Math.Sqrt(vx * vx + vy * vy), arrival, "Converges to requested speed under different steps/headings");
}
foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
{
    Check(!ArrivalBrake.TryCommand(invalid, 2, 0, 1, .25, 0, 1, out _), "Reject invalid velocity");
    Check(!ArrivalBrake.TryCommand(1, 2, invalid, 1, .25, 0, 1, out _), "Reject invalid rotation");
    Check(!ArrivalBrake.TryCommand(1, 2, 0, 1, .25, 0, invalid, out _), "Reject invalid timestep");
}
foreach (double dt in new[] { 0d, -1d }) Check(!ArrivalBrake.TryCommand(1, 2, 0, 1, .25, 0, dt, out _), "Reject nonpositive timestep");
foreach (double throttle in new[] { 0d, -1d, 1.1d }) Check(!ArrivalBrake.TryCommand(1, 2, 0, 1, throttle, 0, 1, out _), "Reject unavailable throttle");
Check(!ArrivalBrake.TryCommand(double.MaxValue, 1, 0, 1, .25, 0, 1, out _), "Reject overflow");
Check(ArrivalBrake.TryCommand(0, 0, 0, 1, .25, 0, 1, out var zero) && zero.X == 0 && zero.Y == 0, "No burn when stopped");
Console.WriteLine($"{checks} numerical assertions passed. These are not in-game tests.");
