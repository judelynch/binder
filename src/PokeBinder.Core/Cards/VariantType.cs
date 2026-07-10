namespace PokeBinder.Core.Cards;

public class VariantType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public static readonly string[] SeedNames =
    {
        "Normal", "Reverse Holo", "Holo", "1st Edition", "Shadowless", "Promo Stamp"
    };
}
