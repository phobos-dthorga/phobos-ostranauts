namespace PhobosAgriculture.Core;

/// <summary>Read-only presentation of the saved cohort, shared by world and panel.</summary>
public static class CropAppearance
{
    public const double YoungProgress = .15, MatureProgress = .45, WiltedHealth = .75;

    public static string PlantKey(CropState state)
    {
        string crop = state.CropId == "potato" ? "Potato" : state.CropId == "lettuce" ? "Lettuce" : "";
        if (crop.Length == 0) return "";
        string stage = state.Health <= 0 ? "dead" : state.Health < WiltedHealth ? "wilted" :
            state.Ready ? "harvest" : state.Progress < YoungProgress ? "sprout" :
            state.Progress < MatureProgress ? "young" : "mature";
        return crop + "-" + stage;
    }

    public static string RackKey(CropState state, bool protectedState = false)
    {
        string plant = protectedState ? "" : PlantKey(state);
        return plant.Length == 0 ? "Rack" : "Rack-" + plant;
    }
}
