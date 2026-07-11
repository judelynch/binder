namespace PokeBinder.Core.Binders;

public enum OccupiedStrategy
{
    Skip,
    Overwrite,
    Fail
}

public record BulkAssignPlacement(int SlotIndex, Guid CardVariantId);

public record BulkAssignPlan(IReadOnlyList<BulkAssignPlacement> Placements, int SkippedOccupiedSlots);

public class BulkAssignConflictException : Exception
{
    public int SlotIndex { get; }

    public BulkAssignConflictException(int slotIndex)
        : base($"Slot at index {slotIndex} is already occupied.")
    {
        SlotIndex = slotIndex;
    }
}

/// <summary>
/// Pure planning for bulk-assign: walks forward from startIndex placing each card
/// variant into a slot per the occupied-slot strategy. Operates on plain occupancy
/// flags so it needs no database access — slot indices beyond the known/existing
/// range are treated as always-empty "virtual" slots representing pages that will
/// be created on demand by the caller once the plan succeeds.
/// </summary>
public static class BulkAssignPlanner
{
    public static BulkAssignPlan Plan(
        IReadOnlyList<bool> existingOccupied,
        int startIndex,
        IReadOnlyList<Guid> cardVariantIds,
        OccupiedStrategy strategy)
    {
        if (startIndex < 0 || startIndex > existingOccupied.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        var virtualOccupied = new Dictionary<int, bool>();
        var placements = new List<BulkAssignPlacement>();
        var cursor = startIndex;
        var skippedCount = 0;

        bool IsOccupied(int index) =>
            index < existingOccupied.Count ? existingOccupied[index] : virtualOccupied.GetValueOrDefault(index, false);

        foreach (var cardVariantId in cardVariantIds)
        {
            if (strategy == OccupiedStrategy.Skip)
            {
                while (IsOccupied(cursor))
                {
                    cursor++;
                    skippedCount++;
                }
            }
            else if (strategy == OccupiedStrategy.Fail && IsOccupied(cursor))
            {
                throw new BulkAssignConflictException(cursor);
            }

            placements.Add(new BulkAssignPlacement(cursor, cardVariantId));
            virtualOccupied[cursor] = true;
            cursor++;
        }

        return new BulkAssignPlan(placements, skippedCount);
    }
}
