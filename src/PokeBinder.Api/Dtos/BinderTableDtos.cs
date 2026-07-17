namespace PokeBinder.Api.Dtos;

public record BinderCardRowDto(
    Guid SlotId,
    int PageNumber,
    int Position,
    string CardId,
    string CardName,
    string SetId,
    string SetName,
    string Number,
    int? ReleaseYear,
    bool Owned,
    Guid? TagId,
    string? TagName,
    string? TagColourHex);
