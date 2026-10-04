using System;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Health;

/// <summary>Why a carried person could not be put down at a point.</summary>
public enum PlacementFailure { None, NotDragging, NotAPerson, Dead, NotUnconscious, NoShip, Failed }

/// <summary>Puts down a person someone is dragging, at a named point of a piece of equipment (Framework 0.82.0, first
/// used by Phobos Medical's bed). The game drags a body by slotting it into the dragger's "drag" slot; its own
/// "Drop Corpse" releases it fully with <c>UnSlotItem("drag")</c>, <c>Ship.AddCO(body, true)</c> and the room at the
/// body's position. This does the same, after moving the body to the point and turning it like the equipment, as the
/// game's own teleport does for a sleeper. The person's own queued actions (a knocked-out person's loop) are left as
/// the game made them.</summary>
public static class PatientPlacement
{
    public const string DragSlot = "drag";

    /// <summary>The person in <paramref name="dragger"/>'s drag slot, or null.</summary>
    public static CondOwner? Dragged(CondOwner? dragger)
    {
        if (dragger == null || dragger.bDestroyed || dragger.compSlots == null || !dragger.HasCond("IsDragging")) return null;
        var slot = dragger.compSlots.GetSlot(DragSlot);
        if (slot?.aCOs == null) return null;
        foreach (var co in slot.aCOs)
            if (co != null && !co.bDestroyed && IsPerson(co)) return co;
        return null;
    }

    public static bool IsPerson(CondOwner co) => co.HasCond("IsHuman") || co.HasCond("IsRobot");

    /// <summary>Whether <paramref name="dragger"/> could put down the person they drag, living and unconscious.</summary>
    public static PlacementFailure CanPlace(CondOwner? dragger)
    {
        if (dragger?.ship == null) return PlacementFailure.NoShip;
        var person = Dragged(dragger);
        if (person == null) return PlacementFailure.NotDragging;
        if (person.HasCond("IsDead")) return PlacementFailure.Dead;
        if (!person.HasCond("Unconscious")) return PlacementFailure.NotUnconscious;
        return PlacementFailure.None;
    }

    /// <summary>Releases the dragged person at <paramref name="point"/> of <paramref name="equipment"/>, on the
    /// dragger's ship. Returns the person, or null with the reason.</summary>
    public static CondOwner? Place(CondOwner dragger, CondOwner equipment, string point, out PlacementFailure failure)
    {
        failure = CanPlace(dragger);
        if (failure != PlacementFailure.None) return null;
        var ship = dragger.ship;
        if (equipment == null || equipment.bDestroyed || equipment.ship != ship) { failure = PlacementFailure.NoShip; return null; }
        var person = dragger.compSlots.UnSlotItem(DragSlot);
        if (person == null) { failure = PlacementFailure.Failed; return null; }
        try
        {
            Vector2 at = equipment.GetPos(point);
            person.tf.position = new Vector3(at.x, at.y, person.tf.position.z);
            person.tf.eulerAngles = equipment.tf.rotation.eulerAngles;
        }
        finally
        {
            // Re-register wherever the body ended up, as the game's Drop Corpse does, so it is never left unattached.
            ship.AddCO(person, bTiles: true);
            person.currentRoom = ship.GetRoomAtWorldCoords1(person.tf.position, bAllowDocked: true);
            person.Pathfinder?.ReacquireTILCurrent();
        }
        return person;
    }
}
