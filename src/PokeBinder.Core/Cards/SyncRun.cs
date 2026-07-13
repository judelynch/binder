namespace PokeBinder.Core.Cards;

public enum SyncRunStatus
{
    Running = 0,
    Completed = 1,
    Failed = 2,
}

/// <summary>
/// A single "apply sync" run. Doubles as both the job-status row a client polls for progress
/// and, once complete, a permanent history entry (when, who, what changed).
/// </summary>
public class SyncRun
{
    public Guid Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string RunByUserId { get; set; } = string.Empty;
    public string RunByEmail { get; set; } = string.Empty;
    public SyncRunStatus Status { get; set; } = SyncRunStatus.Running;

    public int SetsProcessed { get; set; }
    public int TotalSets { get; set; }
    public int CardsProcessed { get; set; }

    public int SetsAdded { get; set; }
    public int SetsUpdated { get; set; }
    public int CardsAdded { get; set; }
    public int CardsUpdated { get; set; }

    public IReadOnlyList<SyncFieldChange> ChangedFieldCounts { get; set; } = Array.Empty<SyncFieldChange>();
    public IReadOnlyList<SyncManualConflict> RemainingManualConflicts { get; set; } = Array.Empty<SyncManualConflict>();

    public string? ErrorMessage { get; set; }
}
