using System;
using System.Globalization;
using Phobos.Ostranauts.Framework.Controls;

internal static class InstrumentNumberChecks
{
    internal static void Run(Action<bool,string> check)
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            foreach (string culture in new[] { "en-US", "de-DE", "fr-FR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                check(InstrumentNumber.TryFormat(700.25, 1, 7, out var digits, out int dot, out bool negative) &&
                    digits == "   7003" && dot == 5 && !negative, "Decimal artwork is right aligned and independent of decimal-comma locale");
                check(InstrumentNumber.TryFormat(-12.34, 2, 7, out digits, out dot, out negative) &&
                    digits == "   1234" && dot == 4 && negative, "Negative readings keep their sign separately without indexing a minus glyph");
                check(InstrumentNumber.TryFormat(-.001, 1, 7, out _, out _, out negative) && !negative, "Rounding does not show negative zero");
                check(!InstrumentNumber.TryFormat(999999.99, 1, 7, out _, out _, out _), "Rounding overflow blanks the display rather than clipping its magnitude");
                check(InstrumentNumber.TryFormat(25, 0, 7, out digits, out dot, out _) && digits == "     25" && dot == -1, "Integer readings disable all dots");
            }
            foreach (double? value in new double?[] { null, double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.MaxValue })
                check(!InstrumentNumber.TryFormat(value, 1, 7, out _, out _, out _), "Unavailable or unrepresentable readings never become zero");
            check(!InstrumentNumber.TryFormat(1, -1, 7, out _, out _, out _) && !InstrumentNumber.TryFormat(1, 7, 7, out _, out _, out _), "Unsupported precision is rejected");
        }
        finally { CultureInfo.CurrentCulture = before; }
    }
}
