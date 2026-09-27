using System;
using System.Linq;
using System.Reflection;
using Phobos.Ostranauts.Framework.Controls;
using UnityEngine;

internal static class PolarisStyleChecks
{
    internal static void Run(Action<bool,string> check)
    {
        // Evaluate the compiled production palette against a worst-case white
        // native sprite, not the browser's illustrative shaded button texture.
        var palette = PolarisWidgets.ButtonColors;
        check(palette.colorMultiplier == 1, "Polaris never inherits native brightness amplification");
        double L(Color c)
        {
            double Linear(double v) => v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4);
            return .2126 * Linear(c.r) + .7152 * Linear(c.g) + .0722 * Linear(c.b);
        }
        foreach (var face in new[]{palette.normalColor,palette.highlightedColor,palette.pressedColor,palette.selectedColor,palette.disabledColor})
        {
            check((L(PanelWidgets.Ink)+.05)/(L(face)+.05) >= 4.5, "Light labels retain contrast even on a white donor sprite");
            check((L(PolarisWidgets.Accent)+.05)/(L(face)+.05) >= 4.5, "Selected gold labels retain contrast on every button state");
        }
        check(palette.normalColor != palette.highlightedColor && palette.normalColor != palette.pressedColor &&
              palette.normalColor != palette.disabledColor, "Enabled, hover, pressed and disabled faces are distinct");
        var binding = typeof(PolarisWidgets).Assembly.GetType("Phobos.Ostranauts.Framework.Controls.PolarisButton")!
            .GetMethod("Bind", BindingFlags.NonPublic|BindingFlags.Instance)!;
        var calls = PlaceholderLoadChecks.Calls(binding);
        check(calls.Any(m=>m.DeclaringType==typeof(PolarisWidgets)&&m.Name=="get_ButtonColors") &&
              !calls.Any(m=>m.Name=="get_colors"), "Actual native-widget binding uses the tested palette, not donor state colours");
    }
}
