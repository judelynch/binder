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

    /// <summary>
    /// Runs the sync. When <paramref name="dryRun"/> is true, every reconciliation/change-detection
    /// step still runs against the tracked context exactly as normal — only the final persistence
    /// (SaveChangesAsync) is skipped for set/card data, so the returned summary is an exact preview
    /// of what an apply would do. Existing cards/sets marked <see cref="DataOrigin.Manual"/> are left
    /// untouched unless their id appears in <paramref name="confirmedOverrideCardIds"/> /
    /// <paramref name="confirmedOverrideSetIds"/>; otherwise any incoming change to them is reverted
    /// and reported as a manual conflict instead of applied.
    /// </summary>
    public async Task<CardImportSummary> RunAsync(
        bool dryRun = false,
        IReadOnlySet<string>? confirmedOverrideCardIds = null,
        IReadOnlySet<string>? confirmedOverrideSetIds = null,
        Action<SyncProgress>? progress = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var context = new SyncBuildContext
        {
            DryRun = dryRun,
            ConfirmedOverrideCardIds = confirmedOverrideCardIds ?? new HashSet<string>(),
            ConfirmedOverrideSetIds = confirmedOverrideSetIds ?? new HashSet<string>(),
            Progress = progress,
        };

        await SeedVariantTypesAsync(ct);

        var (setsAdded, setsUpdated) = await ImportSetsAsync(context, ct);
        var (cardsAdded, cardsUpdated) = await ImportCardsAsync(context, ct);

        stopwatch.Stop();

        var summary = new CardImportSummary
        {
            SetsAdded = setsAdded,
            SetsUpdated = setsUpdated,
            CardsAdded = cardsAdded,
            CardsUpdated = cardsUpdated,
            NewSets = context.NewSets,
            ChangedFieldCounts = context.FieldChangeCounts
                .Select(kv => new SyncFieldChange(kv.Key, kv.Value))
                .OrderByDescending(f => f.Count)
                .ToList(),
            ManualConflicts = context.ManualConflicts,
            Elapsed = stopwatch.Elapsed,
        };

        _logger.LogInformation(
            "Card data import complete (dryRun={DryRun}): {SetsAdded} sets added, {SetsUpdated} sets updated, {CardsAdded} cards added, {CardsUpdated} cards updated, {ManualConflicts} manual conflicts, elapsed {Elapsed}.",
            dryRun, summary.SetsAdded, summary.SetsUpdated, summary.CardsAdded, summary.CardsUpdated, summary.ManualConflicts.Count, summary.Elapsed);

        return summary;
    }

    private class SyncBuildContext
    {
        public bool DryRun { get; init; }
        public IReadOnlySet<string> ConfirmedOverrideCardIds { get; init; } = new HashSet<string>();
        public IReadOnlySet<string> ConfirmedOverrideSetIds { get; init; } = new HashSet<string>();

        // A plain synchronous delegate, not IProgress<T>: IProgress<T>.Report marshals to the
        // ThreadPool when no SynchronizationContext is captured (true for background ASP.NET Core
        // work), which would call back on a different thread than the one driving this DbContext.
        public Action<SyncProgress>? Progress { get; init; }

        public Dictionary<string, int> FieldChangeCounts { get; } = new();
        public List<SyncManualConflict> ManualConflicts { get; } = new();
        public List<SyncSetSummary> NewSets { get; } = new();
    }

    private async Task SeedVariantTypesAsync(CancellationToken ct)
    {
        // Reference-data seeding always persists, even during a dry run: it's fixed lookup data,
        // not part of the sync diff, and later queries in this same run need it to actually exist.
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

    private async Task<(int Added, int Updated)> ImportSetsAsync(SyncBuildContext context, CancellationToken ct)
    {
        var json = await _source.ReadSetsJsonAsync(ct);
        var dtos = JsonSerializer.Deserialize<List<SetJsonDto>>(json, JsonOptions) ?? new();

        var existing = await _db.Sets.ToDictionaryAsync(s => s.Id, ct);

        int added = 0, updated = 0;

        foreach (var dto in dtos)
        {
            if (!existing.TryGetValue(dto.Id, out var set))
            {
                set = new Set { Id = dto.Id, Origin = DataOrigin.Synced };
                _db.Sets.Add(set);
                MapSet(dto, set);
                added++;
                context.NewSets.Add(new SyncSetSummary(set.Id, set.Name));
                continue;
            }

            var entry = _db.Entry(set);
            var wasManual = set.Origin == DataOrigin.Manual;
            var confirmed = context.ConfirmedOverrideSetIds.Contains(set.Id);

            var before = SnapshotSet(set);
            MapSet(dto, set);
            var changedFields = DiffSet(before, set);

            if (changedFields.Count == 0)
            {
                continue;
            }

            if (wasManual && !confirmed)
            {
                entry.CurrentValues.SetValues(entry.OriginalValues);
                entry.State = EntityState.Unchanged;
                context.ManualConflicts.Add(new SyncManualConflict("Set", set.Id, set.Name, changedFields));
                continue;
            }

            updated++;
            foreach (var field in changedFields)
            {
                IncrementFieldCount(context.FieldChangeCounts, field);
            }
        }

        if (!context.DryRun)
        {
            await _db.SaveChangesAsync(ct);
        }

        return (added, updated);
    }

    private async Task<(int Added, int Updated)> ImportCardsAsync(SyncBuildContext context, CancellationToken ct)
    {
        var setIds = await _source.GetCardSetIdsAsync(ct);
        int added = 0, updated = 0, cardsProcessed = 0, setsProcessed = 0;

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
                setsProcessed++;
                continue;
            }

            var cardIds = dtos.Select(d => d.Id).ToList();
            var existingCards = await _db.Cards
                .Include(c => c.PokedexNumbers)
                .Include(c => c.TypeRows)
                .Include(c => c.SubtypeRows)
                .Include(c => c.WeaknessTypeRows)
                .Include(c => c.ResistanceTypeRows)
                .Where(c => cardIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, ct);

            var existingVariantCardIds = await _db.CardVariants
                .Where(v => cardIds.Contains(v.CardId) && v.VariantTypeId == normalVariantTypeId)
                .Select(v => v.CardId)
                .ToListAsync(ct);
            var hasNormalVariant = new HashSet<string>(existingVariantCardIds);

            foreach (var dto in dtos)
            {
                EnsureNormalVariant(dto.Id, normalVariantTypeId, hasNormalVariant);

                if (!existingCards.TryGetValue(dto.Id, out var card))
                {
                    card = new Card { Id = dto.Id, SetId = setId, Origin = DataOrigin.Synced };
                    _db.Cards.Add(card);
                    MapCard(dto, setId, card);
                    added++;

                    ReconcileJoinRows(card.TypeRows, card.Types, t => t.Type, v => new CardType { CardId = card.Id, Type = v });
                    ReconcileJoinRows(card.SubtypeRows, card.Subtypes, s => s.Subtype, v => new CardSubtype { CardId = card.Id, Subtype = v });
                    ReconcileJoinRows(card.WeaknessTypeRows, card.Weaknesses.Select(w => w.Type), w => w.Type, v => new CardWeaknessType { CardId = card.Id, Type = v });
                    ReconcileJoinRows(card.ResistanceTypeRows, card.Resistances.Select(r => r.Type), r => r.Type, v => new CardResistanceType { CardId = card.Id, Type = v });
                    ReconcilePokedexNumbers(card, dto.NationalPokedexNumbers);

                    continue;
                }

                var entry = _db.Entry(card);
                var wasManual = card.Origin == DataOrigin.Manual;
                var confirmed = context.ConfirmedOverrideCardIds.Contains(card.Id);

                var before = SnapshotCard(card);
                MapCard(dto, setId, card);
                var changedFields = DiffCard(before, card);

                if (JoinRowsWouldChange(card.TypeRows, dto.Types, t => t.Type)) changedFields.Add("Types");
                if (JoinRowsWouldChange(card.SubtypeRows, dto.Subtypes, s => s.Subtype)) changedFields.Add("Subtypes");
                if (JoinRowsWouldChange(card.WeaknessTypeRows, dto.Weaknesses.Select(w => w.Type), w => w.Type)) changedFields.Add("Weaknesses");
                if (JoinRowsWouldChange(card.ResistanceTypeRows, dto.Resistances.Select(r => r.Type), r => r.Type)) changedFields.Add("Resistances");
                if (!new HashSet<int>(dto.NationalPokedexNumbers).SetEquals(card.PokedexNumbers.Select(p => p.Number))) changedFields.Add("PokedexNumbers");

                if (changedFields.Count == 0)
                {
                    continue;
                }

                if (wasManual && !confirmed)
                {
                    entry.CurrentValues.SetValues(entry.OriginalValues);
                    entry.State = EntityState.Unchanged;
                    context.ManualConflicts.Add(new SyncManualConflict("Card", card.Id, card.Name, changedFields));
                    continue;
                }

                updated++;
                foreach (var field in changedFields)
                {
                    IncrementFieldCount(context.FieldChangeCounts, field);
                }

                ReconcileJoinRows(card.TypeRows, card.Types, t => t.Type, v => new CardType { CardId = card.Id, Type = v });
                ReconcileJoinRows(card.SubtypeRows, card.Subtypes, s => s.Subtype, v => new CardSubtype { CardId = card.Id, Subtype = v });
                ReconcileJoinRows(card.WeaknessTypeRows, card.Weaknesses.Select(w => w.Type), w => w.Type, v => new CardWeaknessType { CardId = card.Id, Type = v });
                ReconcileJoinRows(card.ResistanceTypeRows, card.Resistances.Select(r => r.Type), r => r.Type, v => new CardResistanceType { CardId = card.Id, Type = v });
                ReconcilePokedexNumbers(card, dto.NationalPokedexNumbers);
            }

            cardsProcessed += dtos.Count;
            setsProcessed++;

            if (!context.DryRun)
            {
                await _db.SaveChangesAsync(ct);
            }

            context.Progress?.Invoke(new SyncProgress(setsProcessed, setIds.Count, cardsProcessed));
        }

        return (added, updated);
    }

    private void EnsureNormalVariant(string cardId, Guid normalVariantTypeId, HashSet<string> hasNormalVariant)
    {
        if (hasNormalVariant.Contains(cardId))
        {
            return;
        }

        _db.CardVariants.Add(new CardVariant
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            VariantTypeId = normalVariantTypeId
        });
        hasNormalVariant.Add(cardId);
    }

    // Diffing is done by hand against a before/after snapshot rather than EF's change tracker.
    // EF's Entry(...).Properties.IsModified is only accurate right after a fresh DetectChanges()
    // pass, and calling that per-card during a dry run (which never flushes via SaveChangesAsync)
    // means the tracked-entity count keeps growing for the whole run — each of ~20,000 cards would
    // force a full O(n) rescan of an ever-larger graph, an O(n^2) cost that stalls for many minutes.
    // A plain field-by-field comparison is O(1) per card regardless of how much else is tracked.
    private readonly record struct SetSnapshot(
        string Name, string Series, int PrintedTotal, int Total, DateOnly ReleaseDate, DateTime UpdatedAt,
        string? PtcgoCode, string? SymbolImageUrl, string? LogoImageUrl, string LegalitiesJson);

    private static SetSnapshot SnapshotSet(Set set) => new(
        set.Name, set.Series, set.PrintedTotal, set.Total, set.ReleaseDate, set.UpdatedAt,
        set.PtcgoCode, set.SymbolImageUrl, set.LogoImageUrl, Json(set.Legalities));

    private static List<string> DiffSet(SetSnapshot before, Set after)
    {
        var changed = new List<string>();
        if (before.Name != after.Name) changed.Add(nameof(Set.Name));
        if (before.Series != after.Series) changed.Add(nameof(Set.Series));
        if (before.PrintedTotal != after.PrintedTotal) changed.Add(nameof(Set.PrintedTotal));
        if (before.Total != after.Total) changed.Add(nameof(Set.Total));
        if (before.ReleaseDate != after.ReleaseDate) changed.Add(nameof(Set.ReleaseDate));
        if (before.UpdatedAt != after.UpdatedAt) changed.Add(nameof(Set.UpdatedAt));
        if (before.PtcgoCode != after.PtcgoCode) changed.Add(nameof(Set.PtcgoCode));
        if (before.SymbolImageUrl != after.SymbolImageUrl) changed.Add(nameof(Set.SymbolImageUrl));
        if (before.LogoImageUrl != after.LogoImageUrl) changed.Add(nameof(Set.LogoImageUrl));
        if (before.LegalitiesJson != Json(after.Legalities)) changed.Add(nameof(Set.Legalities));
        return changed;
    }

    private readonly record struct CardSnapshot(
        string Name, string Supertype, string SubtypesJson, string? Level, string? Hp, int? HpValue,
        string TypesJson, string? EvolvesFrom, string AbilitiesJson, string AttacksJson,
        string WeaknessesJson, string ResistancesJson, string RetreatCostJson, int? ConvertedRetreatCost,
        string Number, string? Artist, string? Rarity, string? FlavorText, string? RegulationMark,
        string LegalitiesJson, string? ImageSmallUrl, string? ImageLargeUrl);

    private static CardSnapshot SnapshotCard(Card card) => new(
        card.Name, card.Supertype, Json(card.Subtypes), card.Level, card.Hp, card.HpValue,
        Json(card.Types), card.EvolvesFrom, Json(card.Abilities), Json(card.Attacks),
        Json(card.Weaknesses), Json(card.Resistances), Json(card.RetreatCost), card.ConvertedRetreatCost,
        card.Number, card.Artist, card.Rarity, card.FlavorText, card.RegulationMark,
        Json(card.Legalities), card.ImageSmallUrl, card.ImageLargeUrl);

    private static List<string> DiffCard(CardSnapshot before, Card after)
    {
        var changed = new List<string>();
        if (before.Name != after.Name) changed.Add(nameof(Card.Name));
        if (before.Supertype != after.Supertype) changed.Add(nameof(Card.Supertype));
        if (before.SubtypesJson != Json(after.Subtypes)) changed.Add(nameof(Card.Subtypes));
        if (before.Level != after.Level) changed.Add(nameof(Card.Level));
        if (before.Hp != after.Hp) changed.Add(nameof(Card.Hp));
        if (before.HpValue != after.HpValue) changed.Add(nameof(Card.HpValue));
        if (before.TypesJson != Json(after.Types)) changed.Add(nameof(Card.Types));
        if (before.EvolvesFrom != after.EvolvesFrom) changed.Add(nameof(Card.EvolvesFrom));
        if (before.AbilitiesJson != Json(after.Abilities)) changed.Add(nameof(Card.Abilities));
        if (before.AttacksJson != Json(after.Attacks)) changed.Add(nameof(Card.Attacks));
        if (before.WeaknessesJson != Json(after.Weaknesses)) changed.Add(nameof(Card.Weaknesses));
        if (before.ResistancesJson != Json(after.Resistances)) changed.Add(nameof(Card.Resistances));
        if (before.RetreatCostJson != Json(after.RetreatCost)) changed.Add(nameof(Card.RetreatCost));
        if (before.ConvertedRetreatCost != after.ConvertedRetreatCost) changed.Add(nameof(Card.ConvertedRetreatCost));
        if (before.Number != after.Number) changed.Add(nameof(Card.Number));
        if (before.Artist != after.Artist) changed.Add(nameof(Card.Artist));
        if (before.Rarity != after.Rarity) changed.Add(nameof(Card.Rarity));
        if (before.FlavorText != after.FlavorText) changed.Add(nameof(Card.FlavorText));
        if (before.RegulationMark != after.RegulationMark) changed.Add(nameof(Card.RegulationMark));
        if (before.LegalitiesJson != Json(after.Legalities)) changed.Add(nameof(Card.Legalities));
        if (before.ImageSmallUrl != after.ImageSmallUrl) changed.Add(nameof(Card.ImageSmallUrl));
        if (before.ImageLargeUrl != after.ImageLargeUrl) changed.Add(nameof(Card.ImageLargeUrl));
        return changed;
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null);

    private static void IncrementFieldCount(Dictionary<string, int> counts, string field) =>
        counts[field] = counts.GetValueOrDefault(field) + 1;

    private static bool JoinRowsWouldChange<TJoin>(ICollection<TJoin> current, IEnumerable<string> desiredValues, Func<TJoin, string> getValue)
    {
        var desired = new HashSet<string>(desiredValues);
        var currentValues = new HashSet<string>(current.Select(getValue));
        return !desired.SetEquals(currentValues);
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
        card.HpValue = int.TryParse(dto.Hp, out var hpValue) ? hpValue : null;
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

    private static void ReconcileJoinRows<TJoin>(
        ICollection<TJoin> current,
        IEnumerable<string> desiredValues,
        Func<TJoin, string> getValue,
        Func<string, TJoin> create)
    {
        var desired = new HashSet<string>(desiredValues);

        foreach (var existing in current.ToList())
        {
            if (!desired.Contains(getValue(existing)))
            {
                current.Remove(existing);
            }
        }

        var currentValues = new HashSet<string>(current.Select(getValue));
        foreach (var value in desired)
        {
            if (!currentValues.Contains(value))
            {
                current.Add(create(value));
            }
        }
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
