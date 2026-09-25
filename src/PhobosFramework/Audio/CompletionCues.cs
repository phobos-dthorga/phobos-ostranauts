namespace Phobos.Ostranauts.Framework.Audio;

/// <summary>One shared optional presentation channel; content owns genuine completion facts.</summary>
public static class CompletionCues
{
    internal static CompletionAudio? Player;
    public static string VolumeLabel => Player?.VolumeLabel ?? Text.Get("Audio.volume_label", 0);
    public static void CycleVolume() => Player?.CycleVolume();
    public static void Complete(CompletionWatch watch, string actorId, string shipId, bool advanced = true)
    {
        // Consume even when muted/unavailable. No event or saved-state replay queue.
        if (watch.Commit(actorId, shipId, advanced)) Player?.Play(actorId, shipId);
    }
}

[HarmonyLib.HarmonyPatch]
internal static class CompletionReloadPatch
{
    private static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods() =>
        System.Linq.Enumerable.Where(typeof(CrewSim).GetMethods(), m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
    private static void Prefix() => CompletionCues.Player?.Stop();
}
