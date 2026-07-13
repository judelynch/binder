namespace PokeBinder.Core.Cards;

public record SyncSetSummary(string SetId, string Name);

public record SyncFieldChange(string Field, int Count);

public record SyncManualConflict(string EntityType, string EntityId, string Name, IReadOnlyList<string> ChangedFields);

/// <summary>Reported periodically during a run so a caller can surface live progress (e.g. for polling from a background job).</summary>
public record SyncProgress(int SetsProcessed, int TotalSets, int CardsProcessed);

public class CardImportSummary
{
    public int SetsAdded { get; set; }
    public int SetsUpdated { get; set; }
    public int CardsAdded { get; set; }
    public int CardsUpdated { get; set; }
    public List<SyncSetSummary> NewSets { get; set; } = new();
    public List<SyncFieldChange> ChangedFieldCounts { get; set; } = new();
    public List<SyncManualConflict> ManualConflicts { get; set; } = new();
    public TimeSpan Elapsed { get; set; }
}
