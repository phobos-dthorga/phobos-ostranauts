using System;

namespace Phobos.Ostranauts.Framework.Controls;

internal static class FixedTextMetrics
{
    internal const float LineHeightEm = 1.12f;

    // TMP lineSpacing is a percentage of font size, not pixels or native font units.
    internal static float LineSpacing(float pointSize, float scale, float lineHeight)
    {
        if (!Positive(pointSize) || !Positive(scale) || !Positive(lineHeight)) return 0;
        return 100 * (LineHeightEm - lineHeight * scale / pointSize);
    }
    private static bool Positive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
}
