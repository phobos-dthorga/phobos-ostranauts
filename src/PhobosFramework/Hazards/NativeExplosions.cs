using System;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Hazards;

/// <summary>Spawns one of the game's own explosion objects (a definition carrying the
/// <c>Explosion,&lt;name&gt;</c> update command) on a ship, the way a mode-switch loot does for an armed
/// charge. The native <c>Explosion</c> component then applies radius damage, shrapnel rays and fire rolls
/// and removes the object; nothing about that behaviour is reproduced here.</summary>
public static class NativeExplosions
{
    public static CondOwner Spawn(Ship ship, Vector3 position, string definitionId)
    {
        if (ship == null) throw new ArgumentNullException(nameof(ship));
        if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("An explosion definition is required.");
        if (DataHandler.dictCOs == null || !DataHandler.dictCOs.TryGetValue(definitionId, out var definition) ||
            definition.aUpdateCommands == null || Array.FindIndex(definition.aUpdateCommands, c => c.StartsWith("Explosion,", StringComparison.Ordinal)) < 0)
            throw new InvalidOperationException("Not a native explosion definition: " + definitionId);
        var co = DataHandler.GetCondOwner(definitionId) ?? throw new InvalidOperationException("Explosion definition unavailable: " + definitionId);
        co.tf.position = position;
        ship.AddCO(co, bTiles: true);
        return co;
    }
}
