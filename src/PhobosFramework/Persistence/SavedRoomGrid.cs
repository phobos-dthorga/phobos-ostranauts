using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Persistence;

// Evidence for a room's saved flat indices, independently anchored by its
// serialized Compartment item's world position. Inputs are never rewritten.
internal sealed class SavedRoomShape
{
    internal readonly string Id;
    internal readonly bool Void;
    internal readonly int[] Tiles;
    internal readonly double X, Y;
    internal SavedRoomShape(string id, bool isVoid, int[] tiles, double x, double y)
    { Id = id; Void = isVoid; Tiles = tiles; X = x; Y = y; }
}

internal static class SavedRoomGrid
{
    private const double TileTolerance = 0.0001;
    private const int MinimumBorderedDimension = 3;

    // Native save trimming retains an exterior border and changes indices/origin,
    // but 1.0.1.5 writes dimensions before trimming. Accept only one smaller grid
    // whose entire exterior border AND every independent room anchor agree.
    internal static bool TryResolve(int headerColumns, int headerRows, double originX, double originY,
        IReadOnlyList<SavedRoomShape> rooms, IEnumerable<int> zoneTiles, out int columns, out int rows)
    {
        columns = rows = 0;
        long headerCount = (long)headerColumns * headerRows;
        if (headerColumns < MinimumBorderedDimension || headerRows < MinimumBorderedDimension ||
            headerCount > int.MaxValue || !Finite(originX) || !Finite(originY) || rooms == null || rooms.Count < 2) return false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var all = new HashSet<int>();
        var memberships = new List<HashSet<int>>();
        var anchors = new List<(int Column, int Row)>();
        HashSet<int>? exterior = null;
        int maximum = -1, enclosed = 0;
        foreach (var room in rooms)
        {
            if (room == null || string.IsNullOrEmpty(room.Id) || !ids.Add(room.Id) || room.Tiles == null || room.Tiles.Length == 0 ||
                !Offset(room.X - originX, out int x) || !Offset(originY - room.Y, out int y)) return false;
            var membership = new HashSet<int>();
            foreach (int tile in room.Tiles)
            {
                if (tile < 0 || tile >= headerCount) return false;
                // Native saves can repeat a tile within one room;
                // retain that serialization, but reject conflicting ownership.
                if (membership.Add(tile) && !all.Add(tile)) return false;
                maximum = Math.Max(maximum, tile);
            }
            if (room.Void && membership.Contains(0)) exterior = membership;
            if (!room.Void) enclosed++;
            memberships.Add(membership);
            anchors.Add((x, y));
        }
        if (exterior == null || enclosed == 0) return false;
        int count = maximum + 1;
        if (zoneTiles != null)
            foreach (int tile in zoneTiles) if (tile < 0 || tile >= count) return false;

        bool Matches(int width, int height)
        {
            if (width < MinimumBorderedDimension || height < MinimumBorderedDimension || width > headerColumns || height > headerRows) return false;
            // All four borders must belong to the SAME saved exterior room.
            for (int x = 0; x < width; x++)
                if (!exterior.Contains(x) || !exterior.Contains((height - 1) * width + x)) return false;
            for (int y = 0; y < height; y++)
                if (!exterior.Contains(y * width) || !exterior.Contains(y * width + width - 1)) return false;
            for (int i = 0; i < anchors.Count; i++)
            {
                var anchor = anchors[i];
                if (anchor.Column >= width || anchor.Row >= height ||
                    !memberships[i].Contains(anchor.Row * width + anchor.Column)) return false;
            }
            return true;
        }

        if (count == headerCount)
        {
            if (!Matches(headerColumns, headerRows)) return false;
            columns = headerColumns; rows = headerRows; return true;
        }
        int matches = 0, foundColumns = 0, foundRows = 0;
        // Enumerate factors, not an unbounded allocation based on the header.
        for (int divisor = MinimumBorderedDimension; (long)divisor * divisor <= count; divisor++)
        {
            if (count % divisor != 0) continue;
            int other = count / divisor;
            if (Matches(divisor, other)) { matches++; foundColumns = divisor; foundRows = other; }
            if (other != divisor && Matches(other, divisor)) { matches++; foundColumns = other; foundRows = divisor; }
        }
        if (matches != 1) return false;
        columns = foundColumns; rows = foundRows; return true;
    }

    private static bool Offset(double value, out int result)
    {
        result = 0;
        if (!Finite(value) || value < 0 || value > int.MaxValue || Math.Abs(value - Math.Round(value)) > TileTolerance) return false;
        result = (int)Math.Round(value); return true;
    }
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
