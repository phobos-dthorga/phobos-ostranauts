using System;

int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
void Throws(Action action, string message) { bool failed = false; try { action(); } catch { failed = true; } Check(failed, message); }
RefineryChecks.Run(Check, Throws);
ChargeChecks.Run(Check, Throws);
LeachChecks.Run(Check, Throws);
AcidPlantChecks.Run(Check, Throws);
FermenterChecks.Run(Check, Throws);
ProcessorChecks.Run(Check, Throws);
HydrogenChecks.Run(Check, Throws);
SabatierChecks.Run(Check, Throws);
CrackerChecks.Run(Check, Throws);
ManifoldChecks.Run(Check, Throws);
GasStoreChecks.Run(Check, Throws);
RegulatorChecks.Run(Check, Throws);
Console.WriteLine($"PASS: {checks} Manufacturing chemistry, charge, record and hazard checks on numbers alone. No game session was run.");
