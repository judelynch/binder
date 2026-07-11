using PokeBinder.Core.Binders;
using Xunit;

namespace PokeBinder.Tests;

public class BulkAssignPlannerTests
{
    private static Guid[] Cards(int count) => Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();

    [Fact]
    public void Overwrite_AlwaysFillsSequentially()
    {
        var occupied = new[] { false, true, true, false, false };
        var cards = Cards(3);

        var plan = BulkAssignPlanner.Plan(occupied, startIndex: 0, cards, OccupiedStrategy.Overwrite);

        Assert.Equal(new[] { 0, 1, 2 }, plan.Placements.Select(p => p.SlotIndex));
        Assert.Equal(0, plan.SkippedOccupiedSlots);
    }

    [Fact]
    public void Skip_AdvancesPastOccupiedSlots()
    {
        var occupied = new[] { false, true, true, false, false };
        var cards = Cards(2);

        var plan = BulkAssignPlanner.Plan(occupied, startIndex: 0, cards, OccupiedStrategy.Skip);

        Assert.Equal(new[] { 0, 3 }, plan.Placements.Select(p => p.SlotIndex));
        Assert.Equal(2, plan.SkippedOccupiedSlots);
    }

    [Fact]
    public void Fail_ThrowsOnFirstOccupiedTargetAndPlansNothing()
    {
        var occupied = new[] { false, true, false };
        var cards = Cards(3);

        var ex = Assert.Throws<BulkAssignConflictException>(() =>
            BulkAssignPlanner.Plan(occupied, startIndex: 0, cards, OccupiedStrategy.Fail));

        Assert.Equal(1, ex.SlotIndex);
    }

    [Fact]
    public void Fail_SucceedsWhenNoTargetIsOccupied()
    {
        var occupied = new[] { false, false, false };
        var cards = Cards(3);

        var plan = BulkAssignPlanner.Plan(occupied, startIndex: 0, cards, OccupiedStrategy.Fail);

        Assert.Equal(new[] { 0, 1, 2 }, plan.Placements.Select(p => p.SlotIndex));
    }

    [Fact]
    public void Plan_RunsPastKnownSlotsIntoVirtualSlots()
    {
        // Only 2 real slots exist; requesting 4 placements walks into virtual
        // (not-yet-created) slot indices 2 and 3, which the caller is expected
        // to materialize as new pages before applying the plan.
        var occupied = new[] { false, false };
        var cards = Cards(4);

        var plan = BulkAssignPlanner.Plan(occupied, startIndex: 0, cards, OccupiedStrategy.Overwrite);

        Assert.Equal(new[] { 0, 1, 2, 3 }, plan.Placements.Select(p => p.SlotIndex));
    }

    [Fact]
    public void Skip_TreatsVirtualSlotsAsAlwaysEmpty()
    {
        var occupied = new[] { true, true };
        var cards = Cards(1);

        var plan = BulkAssignPlanner.Plan(occupied, startIndex: 0, cards, OccupiedStrategy.Skip);

        Assert.Equal(2, plan.Placements.Single().SlotIndex);
        Assert.Equal(2, plan.SkippedOccupiedSlots);
    }

    [Fact]
    public void StartIndex_PastEndOfKnownSlots_IsValid()
    {
        var occupied = Array.Empty<bool>();
        var cards = Cards(1);

        var plan = BulkAssignPlanner.Plan(occupied, startIndex: 0, cards, OccupiedStrategy.Overwrite);

        Assert.Equal(0, plan.Placements.Single().SlotIndex);
    }

    [Fact]
    public void StartIndex_OutOfRange_Throws()
    {
        var occupied = new[] { false, false };
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BulkAssignPlanner.Plan(occupied, startIndex: 3, Cards(1), OccupiedStrategy.Overwrite));
    }
}
