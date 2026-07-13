namespace PokeBinder.Core.Cards;

public class Card
{
    public string Id { get; set; } = string.Empty;
    public string SetId { get; set; } = string.Empty;
    public Set? Set { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Supertype { get; set; } = string.Empty;
    public IReadOnlyList<string> Subtypes { get; set; } = Array.Empty<string>();
    public string? Level { get; set; }
    public string? Hp { get; set; }

    /// <summary>Hp parsed to an int at import time (null if unparseable), so range filters can use a real indexed column instead of casting the string Hp at query time.</summary>
    public int? HpValue { get; set; }
    public IReadOnlyList<string> Types { get; set; } = Array.Empty<string>();
    public string? EvolvesFrom { get; set; }
    public IReadOnlyList<Ability> Abilities { get; set; } = Array.Empty<Ability>();
    public IReadOnlyList<Attack> Attacks { get; set; } = Array.Empty<Attack>();
    public IReadOnlyList<TypeEffect> Weaknesses { get; set; } = Array.Empty<TypeEffect>();
    public IReadOnlyList<TypeEffect> Resistances { get; set; } = Array.Empty<TypeEffect>();
    public IReadOnlyList<string> RetreatCost { get; set; } = Array.Empty<string>();
    public int? ConvertedRetreatCost { get; set; }

    /// <summary>Raw card number exactly as printed in the source data, e.g. "4", "TG12", "28a".</summary>
    public string Number { get; set; } = string.Empty;

    // Sort key computed at import time from Number. See NumberSortKeyCalculator.
    public byte NumberSortGroup { get; set; }
    public string NumberSortPrefix { get; set; } = string.Empty;
    public int NumberSortValue { get; set; }
    public string NumberSortSuffix { get; set; } = string.Empty;

    public string? Artist { get; set; }
    public string? Rarity { get; set; }
    public string? FlavorText { get; set; }
    public string? RegulationMark { get; set; }
    public IReadOnlyDictionary<string, string> Legalities { get; set; } = new Dictionary<string, string>();

    public string? ImageSmallUrl { get; set; }
    public string? ImageLargeUrl { get; set; }

    public DataOrigin Origin { get; set; } = DataOrigin.Synced;

    public ICollection<CardPokedexNumber> PokedexNumbers { get; set; } = new List<CardPokedexNumber>();
    public ICollection<CardVariant> Variants { get; set; } = new List<CardVariant>();
    public ICollection<CardType> TypeRows { get; set; } = new List<CardType>();
    public ICollection<CardSubtype> SubtypeRows { get; set; } = new List<CardSubtype>();
    public ICollection<CardWeaknessType> WeaknessTypeRows { get; set; } = new List<CardWeaknessType>();
    public ICollection<CardResistanceType> ResistanceTypeRows { get; set; } = new List<CardResistanceType>();
}
