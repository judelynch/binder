using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Cards;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Cards;
using PokeBinder.Core.Identity;
using PokeBinder.Infrastructure;
using PokeBinder.Infrastructure.Cards.Import;

namespace PokeBinder.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController : ControllerBase
{
    private readonly PokeBinderDbContext _db;
    private readonly CardDataImporter _importer;
    private readonly IServiceScopeFactory _scopeFactory;

    public AdminController(PokeBinderDbContext db, CardDataImporter importer, IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _importer = importer;
        _scopeFactory = scopeFactory;
    }

    // ---- Data sync ----

    [HttpPost("sync/dry-run")]
    public async Task<ActionResult<CardImportSummary>> DryRunSync(CancellationToken ct)
    {
        var summary = await _importer.RunAsync(dryRun: true, ct: ct);
        return Ok(summary);
    }

    [HttpPost("sync/apply")]
    public async Task<ActionResult<SyncJobStartedDto>> ApplySync(ApplySyncRequest request, CancellationToken ct)
    {
        var run = new SyncRun
        {
            Id = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow,
            RunByUserId = this.GetUserId(),
            RunByEmail = this.GetEmail(),
            Status = SyncRunStatus.Running,
        };
        _db.SyncRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        var confirmedCardIds = new HashSet<string>(request.ConfirmedOverrideCardIds ?? Array.Empty<string>());
        var confirmedSetIds = new HashSet<string>(request.ConfirmedOverrideSetIds ?? Array.Empty<string>());
        var runId = run.Id;

        _ = Task.Run(() => RunApplyJobAsync(runId, confirmedCardIds, confirmedSetIds));

        return Accepted(new SyncJobStartedDto(run.Id));
    }

    private async Task RunApplyJobAsync(Guid runId, HashSet<string> confirmedCardIds, HashSet<string> confirmedSetIds)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PokeBinderDbContext>();
        var importer = scope.ServiceProvider.GetRequiredService<CardDataImporter>();

        var run = await db.SyncRuns.FindAsync(runId);
        if (run is null)
        {
            return;
        }

