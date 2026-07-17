namespace PokeBinder.Core.Pricing;

/// <summary>
/// Per-CardVariant scheduling state driving the stalest-first scrape order. ScrapePriority is a
/// pure manual-jump mechanism ("Scrape this card now" bumps it) - it does not encode anything
/// derived (owned vs missing, binder count, etc.); staleness alone drives ordering otherwise.
/// </summary>
public class CardVariantScrapeState
{
    public Guid CardVariantId { get; set; }
    public DateTime? LastScrapedAt { get; set; }
    public int ScrapePriority { get; set; }
}
