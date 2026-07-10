namespace PokeBinder.Core.Cards;

public record CardImportSummary(
    int SetsAdded,
    int SetsUpdated,
    int CardsAdded,
    int CardsUpdated,
    TimeSpan Elapsed);
