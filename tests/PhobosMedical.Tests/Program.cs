using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Health;
using PhobosMedical.Core;

int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
void Throws(Action action, string message) { bool failed = false; try { action(); } catch { failed = true; } Check(failed, message); }

// ---- The care pack ------------------------------------------------------------------------------------------
string shipped = DataPacks.ShippedText(Care.Source);
CarePack Load(string json) => DataPacks.LoadText<CarePack>(json, "", MedicalRules.Owner, CareSchema.Name, CareSchema.Validate);
var pack = Load(shipped);
Care.Use(pack);
var bed = Care.Station(CareSchema.Bed);
Check(bed.idleKW > 0 && bed.idleKW < bed.workingKW && bed.workingKW <= CareSchema.MaxKW, "the shipped bed draws a little idle and more while caring");
Check(Math.Abs(bed.idleKW - 0.0225) < 1e-12, "idle matches the vanilla Infirmaway's 22.5 W");
var a = Care.Admission;
Check(a.bloodLost < CareSchema.FatalBloodLost && a.infection < CareSchema.FatalInfection && a.pain < CareSchema.KnockoutPain && a.wound < 1 && a.dischargeShare < 1,
    "shipped thresholds sit below the game's fatal and knock-out levels");
string Edited(Action<JObject> edit) { var o = JObject.Parse(shipped); edit(o); return o.ToString(); }
Throws(() => Load(Edited(o => o["admission"]!["bloodLost"] = 40)), "an admission threshold at the fatal level is refused");
Throws(() => Load(Edited(o => o["admission"]!["dischargeShare"] = 1)), "a discharge share of one is refused");
Throws(() => Load(Edited(o => o["stations"]!["bed"]!["idleKW"] = 1)), "idle above working is refused");
Throws(() => Load(Edited(o => o["stations"]!["bed"]!["workingKW"] = 5)), "a station above the power cap is refused");
Throws(() => Load(Edited(o => o["stations"]!["monitor"] = new JObject { ["idleKW"] = 0, ["workingKW"] = 0.1 })), "a station the code does not know is refused");
Throws(() => Load(Edited(o => ((JObject)o["stations"]!).Remove("bed"))), "a missing bed station is refused");
Throws(() => Load(Edited(o => o["admission"]!["heal"] = 2)), "an unknown field is refused (no authored healing)");
Check(Math.Abs(Care.WeightlessHealing(CareSchema.Bed) - 1) < 1e-12, "the shipped bed lifts the weightless healing penalty fully");
Throws(() => Load(Edited(o => o["levels"]!["bed"]!["weightlessHealing"] = 0.01)), "weightless healing below the game's own 0.05 is refused");
Throws(() => Load(Edited(o => o["levels"]!["bed"]!["weightlessHealing"] = 1.5)), "weightless healing above normal is refused");
Throws(() => Load(Edited(o => o["levels"]!["monitor"] = new JObject { ["weightlessHealing"] = 0.5 })), "a care level the code does not know is refused");
var noLevels = Load(Edited(o => o.Remove("levels")));
Check(noLevels.levels == null, "a pack without levels (as written for 0.1.0) still loads");
var tuned = Load(Edited(o => o["admission"]!["pain"] = 30));
Check(tuned.admission.pain == 30 && tuned.admission.bloodLost == a.bloodLost, "a tuned threshold loads and the rest stay shipped");

// ---- Decisions ----------------------------------------------------------------------------------------------
Check(BedRules.Decide(false, true, true, true) == CareReason.NoPatient, "no patient, no care");
Check(BedRules.Decide(true, false, true, true) == CareReason.Damaged, "a damaged bed gives no care");
Check(BedRules.Decide(true, true, false, true) == CareReason.NoPower, "honest power: no power, no care");
Check(BedRules.Decide(true, true, true, false) == CareReason.NoAir, "no air, no care (machines do not work in vacuum)");
Check(BedRules.Decide(true, true, true, true) == CareReason.Caring, "patient, intact, powered and in air: care");
Check(BedRules.CareCondition(BedRoute.Resting) == MedicalRules.Recovering, "a resting patient gets the bed's own Recovering, so the game does not treat them as asleep");
Check(BedRules.CareCondition(BedRoute.Asleep) == MedicalRules.SleepingMedical && BedRules.CareCondition(BedRoute.Laid) == MedicalRules.SleepingMedical,
    "a sleeping or unconscious patient gets the game's own medical sleep");
