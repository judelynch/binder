namespace PokeBinder.Core.Pricing;

public class PricingScrapeOptions
{
    public int BatchSize { get; set; } = 10;
    public int BatchPauseSeconds { get; set; } = 120;
    public int RequestDelaySeconds { get; set; } = 15;
    public int RequestJitterMaxSeconds { get; set; } = 10;
    public int MaxCardsPerRun { get; set; } = 100;
    public int FreshnessHours { get; set; } = 24;
    public int StaleLockTimeoutMinutes { get; set; } = 120;
}
