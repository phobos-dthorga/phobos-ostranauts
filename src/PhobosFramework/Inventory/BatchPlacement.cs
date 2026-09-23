using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Inventory;

public readonly struct ItemSize
{
    public readonly int Width, Height;
    public ItemSize(int width, int height) { Width = width; Height = height; }
}

public readonly struct Position
{
    public readonly int X, Y;
    public Position(int x, int y) { X = x; Y = y; }
}

public static class BatchPlacement
{
    // Reserve every new item's cells in a private copy. Never merge existing stacks.
    public static Position[]? Plan(bool[,] occupied, IReadOnlyList<ItemSize> sizes)
    {
        var cells = (bool[,])occupied.Clone();
        int width = cells.GetLength(0), height = cells.GetLength(1);
        var result = new Position[sizes.Count];
        for (int i = 0; i < sizes.Count; i++)
        {
            var size = sizes[i];
            if (size.Width <= 0 || size.Height <= 0) return null;
            bool placed = false;
            for (int y = 0; y <= height - size.Height && !placed; y++)
            for (int x = 0; x <= width - size.Width && !placed; x++)
            {
                bool free = true;
                for (int dy = 0; dy < size.Height; dy++)
                for (int dx = 0; dx < size.Width; dx++) free &= !cells[x + dx, y + dy];
                if (!free) continue;
                result[i] = new Position(x, y);
                for (int dy = 0; dy < size.Height; dy++)
                for (int dx = 0; dx < size.Width; dx++) cells[x + dx, y + dy] = true;
                placed = true;
            }
            if (!placed) return null;
        }
        return result;
    }
}

public interface IBatchDelivery
{
    bool Prepare();
    void PlaceProducts();
    void ConsumeInput();
    bool InputConsumed { get; }
    void RollbackProducts();
}

public enum DeliveryResult { Blocked, Completed }

public static class BatchDelivery
{
    public static DeliveryResult Commit(IBatchDelivery delivery)
    {
        if (delivery.InputConsumed) return DeliveryResult.Completed;
        try
        {
            if (!delivery.Prepare())
            { delivery.RollbackProducts(); return DeliveryResult.Blocked; }
            delivery.PlaceProducts();
            delivery.ConsumeInput();
            if (!delivery.InputConsumed) throw new InvalidOperationException("Input was not consumed");
            return DeliveryResult.Completed;
        }
        catch
        {
            // Once destruction took effect, products are the only surviving material.
            // The caller still receives the exception and must pause, not retry blindly.
            if (!delivery.InputConsumed) delivery.RollbackProducts();
            throw;
        }
    }
}