Check(BedRules.Route(true, false, false) == BedRoute.Resting, "resting and awake");
Check(BedRules.Route(true, true, false) == BedRoute.Laid, "a rester knocked out where they lie is an unconscious patient");
Check(BedRules.Route(false, true, true) == BedRoute.Asleep, "asleep in this bed through the game's sleep chain");
Check(BedRules.Route(false, true, false) == BedRoute.Laid, "unconscious, the loop targets the ship: laid");
Check(BedRules.Route(false, false, true) == BedRoute.None, "awake and not resting: not a patient");

PatientFacts Facts(double blood = 0, double infection = 0, double pain = 0, double worst = 0, bool bleeding = false, bool fracture = false, bool splinted = false) =>
    new(blood, infection, pain, new List<WoundFacts> { new("WoundArmLowerL", worst, 0, bleeding ? 0.5 : 0, 0, false, fracture, splinted, false) }, false, false);
Check(!BedRules.Injured(Facts(), a), "a healthy person is not injured");
Check(BedRules.Injured(Facts(blood: a.bloodLost), a) && BedRules.Injured(Facts(infection: a.infection), a) && BedRules.Injured(Facts(pain: a.pain), a) && BedRules.Injured(Facts(worst: a.wound), a),
    "each figure at its threshold makes a person injured");
Check(BedRules.Injured(Facts(bleeding: true), a), "a bleeding wound makes a person injured");
Check(BedRules.Injured(Facts(fracture: true), a) && !BedRules.Injured(Facts(fracture: true, splinted: true), a), "an unsplinted fracture counts, a splinted one does not");
Check(!new WoundFacts("x", 0, 0, 0.5, 0, true, false, false, false).Bleeding && !new WoundFacts("x", 0, 0, 0.09, 0, false, false, false, false).Bleeding,
    "a staunched wound and a rate under the game's floor are not bleeding");
Check(BedRules.Recovered(Facts(), a), "a healthy person has recovered");
double between = a.wound * (1 + a.dischargeShare) / 2;
Check(!BedRules.Injured(Facts(worst: a.wound * 0.9), a) && !BedRules.Recovered(Facts(worst: a.wound * 0.9), a) && !BedRules.Recovered(Facts(worst: between), a),
    "between discharge and admission a patient keeps resting (no bouncing)");
Check(!BedRules.Recovered(Facts(bleeding: true), a), "nobody gets up while bleeding");
Check(BedRules.Severity(Facts(blood: 20)) > BedRules.Severity(Facts(blood: 10)) && BedRules.Severity(Facts(bleeding: true)) > BedRules.Severity(Facts()),
    "the worse off someone is, the sooner a free bed calls them");

// ---- The saved record ---------------------------------------------------------------------------------------
var state = new BedState { Patient = "crew-1234", Route = BedRoute.Laid, Since = 1234.5, Reserved = true, OutageNoticed = true, SendInjured = true };
var round = BedState.Read(state.Save());
Check(round.Patient == state.Patient && round.Route == state.Route && round.Since == state.Since && round.Reserved && round.OutageNoticed && round.SendInjured, "the bed record round-trips");
var legacy = new Dictionary<string, string>(state.Save()); legacy.Remove("send");
var old = BedState.Read(legacy);
Check(old.Patient == state.Patient && old.Reserved && !old.SendInjured, "a bed saved by 0.1.0 (five fields) reads with Send injured crew off");
var empty = BedState.Read(new BedState().Save());
Check(empty.Patient == "" && empty.Route == BedRoute.None && !empty.Reserved, "an empty bed saves as nobody and reads back empty");
Check(new BedState().Save()["patient"] == BedState.Nobody, "an empty patient is saved as a word, never an empty value");
Throws(() => BedState.Read(new Dictionary<string, string>(state.Save()) { ["extra"] = "1" }), "an unknown field is refused");
Throws(() => BedState.Read(new Dictionary<string, string>(state.Save()) { ["route"] = "Floating" }), "an unknown route is refused");
Throws(() => BedState.Read(new Dictionary<string, string>(state.Save()) { ["route"] = "None" }), "a patient with no route is refused");
Throws(() => BedState.Read(new Dictionary<string, string>(state.Save()) { ["since"] = "-1" }), "a negative time is refused");
Throws(() => BedState.Read(new Dictionary<string, string>(state.Save()) { ["reserved"] = "yes" }), "a malformed flag is refused");

Console.WriteLine($"PASS: {checks} care-pack, bed-decision and record checks. No game session was run.");
