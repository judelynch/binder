namespace PokeBinder.Core.Pricing;

public enum ScrapeRunStatus
{
    Running,
    Completed,
    Failed,
}

public enum ScrapeTrigger
{
    Nightly,
    LoginCatchUp,
    Manual,
}

/// <summary>
/// A single pricing-pipeline run - job-status row while Running, permanent history entry once
/// Completed/Failed. Modeled directly on Cards.SyncRun's pattern.
/// </summary>
public class ScrapeRun
{
    public Guid Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ScrapeRunStatus Status { get; set; } = ScrapeRunStatus.Running;
    public ScrapeTrigger TriggeredBy { get; set; }
    public string? TriggeredByUserId { get; set; }

    public int CardsProcessed { get; set; }
    public int ListingsFound { get; set; }
    public int ListingsAccepted { get; set; }
    public int ListingsQuarantined { get; set; }
    public int ListingsRejected { get; set; }

    public string? ErrorMessage { get; set; }
}