        try
        {
            void Progress(SyncProgress p)
            {
                run.SetsProcessed = p.SetsProcessed;
                run.TotalSets = p.TotalSets;
                run.CardsProcessed = p.CardsProcessed;
                db.SaveChanges();
            }

            var summary = await importer.RunAsync(
                dryRun: false,
                confirmedOverrideCardIds: confirmedCardIds,
                confirmedOverrideSetIds: confirmedSetIds,
                progress: Progress);

            run.Status = SyncRunStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            run.SetsAdded = summary.SetsAdded;
            run.SetsUpdated = summary.SetsUpdated;
            run.CardsAdded = summary.CardsAdded;
            run.CardsUpdated = summary.CardsUpdated;
            run.ChangedFieldCounts = summary.ChangedFieldCounts;
            run.RemainingManualConflicts = summary.ManualConflicts;
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            run.Status = SyncRunStatus.Failed;
            run.CompletedAt = DateTime.UtcNow;
            run.ErrorMessage = ex.Message;
            await db.SaveChangesAsync();
        }
    }

    [HttpGet("sync/jobs/{id:guid}")]
    public async Task<ActionResult<SyncRunDto>> GetSyncJob(Guid id, CancellationToken ct)
    {
        var run = await _db.SyncRuns.FindAsync(new object[] { id }, ct);
        return run is null ? NotFound() : Ok(ToDto(run));
    }

    [HttpGet("sync/history")]
    public async Task<ActionResult<PagedResult<SyncRunDto>>> GetSyncHistory([FromQuery] int page, [FromQuery] int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0 ? 20 : Math.Clamp(pageSize, 1, 100);

        var query = _db.SyncRuns.Where(r => r.Status != SyncRunStatus.Running).OrderByDescending(r => r.StartedAt);
        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Ok(new PagedResult<SyncRunDto>(items.Select(ToDto).ToList(), page, pageSize, totalCount));
    }

    private static SyncRunDto ToDto(SyncRun r) => new(
        r.Id, r.StartedAt, r.CompletedAt, r.RunByEmail, r.Status.ToString(),
        r.SetsProcessed, r.TotalSets, r.CardsProcessed,
        r.SetsAdded, r.SetsUpdated, r.CardsAdded, r.CardsUpdated,
        r.ChangedFieldCounts, r.RemainingManualConflicts, r.ErrorMessage);

    // ---- Card / set management ----

    [HttpPut("cards/{id}")]
    public async Task<ActionResult<CardDetailDto>> UpdateCard(string id, UpdateCardRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.AuditNote))
        {
            return BadRequest("An audit note is required.");
        }

        var card = await _db.Cards
            .Include(c => c.PokedexNumbers)
            .Include(c => c.Variants).ThenInclude(v => v.VariantType)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (card is null)
        {
            return NotFound();
        }

        var entry = _db.Entry(card);
        if (request.Name is not null) card.Name = request.Name;
        if (request.Rarity is not null) card.Rarity = request.Rarity;
        if (request.Artist is not null) card.Artist = request.Artist;
        if (request.FlavorText is not null) card.FlavorText = request.FlavorText;
        if (request.RegulationMark is not null) card.RegulationMark = request.RegulationMark;
        if (request.ImageSmallUrl is not null) card.ImageSmallUrl = request.ImageSmallUrl;
        if (request.ImageLargeUrl is not null) card.ImageLargeUrl = request.ImageLargeUrl;

        // Properties.IsModified only reflects the latest DetectChanges() pass; entry was captured
        // before the mutations above, so force one now. Cheap here — a single request-scoped
        // context with a handful of tracked entities, not a hot loop over thousands of rows.
        _db.ChangeTracker.DetectChanges();
        var changedFields = entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).ToList();
        if (changedFields.Count == 0)
        {
            return BadRequest("No changes were made.");
        }

        _db.CardEditAudits.Add(new CardEditAudit
        {
            Id = Guid.NewGuid(),
            CardId = card.Id,
            EditedByUserId = this.GetUserId(),
            EditedByEmail = this.GetEmail(),
            EditedAt = DateTime.UtcNow,
            Note = request.AuditNote,
            ChangedFields = changedFields,
        });
        await _db.SaveChangesAsync(ct);

        var dto = new CardDetailDto(
            card.Id, card.SetId, card.Name, card.Supertype, card.Subtypes, card.Level, card.Hp,
            card.Types, card.EvolvesFrom, card.Number, card.Artist, card.Rarity, card.FlavorText,
            card.RegulationMark,
            card.PokedexNumbers.Select(p => p.Number).OrderBy(n => n).ToList(),
            card.ImageSmallUrl, card.ImageLargeUrl,
            card.Variants.Select(v => v.VariantType!.Name).OrderBy(n => n).ToList());
        return Ok(dto);
    }

    [HttpGet("cards/{id}/audit")]
    public async Task<ActionResult<IReadOnlyList<CardEditAuditDto>>> GetCardAudit(string id, CancellationToken ct)
    {
        var audits = await _db.CardEditAudits
            .Where(a => a.CardId == id)
            .OrderByDescending(a => a.EditedAt)
            .Select(a => new CardEditAuditDto(a.Id, a.EditedByEmail, a.EditedAt, a.Note, a.ChangedFields))
            .ToListAsync(ct);
        return Ok(audits);
    }

    [HttpPost("sets")]
    public async Task<ActionResult<SetSummaryDto>> CreateSet(CreateSetRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Id and name are required.");
        }

        if (await _db.Sets.AnyAsync(s => s.Id == request.Id, ct))
        {
            return Conflict("A set with this id already exists.");
        }

        var set = new Set
        {
            Id = request.Id,
            Name = request.Name,
            Series = request.Series,
            PrintedTotal = request.PrintedTotal,
            Total = request.Total,
            ReleaseDate = request.ReleaseDate,
            UpdatedAt = DateTime.UtcNow,
            PtcgoCode = request.PtcgoCode,
            SymbolImageUrl = request.SymbolImageUrl,
            LogoImageUrl = request.LogoImageUrl,
            Legalities = new Dictionary<string, string>(),
            Origin = DataOrigin.Manual,
        };
        _db.Sets.Add(set);
        await _db.SaveChangesAsync(ct);

        return Ok(new SetSummaryDto(set.Id, set.Name, set.Series, set.PrintedTotal, set.Total, set.ReleaseDate, set.PtcgoCode, set.SymbolImageUrl, set.LogoImageUrl));
    }

    [HttpPost("sets/{setId}/cards")]
    public async Task<ActionResult<CardSummaryDto>> CreateCard(string setId, CreateCardRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Number))
        {
            return BadRequest("Id, name and number are required.");
        }

        var setExists = await _db.Sets.AnyAsync(s => s.Id == setId, ct);
        if (!setExists)
        {
            return NotFound("Set not found.");
        }

        if (await _db.Cards.AnyAsync(c => c.Id == request.Id, ct))
        {
            return Conflict("A card with this id already exists.");
        }

        var sortKey = NumberSortKeyCalculator.Compute(request.Number);
        var types = request.Types ?? Array.Empty<string>();
        var subtypes = request.Subtypes ?? Array.Empty<string>();

        var card = new Card
        {
            Id = request.Id,
            SetId = setId,
            Name = request.Name,
            Supertype = request.Supertype,
            Number = request.Number,
            Rarity = request.Rarity,
            Hp = request.Hp,
            HpValue = int.TryParse(request.Hp, out var hp) ? hp : null,
            Types = types,
            Subtypes = subtypes,
            Artist = request.Artist,
            ImageSmallUrl = request.ImageSmallUrl,
            ImageLargeUrl = request.ImageLargeUrl,
            Legalities = new Dictionary<string, string>(),
            Origin = DataOrigin.Manual,
            NumberSortGroup = sortKey.Group,
            NumberSortPrefix = sortKey.Prefix,
            NumberSortValue = sortKey.Value,
            NumberSortSuffix = sortKey.Suffix,
        };
        _db.Cards.Add(card);

        foreach (var type in types)
        {
            _db.CardTypes.Add(new CardType { CardId = card.Id, Type = type });
        }
        foreach (var subtype in subtypes)
        {
            _db.CardSubtypes.Add(new CardSubtype { CardId = card.Id, Subtype = subtype });
        }

        var normalVariantTypeId = await _db.VariantTypes.Where(v => v.Name == "Normal").Select(v => v.Id).SingleAsync(ct);
        _db.CardVariants.Add(new CardVariant { Id = Guid.NewGuid(), CardId = card.Id, VariantTypeId = normalVariantTypeId });

        await _db.SaveChangesAsync(ct);

        return Ok(new CardSummaryDto(
            card.Id, card.SetId, card.Name, card.Number, card.Rarity, card.Supertype,
            card.ImageSmallUrl, card.ImageLargeUrl,
            new List<VariantSummaryDto> { new(Guid.Empty, "Normal") }));
    }

    // ---- Variant management ----

    [HttpGet("variant-types")]
    public async Task<ActionResult<IReadOnlyList<VariantTypeDto>>> GetVariantTypes(CancellationToken ct) =>
        Ok(await _db.VariantTypes.OrderBy(v => v.Name).Select(v => new VariantTypeDto(v.Id, v.Name)).ToListAsync(ct));

    [HttpPost("variant-types")]
    public async Task<ActionResult<VariantTypeDto>> CreateVariantType(CreateVariantTypeRequest request, CancellationToken ct)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required.");
        }
        if (await _db.VariantTypes.AnyAsync(v => v.Name == name, ct))
        {
            return Conflict("A variant type with this name already exists.");
        }

        var variantType = new VariantType { Id = Guid.NewGuid(), Name = name };
        _db.VariantTypes.Add(variantType);
        await _db.SaveChangesAsync(ct);
        return Ok(new VariantTypeDto(variantType.Id, variantType.Name));
    }

    [HttpPut("variant-types/{id:guid}")]
    public async Task<ActionResult<VariantTypeDto>> UpdateVariantType(Guid id, CreateVariantTypeRequest request, CancellationToken ct)
    {
        var variantType = await _db.VariantTypes.FindAsync(new object[] { id }, ct);
        if (variantType is null)
        {
            return NotFound();
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required.");
        }
        if (await _db.VariantTypes.AnyAsync(v => v.Id != id && v.Name == name, ct))
        {
            return Conflict("A variant type with this name already exists.");
        }

        variantType.Name = name;
        await _db.SaveChangesAsync(ct);
        return Ok(new VariantTypeDto(variantType.Id, variantType.Name));
    }

    [HttpDelete("variant-types/{id:guid}")]
    public async Task<IActionResult> DeleteVariantType(Guid id, CancellationToken ct)
    {
        var variantType = await _db.VariantTypes.FindAsync(new object[] { id }, ct);
        if (variantType is null)
        {
            return NotFound();
        }

        var slotUsageCount = await _db.BinderSlots.CountAsync(s => s.CardVariant != null && s.CardVariant.VariantTypeId == id, ct);
        if (slotUsageCount > 0)
        {
            return Conflict(new { message = $"Cannot delete '{variantType.Name}': referenced by {slotUsageCount} binder slot(s).", slotUsageCount });
        }

        _db.CardVariants.RemoveRange(_db.CardVariants.Where(v => v.VariantTypeId == id));
        _db.VariantTypes.Remove(variantType);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("cards/{cardId}/variants/{variantTypeId:guid}")]
    public async Task<IActionResult> AddCardVariant(string cardId, Guid variantTypeId, CancellationToken ct)
    {
        if (!await _db.Cards.AnyAsync(c => c.Id == cardId, ct))
        {
            return NotFound("Card not found.");
        }
        if (!await _db.VariantTypes.AnyAsync(v => v.Id == variantTypeId, ct))
        {
            return NotFound("Variant type not found.");
        }
        if (await _db.CardVariants.AnyAsync(v => v.CardId == cardId && v.VariantTypeId == variantTypeId, ct))
        {
            return NoContent();
        }

        _db.CardVariants.Add(new CardVariant { Id = Guid.NewGuid(), CardId = cardId, VariantTypeId = variantTypeId });
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("cards/{cardId}/variants/{variantTypeId:guid}")]
    public async Task<IActionResult> RemoveCardVariant(string cardId, Guid variantTypeId, CancellationToken ct)
    {
        var variant = await _db.CardVariants.FirstOrDefaultAsync(v => v.CardId == cardId && v.VariantTypeId == variantTypeId, ct);
        if (variant is null)
        {
            return NoContent();
        }

        var slotUsageCount = await _db.BinderSlots.CountAsync(s => s.CardVariantId == variant.Id, ct);
        if (slotUsageCount > 0)
        {
            return Conflict(new { message = $"Cannot remove: placed in {slotUsageCount} binder slot(s).", slotUsageCount });
        }

        _db.CardVariants.Remove(variant);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("variants/bulk-assign")]
    public async Task<ActionResult<BulkVariantAssignResultDto>> BulkAssignVariants(BulkVariantAssignRequest request, CancellationToken ct)
    {
        if (request.VariantTypeIds is null || request.VariantTypeIds.Length == 0)
        {
            return BadRequest("Select at least one variant type.");
        }

        var variantTypeIds = request.VariantTypeIds.Distinct().ToList();
        var validCount = await _db.VariantTypes.CountAsync(v => variantTypeIds.Contains(v.Id), ct);
        if (validCount != variantTypeIds.Count)
        {
            return BadRequest("One or more variant types were not found.");
        }

        var matchingCardIds = await _db.Cards.AsQueryable().ApplyFilters(request.Filter).Select(c => c.Id).ToListAsync(ct);

        var existingPairs = await _db.CardVariants
            .Where(v => matchingCardIds.Contains(v.CardId) && variantTypeIds.Contains(v.VariantTypeId))
            .Select(v => new { v.CardId, v.VariantTypeId })
            .ToListAsync(ct);
        var existingSet = existingPairs.Select(e => (e.CardId, e.VariantTypeId)).ToHashSet();

        int created = 0, skipped = 0;
        foreach (var cardId in matchingCardIds)
        {
            foreach (var variantTypeId in variantTypeIds)
            {
                if (existingSet.Contains((cardId, variantTypeId)))
                {
                    skipped++;
                    continue;
                }

                created++;
                if (!request.DryRun)
                {
                    _db.CardVariants.Add(new CardVariant { Id = Guid.NewGuid(), CardId = cardId, VariantTypeId = variantTypeId });
                }
            }
        }

        if (!request.DryRun)
        {
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new BulkVariantAssignResultDto(matchingCardIds.Count, created, skipped));
    }
}
