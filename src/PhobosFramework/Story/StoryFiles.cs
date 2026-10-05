using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Story;

/// <summary>Story data files (Framework 0.110.0). The game keeps data as <c>DataFile</c> objects in a data card's
/// <c>DataStore</c>, listed and opened on any computer or PDA; opening one shows the object's name and description.
/// Framework owns one such definition, <see cref="Definition"/>, whose objects carry a story file id in a Phobos record.
/// The file name is set when the file is made (the game saves an object's own name); the text is filled in each time a
/// computer opens the file, from the loaded packs, so an unknown id reads as a corrupted file instead of breaking.</summary>
internal static class StoryFiles
{
    public const string Definition = "PhobosStoryDataFile", VanillaFile = "DataFile", Card = "ItmDataCard01", Store = "DataStore";
    private const string RecordName = "PhobosStoryFile";

    /// <summary>Framework's data file: the game's own <c>DataFile</c> under our name, so the game treats it as data.</summary>
    internal static void AddDefinition(NativeDefinitions d)
    {
        if (DataHandler.dictCOs == null || !DataHandler.dictCOs.TryGetValue(VanillaFile, out var vanilla)) return;
        var file = NativeDefinitions.Clone(vanilla);
        file.strName = Definition;
        file.strNameFriendly = Text.Get("Story.file_default_name");
        file.strDesc = Text.Get("Story.file_corrupted");
        d.Objects[Definition] = file;
    }

    private static ObjectStateStore Record(CondOwner file) => new(file.mapGUIPropMaps, RecordName, FrameworkInfo.PluginId, 1);
    internal static string? IdOf(CondOwner file) =>
        Record(file).Read(out var fields) == SavedStateStatus.Ready && fields.TryGetValue("id", out var id) ? id : null;

    /// <summary>One data card holding these story files, or null with the reason when the game would not take them.</summary>
    internal static CondOwner? MakeCard(IReadOnlyList<string> ids, out string? problem)
    {
        problem = null;
        var library = StoryContent.Library;
        var card = DataHandler.GetCondOwner(Card);
        if (card == null) { problem = Text.Get("Story.file_no_card", Card); return null; }
        var store = card.GetCOs(true, null)?.FirstOrDefault(c => c != null && c.strCODef == Store);
        if (store == null) { card.Destroy(); problem = Text.Get("Story.file_no_store", Card); return null; }
        foreach (var id in ids)
        {
            if (!library.Files.TryGetValue(id, out var entry)) { problem = Text.Get("Story.unknown_file", id); continue; }
            var file = DataHandler.GetCondOwner(Definition);
            if (file == null) { problem = Text.Get("Story.file_no_card", Definition); continue; }
            if (!Record(file).TryWrite(new Dictionary<string, string> { ["id"] = id })) { file.Destroy(); problem = Text.Get("Story.record_refused"); continue; }
            file.strNameFriendly = StoryContent.Words(entry.Owner, id + ".name", entry.Value.name);
            if (store.AddCO(file, bEquip: false, bOverflow: false, bIgnoreLocks: true) != null) { file.Destroy(); problem = Text.Get("Story.file_full"); }
        }
        return card;
    }

    /// <summary>Just before a computer opens a file: a story file gets its text (or reads as corrupted), and the first
    /// opening is remembered and may start an arc.</summary>
    internal static void Opening(string? id)
    {
        if (string.IsNullOrEmpty(id) || DataHandler.mapCOs == null || !DataHandler.mapCOs.TryGetValue(id!, out var file) || file == null || file.strCODef != Definition) return;
        string? storyId = IdOf(file);
        if (storyId == null || !StoryContent.Library.Files.TryGetValue(storyId, out var entry))
        {
            file.strDesc = Text.Get("Story.file_corrupted");
            return;
        }
        file.strNameFriendly = StoryContent.Words(entry.Owner, storyId + ".name", entry.Value.name);
        file.strDesc = StoryArcs.Fill(StoryContent.Words(entry.Owner, storyId + ".text", entry.Value.text));
        StoryArcs.FileOpened(storyId, entry.Value.startsArc);
    }
}

[HarmonyPatch(typeof(GUIComputer2), "RunFile")]
internal static class StoryFileOpenPatch
{
    private static void Prefix(string ___strStorageRun)
    {
        try { StoryFiles.Opening(___strStorageRun); }
        catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("Story.check_failed", ex.Message)); }
    }
}
