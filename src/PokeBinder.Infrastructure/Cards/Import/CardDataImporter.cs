using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PokeBinder.Core.Cards;

namespace PokeBinder.Infrastructure.Cards.Import;

public class CardDataImporter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PokeBinderDbContext _db;
    private readonly ICardDataSource _source;
    private readonly ILogger<CardDataImporter> _logger;

    public CardDataImporter(PokeBinderDbContext db, ICardDataSource source, ILogger<CardDataImporter> logger)
    {
        _db = db;
        _source = source;
        _logger = logger;
    }

    public async Task<CardImportSummary> RunAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        await SeedVariantTypesAsync(ct);

        var (setsAdded, setsUpdated) = await ImportSetsAsync(ct);
        var (cardsAdded, cardsUpdated) = await ImportCardsAsync(ct);

        stopwatch.Stop();

        var summary = new CardImportSummary(setsAdded, setsUpdated, cardsAdded, cardsUpdated, stopwatch.Elapsed);
        _logger.LogInformation(
            "Card data import complete: {SetsAdded} sets added, {SetsUpdated} sets updated, {CardsAdded} cards added, {CardsUpdated} cards updated, elapsed {Elapsed}.",
            summary.SetsAdded, summary.SetsUpdated, summary.CardsAdded, summary.CardsUpdated, summary.Elapsed);

        return summary;
    }

    private async Task SeedVariantTypesAsync(CancellationToken ct)
    {
        var existingNames = await _db.VariantTypes.Select(v => v.Name).ToListAsync(ct);
        foreach (var name in VariantType.SeedNames)
        {
            if (!existingNames.Contains(name))
            {
                _db.VariantTypes.Add(new VariantType { Id = Guid.NewGuid(), Name = name });
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task<(int Added, int Updated)> ImportSetsAsync(CancellationToken ct)
    {
        var json = await _source.ReadSetsJsonAsync(ct);
        var dtos = JsonSerializer.Deserialize<List<SetJsonDto>>(json, JsonOptions) ?? new();

        var existing = await _db.Sets.ToDictionaryAsync(s => s.Id, ct);

        int added = 0, updated = 0;

        foreach (var dto in dtos)
        {
            if (!existing.TryGetValue(dto.Id, out var set))
            {
                set = new Set { Id = dto.Id };
                _db.Sets.Add(set);
                MapSet(dto, set);
                added++;
                continue;
            }

            MapSet(dto, set);
            if (_db.Entry(set).State == EntityState.Modified)
            {
                updated++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return (added, updated);
    }

    private async Task<(int Added, int Updated)> ImportCardsAsync(CancellationToken ct)
    {
        var setIds = await _source.GetCardSetIdsAsync(ct);
        int added = 0, updated = 0;

        var normalVariantTypeId = await _db.VariantTypes
            .Where(v => v.Name == "Normal")
            .Select(v => v.Id)
            .SingleAsync(ct);

        foreach (var setId in setIds)
        {
            var json = await _source.ReadCardsJsonAsync(setId, ct);
            var dtos = JsonSerializer.Deserialize<List<CardJsonDto>>(json, JsonOptions) ?? new();
            if (dtos.Count == 0)
            {
                continue;
            }

            var cardIds = dtos.Select(d => d.Id).ToList();
            var existingCards = await _db.Cards
                .Include(c => c.PokedexNumbers)
                .Where(c => cardIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, ct);

            var existingVariantCardIds = await _db.CardVariants
                .Where(v => cardIds.Contains(v.CardId) && v.VariantTypeId == normalVariantTypeId)
                .Select(v => v.CardId)
                .ToListAsync(ct);
            var hasNormalVariant = new HashSet<string>(existingVariantCardIds);

            foreach (var dto in dtos)
            {
                if (!existingCards.TryGetValue(dto.Id, out var card))
                {
                    card = new Card { Id = dto.Id, SetId = setId };
                    _db.Cards.Add(card);
                    MapCard(dto, setId, card);
                    added++;
                }
                else
                {
                    MapCard(dto, setId, card);
                    if (_db.Entry(card).State == EntityState.Modified)
                    {
                        updated++;
                    }
                }

                ReconcilePokedexNumbers(card, dto.NationalPokedexNumbers);

                if (!hasNormalVariant.Contains(dto.Id))
                {
                    _db.CardVariants.Add(new CardVariant
                    {
                        Id = Guid.NewGuid(),
                        CardId = dto.Id,
                        VariantTypeId = normalVariantTypeId
                    });
                    hasNormalVariant.Add(dto.Id);
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        return (added, updated);
    }

    private static void MapSet(SetJsonDto dto, Set set)
    {
        set.Name = dto.Name;
        set.Series = dto.Series;
        set.PrintedTotal = dto.PrintedTotal;
        set.Total = dto.Total;
        set.ReleaseDate = DateOnly.ParseExact(dto.ReleaseDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
        set.UpdatedAt = DateTime.ParseExact(dto.UpdatedAt, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
        set.PtcgoCode = dto.PtcgoCode;
        set.SymbolImageUrl = dto.Images?.Symbol;
        set.LogoImageUrl = dto.Images?.Logo;
        set.Legalities = dto.Legalities;
    }

    private static void MapCard(CardJsonDto dto, string setId, Card card)
    {
        card.SetId = setId;
        card.Name = dto.Name;
        card.Supertype = dto.Supertype;
        card.Subtypes = dto.Subtypes;
        card.Level = dto.Level;
        card.Hp = dto.Hp;
        card.Types = dto.Types;
        card.EvolvesFrom = dto.EvolvesFrom;
        card.Abilities = dto.Abilities.Select(a => new Ability(a.Name, a.Text, a.Type)).ToList();
        card.Attacks = dto.Attacks.Select(a => new Attack(a.Name, a.Cost, a.ConvertedEnergyCost, a.Damage, a.Text)).ToList();
        card.Weaknesses = dto.Weaknesses.Select(w => new TypeEffect(w.Type, w.Value)).ToList();
        card.Resistances = dto.Resistances.Select(r => new TypeEffect(r.Type, r.Value)).ToList();
        card.RetreatCost = dto.RetreatCost;
        card.ConvertedRetreatCost = dto.ConvertedRetreatCost;
        card.Number = dto.Number;

        var sortKey = NumberSortKeyCalculator.Compute(dto.Number);
        card.NumberSortGroup = sortKey.Group;
        card.NumberSortPrefix = sortKey.Prefix;
        card.NumberSortValue = sortKey.Value;
        card.NumberSortSuffix = sortKey.Suffix;

        card.Artist = dto.Artist;
        card.Rarity = dto.Rarity;
        card.FlavorText = dto.FlavorText;
        card.RegulationMark = dto.RegulationMark;
        card.Legalities = dto.Legalities;
        card.ImageSmallUrl = dto.Images?.Small;
        card.ImageLargeUrl = dto.Images?.Large;
    }

    private static void ReconcilePokedexNumbers(Card card, List<int> numbers)
    {
        var desired = new HashSet<int>(numbers);
        var current = card.PokedexNumbers.ToList();

        foreach (var existing in current)
        {
            if (!desired.Contains(existing.Number))
            {
                card.PokedexNumbers.Remove(existing);
            }
        }

        var currentNumbers = new HashSet<int>(card.PokedexNumbers.Select(p => p.Number));
        foreach (var number in desired)
        {
            if (!currentNumbers.Contains(number))
            {
                card.PokedexNumbers.Add(new CardPokedexNumber { CardId = card.Id, Number = number });
            }
        }
    }
}
