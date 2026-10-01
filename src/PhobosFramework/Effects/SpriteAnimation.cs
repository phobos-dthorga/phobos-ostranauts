using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Events;

namespace Phobos.Ostranauts.Framework.Effects;

/// <summary>Plays a sprite sheet on an installed item with the game's own frame animation (the one its heater and
/// firing guns use), switched on and off from code. The game applies a sheet through <c>Item.SetAlt</c> and steps
/// it from its own per-frame event, paused with the game. It never detaches that step when the art changes and adds
/// another on every start, so this helper removes the item's step before each change. Each sheet cell must be the
/// item's footprint at sixteen pixels a tile. Nothing is saved: a loaded item shows its plain image until content
/// starts the sheet again.</summary>
public static class SpriteAnimation
{
    private static readonly MethodInfo? step = typeof(Item).GetMethod("OnItemAnimationUpdate", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
    private static readonly ConditionalWeakTable<Item, string> playing = new();

    /// <summary>Whether this game build has the frame step this helper manages.</summary>
    public static bool Supported => step != null;

    public static bool IsRunning(Item? item) => item != null && playing.TryGetValue(item, out _);

    /// <summary>Starts the sheet, or leaves it running when it already plays. False when the game cannot play it.</summary>
    public static bool Start(Item item, string sheet, string normal, string damaged, JsonItemAnimation animation)
    {
        if (item == null || item.rend == null || item.jid == null || animation == null || string.IsNullOrEmpty(sheet) || step == null) return false;
        if (playing.TryGetValue(item, out var current) && current == sheet && item.ImgOverride == sheet) return true;
        Detach(item);
        playing.Remove(item);
        item.SetAlt(sheet, normal, damaged, item.jid.strDmgColor, animation);
        playing.Add(item, sheet);
        return true;
    }

    /// <summary>Stops a sheet this helper started and shows the item's own image again.</summary>
    public static void Stop(Item item)
    {
        if (item == null || !playing.TryGetValue(item, out _)) return;
        playing.Remove(item);
        if (item.rend == null) return;
        Detach(item);
        item.SetAlt(null, null);
    }

    private static void Detach(Item item)
    {
        if (step == null) return;
        var call = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), item, step);
        Item.ItemAnimationUpdate.RemoveListener(call);
    }
}
