namespace PokeBinder.Api.Dtos;

public class CardSearchRequest
{
    public string? Name { get; set; }
    public string? Supertype { get; set; }
    public string[]? Subtypes { get; set; }
    public string[]? Types { get; set; }
    public string[]? SetIds { get; set; }
    public string[]? Series { get; set; }
    public string[]? Rarities { get; set; }
    public int? HpMin { get; set; }
    public int? HpMax { get; set; }
    public string? WeaknessType { get; set; }
    public string? ResistanceType { get; set; }
    public int? RetreatCostMin { get; set; }
    public int? RetreatCostMax { get; set; }
    public string? Artist { get; set; }
    public string[]? RegulationMarks { get; set; }
    public int? NationalPokedexNumber { get; set; }
    public string[]? VariantTypes { get; set; }

    /// <summary>setNumber (default) | name | releaseDate | rarity</summary>
    public string Sort { get; set; } = "setNumber";

    /// <summary>Null = each sort field's natural default direction (name asc, everything else desc). Set explicitly to override.</summary>
    public bool? SortDescending { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
