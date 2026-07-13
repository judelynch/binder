namespace PokeBinder.Core.Cards;

/// <summary>Distinguishes rows seeded/updated by the pokemon-tcg-data sync from ones an admin hand-created, so sync knows what it's allowed to overwrite.</summary>
public enum DataOrigin
{
    Synced = 0,
    Manual = 1,
}
