using System;
using Phobos.Ostranauts.Framework.Effects;
using PhobosShipbreaker.Core;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>What the player sees while the ML-2 cuts: the head's firing sheet through the game's own frame
/// animation, and a beam from the emitter to the object in hand. Presentation only, never saved and never read by
/// the service. Any art or rendering fault switches the display off for the session; the laser keeps working.</summary>
internal static class LaserPresentation
{
    internal const string Folder = "phobos/shipbreaker/";
    internal const string Sheet = Folder + "PhobosMiningLaserSheet", Beam = Folder + "PhobosLaserBeam";
    // A light warm tint: the beam texture carries its own pale core and orange falloff.
    internal static readonly Color BeamColour = new(1f, 0.9f, 0.8f, 1f);
    private static bool failed, sheetChecked, sheetReady;

    internal static JsonItemAnimation Animation() => new JsonItemAnimation { strName = LaserRules.Prefix + "Firing",
        nFrameCount = LaserRules.SheetFrames, strFrameRate = LaserRules.SheetFrameRate, bLoop = true, bRandomStartingFrame = false,
        nSheetColumns = LaserRules.SheetColumns, nSheetRows = LaserRules.SheetRows };

    internal static void Refresh(CondOwner co, bool firing, Vector3? target)
    {
        if (failed || co == null || co.bDestroyed || co.Item == null) return;
        try
        {
            bool show = firing && Plugin.Options.LaserEffects;
            if (show && SheetReady()) SpriteAnimation.Start(co.Item, Sheet, Sheet + "Normal", "blank", Animation());
            else SpriteAnimation.Stop(co.Item);
            var beam = WorldBeam.For(co, show && target.HasValue);
            if (beam == null) return;
            if (show && target.HasValue)
            {
                beam.Configure(Beam, new Vector2((float)LaserRules.EmitterPixelsX, (float)LaserRules.EmitterPixelsY), BeamColour, (float)LaserRules.BeamThicknessTiles);
                beam.Aim(target.Value);
            }
            else beam.Hide();
        }
        catch (Exception e) { failed = true; Plugin.Log("Mining laser display switched off for this session: " + e.Message); }
    }

    // The game sizes each frame from the sheet, so a wrong sheet would draw the head at the wrong size.
    private static bool SheetReady()
    {
        if (sheetChecked) return sheetReady;
        sheetChecked = true;
        int width = LaserRules.SheetColumns * LaserRules.Footprint * LaserRules.PixelsPerTile, height = LaserRules.SheetRows * LaserRules.Footprint * LaserRules.PixelsPerTile;
        var colour = DataHandler.LoadPNG(Sheet + ".png", bNorm: false); var normal = DataHandler.LoadPNG(Sheet + "Normal.png", bNorm: true);
        sheetReady = colour != null && normal != null && colour.width == width && colour.height == height && normal.width == width && normal.height == height;
        if (sheetReady) { colour!.filterMode = FilterMode.Point; normal!.filterMode = FilterMode.Point; }
        else Plugin.Log("Mining laser firing sheet is missing or the wrong size; the head stays on its plain image.");
        return sheetReady;
    }
    internal static void Reset() { failed = false; sheetChecked = false; sheetReady = false; }
}
