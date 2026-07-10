namespace PokeBinder.Infrastructure.Cards.Import;

public class SetJsonDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public int PrintedTotal { get; set; }
    public int Total { get; set; }
    public Dictionary<string, string> Legalities { get; set; } = new();
    public string? PtcgoCode { get; set; }
    public string ReleaseDate { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public SetImagesJsonDto? Images { get; set; }
}

public class SetImagesJsonDto
{
    public string? Symbol { get; set; }
    public string? Logo { get; set; }
}

public class CardJsonDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Supertype { get; set; } = string.Empty;
    public List<string> Subtypes { get; set; } = new();
    public string? Level { get; set; }
    public string? Hp { get; set; }
    public List<string> Types { get; set; } = new();
    public string? EvolvesFrom { get; set; }
    public List<AbilityJsonDto> Abilities { get; set; } = new();
    public List<AttackJsonDto> Attacks { get; set; } = new();
    public List<TypeEffectJsonDto> Weaknesses { get; set; } = new();
    public List<TypeEffectJsonDto> Resistances { get; set; } = new();
    public List<string> RetreatCost { get; set; } = new();
    public int? ConvertedRetreatCost { get; set; }
    public string Number { get; set; } = string.Empty;
    public string? Artist { get; set; }
    public string? Rarity { get; set; }
    public string? FlavorText { get; set; }
    public List<int> NationalPokedexNumbers { get; set; } = new();
    public Dictionary<string, string> Legalities { get; set; } = new();
    public string? RegulationMark { get; set; }
    public CardImagesJsonDto? Images { get; set; }
}

public class CardImagesJsonDto
{
    public string? Small { get; set; }
    public string? Large { get; set; }
}

public class AbilityJsonDto
{
    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class AttackJsonDto
{
    public string Name { get; set; } = string.Empty;
    public List<string> Cost { get; set; } = new();
    public int ConvertedEnergyCost { get; set; }
    public string? Damage { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class TypeEffectJsonDto
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
