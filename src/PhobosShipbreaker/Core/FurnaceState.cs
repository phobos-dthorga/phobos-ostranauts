using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

/// <summary>Version-one batch record. Loading never grants permission to heat.</summary>
public sealed class FurnaceState
{
    public readonly FurnaceBatch Batch = new();
    public readonly List<string> Inputs = new();
    public string ShipId = "", RoomId = "";
    public double LastEpoch;
    public bool NativeMutation;
    public Dictionary<string, string> Save()
    {
        var b = Batch;
        var f = new Dictionary<string, string>(StringComparer.Ordinal);
        void Put(string key, double value) => f[key] = value.ToString("R", CultureInfo.InvariantCulture);
        Put("revision", FurnaceRules.RecipeRevision); Put("phase", (int)b.Phase);
        f["nativeCommit"] = NativeMutation ? "1" : "0";
        Put("hot", b.HotKJ); Put("hold", b.Hold); Put("pump", b.PumpSeconds); Put("epoch", LastEpoch);
        Put("heat", b.HeatCapKW); Put("ramp", b.RampKPerSecond); Put("cool", b.CoolingCapKW);
        f["qualified"] = b.Qualified ? "1" : "0"; f["step"] = b.StepMode ? "1" : "0";
        f["ship"] = ShipId.Length == 0 ? "-" : ShipId; f["room"] = RoomId.Length == 0 ? "-" : RoomId;
        Put("inputs", Inputs.Count);
        for (int i = 0; i < Inputs.Count; i++) f["input." + i] = Inputs[i];
        Gas("chamber", b.Chamber); Gas("receiver", b.Receiver);
        return f;
        void Gas(string key, GasParcel parcel)
        {
            Put(key + ".temperature", parcel.Moles == 0 ? FurnaceRules.ReferenceK : parcel.TemperatureK);
            Put(key + ".count", parcel.Species.Count);
            int i = 0;
            foreach (var pair in parcel.Species.OrderBy(p => p.Key, StringComparer.Ordinal))
            { f[key + ".id." + i] = pair.Key; Put(key + ".mol." + i++, pair.Value); }
        }
    }
    public static bool TryLoad(IReadOnlyDictionary<string, string> fields, out FurnaceState state)
    {
        state = new FurnaceState(); var s = state; var b = s.Batch;
        try
        {
            double Number(string key, double min, double max)
            {
                if (!fields.TryGetValue(key, out var text) || !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double n) ||
                    !ThermalMath.Finite(n) || n < min || n > max) throw new FormatException(key);
                return n;
            }
            int Integer(string key, int min, int max) { double n = Number(key, min, max); if (n != Math.Truncate(n)) throw new FormatException(key); return (int)n; }
            string Id(string key) { if (!fields.TryGetValue(key, out var id) || string.IsNullOrWhiteSpace(id)) throw new FormatException(key); return id!; }
            if (Integer("revision", 1, 1) != FurnaceRules.RecipeRevision) return false;
            if (Integer("nativeCommit", 0, 1) != 0) return false;
            b.Phase = (FurnacePhase)Integer("phase", 0, (int)FurnacePhase.Ready);
            b.HotKJ = Number("hot", -15000, 1000000); b.Hold = Number("hold", 0, FurnaceRules.HoldSeconds + 1);
            b.PumpSeconds = Number("pump", 0, 100000); s.LastEpoch = Number("epoch", 0, double.MaxValue);
            b.HeatCapKW = Number("heat", FurnaceRules.MinPowerSettingKW, FurnaceRules.HeatLimitKW); b.RampKPerSecond = Number("ramp", FurnaceRules.MinRamp, FurnaceRules.MaxRamp);
            b.CoolingCapKW = Number("cool", FurnaceRules.MinPowerSettingKW, FurnaceRules.CoolingKW);
            b.Qualified = Integer("qualified", 0, 1) == 1; b.StepMode = Integer("step", 0, 1) == 1;
            s.ShipId = Id("ship"); s.RoomId = Id("room"); if (s.ShipId == "-") s.ShipId = ""; if (s.RoomId == "-") s.RoomId = "";
            int count = Integer("inputs", 0, FurnaceRules.ChargeUnits);
            for (int i = 0; i < count; i++) s.Inputs.Add(Id("input." + i));
            if (s.Inputs.Distinct(StringComparer.Ordinal).Count() != count ||
                (b.Phase == FurnacePhase.Idle ? count != 0 : count != FurnaceRules.ChargeUnits)) return false;
            Gas("chamber", b.Chamber); Gas("receiver", b.Receiver);
            if (b.TemperatureK <= 0 || b.TemperatureK > 5000 || (b.Phase == FurnacePhase.Idle && (b.Chamber.Moles != 0 || b.Receiver.Moles != 0))) return false;
            b.Armed = false; b.StepWaiting = false; b.Hold = 0;
            return true;
            void Gas(string key, GasParcel parcel)
            {
                double temp = Number(key + ".temperature", 1, 5000);
                int n = Integer(key + ".count", 0, 32);
                for (int i = 0; i < n; i++)
                {
                    string id = Id(key + ".id." + i);
                    if (!id.StartsWith("StatGasMol", StringComparison.Ordinal) || id == "StatGasMolTotal" || parcel.Species.ContainsKey(id)) throw new FormatException(key);
                    parcel.Add(id, Number(key + ".mol." + i, double.Epsilon, 100), temp);
                }
            }
        }
        catch (FormatException) { return false; }
    }
}
