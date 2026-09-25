using System;
using System.Globalization;
using Phobos.Ostranauts.Framework.Processing;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Culture-independent digit artwork; signs, units and unavailable text stay localized outside it.</summary>
public static class InstrumentNumber
{
    public static bool TryFormat(double? value, int places, int capacity, out string digits, out int dot, out bool negative)
    {
        digits = ""; dot = -1; negative = false;
        if (!value.HasValue || !ThermalMath.Finite(value.Value) || places < 0 || places > 6 || capacity < 1 || capacity > 16) return false;
        double rounded = Math.Round(value.Value, places, MidpointRounding.AwayFromZero);
        negative = rounded < 0;
        string text = Math.Abs(rounded).ToString("F" + places, CultureInfo.InvariantCulture);
        int point = text.IndexOf('.');
        string raw = text.Replace(".", "");
        if (raw.Length > capacity) return false;
        digits = raw.PadLeft(capacity, ' ');
        dot = point < 0 ? -1 : capacity - raw.Length + point - 1;
        return true;
    }
}
