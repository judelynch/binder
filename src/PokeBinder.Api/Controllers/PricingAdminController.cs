using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Identity;
using PokeBinder.Core.Pricing;
using PokeBinder.Infrastructure;
using PokeBinder.Infrastructure.Pricing;

namespace PokeBinder.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/pricing")]
public class PricingAdminController : ControllerBase
{
    private readonly PokeBinderDbContext _db;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly PriceReaggregationService _reaggregation;

    public PricingAdminController(PokeBinderDbContext db, IBackgroundJobClient backgroundJobClient, PriceReaggregationService reaggregation)
    {
        _db = db;
        _backgroundJobClient = backgroundJobClient;
        _reaggregation = reaggregation;
    }

    /// <summary>Manual "Run now" - enqueues immediately; the orchestrator's own concurrency guard no-ops harmlessly if a run is already in flight.</summary>
    [HttpPost("run")]
    public IActionResult RunNow()
    {
        var userId = this.GetUserId();
        _backgroundJobClient.Enqueue<PricingScrapeOrchestrator>(o => o.RunAsync(ScrapeTrigger.Manual, userId, null, CancellationToken.None));
        return Accepted();
    }

    /// <summary>
    /// "Scrape this card now": without ?force=true, just bumps ScrapePriority so it's first in
    /// line on the next eligible run (still respects the 24h freshness rule). With force=true,
    /// bypasses freshness entirely and scrapes it immediately.
    /// </summary>
    [HttpPost("run/{cardVariantId:guid}")]
    public async Task<IActionResult> ScrapeCardNow(Guid cardVariantId, [FromQuery] bool force, CancellationToken ct)
    {
        var variantExists = await _db.CardVariants.AnyAsync(v => v.Id == cardVariantId, ct);
        if (!variantExists)
        {
            return NotFound();
        }

        if (force)
        {
            var userId = this.GetUserId();
            _backgroundJobClient.Enqueue<PricingScrapeOrchestrator>(o => o.RunAsync(ScrapeTrigger.Manual, userId, cardVariantId, CancellationToken.None));
            return Accepted();
        }

        var state = await _db.CardVariantScrapeStates.FindAsync(new object[] { cardVariantId }, ct);
        if (state is null)
        {
            state = new CardVariantScrapeState { CardVariantId = cardVariantId };
            _db.CardVariantScrapeStates.Add(state);
        }

        state.ScrapePriority += 1;
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    // ---- Review queue ----

    /// <summary>Quarantined listings awaiting manual review, oldest-classified first (a review backlog is worked in arrival order).</summary>
    [HttpGet("queue")]
    public async Task<ActionResult<PagedResult<QueuedListingDto>>> GetQueue([FromQuery] int page, [FromQuery] int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0 ? 20 : Math.Clamp(pageSize, 1, 100);

        var query = _db.ListingClassifications
            .Where(c => c.Status == ClassificationStatus.Quarantined)
            .OrderBy(c => c.ClassifiedAt);

        var totalCount = await query.CountAsync(ct);
        var classifications = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(c => c.RawListing)
            .ToListAsync(ct);

        var variantIds = classifications.Select(c => c.ResolvedCardVariantId).Distinct().ToList();
        var variantInfo = await _db.CardVariants
            .Where(v => variantIds.Contains(v.Id))
            .Include(v => v.Card).ThenInclude(c => c!.Set)
            .Include(v => v.VariantType)
            .ToDictionaryAsync(v => v.Id, ct);

        var items = classifications.Select(c =>
        {
            variantInfo.TryGetValue(c.ResolvedCardVariantId, out var variant);
            var card = variant?.Card;
            var setNumber = card is null ? string.Empty : $"{card.Number}/{card.Set?.PrintedTotal.ToString() ?? "?"}";

            return new QueuedListingDto(
                c.Id, c.RawListingId,
                c.RawListing?.Title ?? string.Empty,
                c.RawListing?.ItemPriceGbp ?? 0,
                c.RawListing?.PostagePriceGbp,
                c.RawListing?.SoldDate ?? default,
                c.RawListing?.ListingFormat.ToString() ?? string.Empty,
                c.RawListing?.ThumbnailUrl,
                c.ResolvedCardVariantId,
                card?.Name ?? "(unknown card)",
                setNumber,
                variant?.VariantType?.Name ?? "(unknown variant)",
                c.IdentityMatchStrong,
                c.GradedStatus.ToString(),
                c.Grader,
                c.Grade,
                c.RawCondition.ToString(),
                c.VariantMatch.ToString(),
                c.Language,
                c.BestOfferAccepted,
                c.KillReason,
                c.ConfidenceScore,
                c.Status.ToString(),
                c.ClassifiedAt);
        }).ToList();

        return Ok(new PagedResult<QueuedListingDto>(items, page, pageSize, totalCount));
    }

    /// <summary>Accepts the classifier's guess as-is: flips to AutoAccepted and re-aggregates.</summary>
    [HttpPost("queue/{classificationId:guid}/approve")]
    public async Task<IActionResult> ApproveListing(Guid classificationId, [FromBody] RejectRequest? request, CancellationToken ct)
    {
        var classification = await _db.ListingClassifications.FindAsync(new object[] { classificationId }, ct);
        if (classification is null)
        {
            return NotFound();
        }

        _db.ClassificationFeedbacks.Add(new ClassificationFeedback
        {
            Id = Guid.NewGuid(),
            ListingClassificationId = classification.Id,
            OriginalGuessJson = SnapshotJson(classification),
            CorrectedValuesJson = null,
            Action = FeedbackAction.Approved,
            Reason = request?.Reason,
            ReviewedByUserId = this.GetUserId(),
        });

        classification.Status = ClassificationStatus.AutoAccepted;
        await _db.SaveChangesAsync(ct);
        await _reaggregation.ReaggregateAsync(classification.ResolvedCardVariantId, ct);
        return Ok();
    }

    /// <summary>Overrides the classifier's grading/condition guess with a human correction, then accepts and re-aggregates.</summary>
    [HttpPost("queue/{classificationId:guid}/reclassify")]
    public async Task<IActionResult> ReclassifyListing(Guid classificationId, ReclassifyRequest request, CancellationToken ct)
    {
        var classification = await _db.ListingClassifications.FindAsync(new object[] { classificationId }, ct);
        if (classification is null)
        {
            return NotFound();
        }

        if (!Enum.TryParse<GradedStatus>(request.GradedStatus, out var gradedStatus) ||
            !Enum.TryParse<RawConditionClassification>(request.RawCondition, out var rawCondition))
        {
            return BadRequest("Invalid GradedStatus or RawCondition.");
        }

        var originalJson = SnapshotJson(classification);

        classification.GradedStatus = gradedStatus;
        classification.Grader = request.Grader;
        classification.Grade = request.Grade;
        classification.RawCondition = rawCondition;
        classification.Status = ClassificationStatus.AutoAccepted;

        _db.ClassificationFeedbacks.Add(new ClassificationFeedback
        {
            Id = Guid.NewGuid(),
            ListingClassificationId = classification.Id,
            OriginalGuessJson = originalJson,
            CorrectedValuesJson = SnapshotJson(classification),
            Action = FeedbackAction.Reclassified,
            Reason = request.Reason,
            ReviewedByUserId = this.GetUserId(),
        });

        await _db.SaveChangesAsync(ct);
        await _reaggregation.ReaggregateAsync(classification.ResolvedCardVariantId, ct);
        return Ok();
    }

    /// <summary>Rejects the listing outright: never counts toward any price.</summary>
    [HttpPost("queue/{classificationId:guid}/reject")]
    public async Task<IActionResult> RejectListing(Guid classificationId, [FromBody] RejectRequest? request, CancellationToken ct)
    {
        var classification = await _db.ListingClassifications.FindAsync(new object[] { classificationId }, ct);
        if (classification is null)
        {
            return NotFound();
        }

        _db.ClassificationFeedbacks.Add(new ClassificationFeedback
        {
            Id = Guid.NewGuid(),
            ListingClassificationId = classification.Id,
            OriginalGuessJson = SnapshotJson(classification),
            CorrectedValuesJson = null,
            Action = FeedbackAction.Rejected,
            Reason = request?.Reason,
            ReviewedByUserId = this.GetUserId(),
        });

        classification.Status = ClassificationStatus.Rejected;
        await _db.SaveChangesAsync(ct);
        await _reaggregation.ReaggregateAsync(classification.ResolvedCardVariantId, ct);
        return Ok();
    }

    /// <summary>Bulk Approve/Reject across a multi-select. Reclassify isn't offered in bulk since each listing needs its own corrected values.</summary>
    [HttpPost("queue/bulk")]
    public async Task<ActionResult<BulkClassificationActionResultDto>> BulkAction(BulkClassificationActionRequest request, CancellationToken ct)
    {
        if (!string.Equals(request.Action, "Approve", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Action, "Reject", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Action must be 'Approve' or 'Reject'.");
        }

        var ids = request.ClassificationIds?.Distinct().ToList() ?? new List<Guid>();
        var classifications = await _db.ListingClassifications.Where(c => ids.Contains(c.Id)).ToListAsync(ct);
        var userId = this.GetUserId();
        var touchedVariantIds = new HashSet<Guid>();

        foreach (var classification in classifications)
        {
            var isApprove = string.Equals(request.Action, "Approve", StringComparison.OrdinalIgnoreCase);
            _db.ClassificationFeedbacks.Add(new ClassificationFeedback
            {
                Id = Guid.NewGuid(),
                ListingClassificationId = classification.Id,
                OriginalGuessJson = SnapshotJson(classification),
                CorrectedValuesJson = null,
                Action = isApprove ? FeedbackAction.Approved : FeedbackAction.Rejected,
                Reason = request.Reason,
                ReviewedByUserId = userId,
            });
            classification.Status = isApprove ? ClassificationStatus.AutoAccepted : ClassificationStatus.Rejected;
            touchedVariantIds.Add(classification.ResolvedCardVariantId);
        }

        await _db.SaveChangesAsync(ct);
        foreach (var variantId in touchedVariantIds)
        {
            await _reaggregation.ReaggregateAsync(variantId, ct);
        }

        return Ok(new BulkClassificationActionResultDto(classifications.Count, ids.Count - classifications.Count));
    }

    private static string SnapshotJson(ListingClassification c) => JsonSerializer.Serialize(new
    {
        c.GradedStatus,
        c.Grader,
        c.Grade,
        c.RawCondition,
        c.VariantMatch,
        c.Language,
        c.BestOfferAccepted,
        c.ConfidenceScore,
        c.Status,
    });

    // ---- Run history ----

    [HttpGet("runs")]
    public async Task<ActionResult<PagedResult<ScrapeRunDto>>> GetRunHistory([FromQuery] int page, [FromQuery] int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0 ? 20 : Math.Clamp(pageSize, 1, 100);

        var query = _db.ScrapeRuns.OrderByDescending(r => r.StartedAt);
        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var dtos = items.Select(r => new ScrapeRunDto(
            r.Id, r.StartedAt, r.CompletedAt, r.Status.ToString(), r.TriggeredBy.ToString(), r.TriggeredByUserId,
            r.CardsProcessed, r.ListingsFound, r.ListingsAccepted, r.ListingsQuarantined, r.ListingsRejected, r.ErrorMessage)).ToList();

        return Ok(new PagedResult<ScrapeRunDto>(dtos, page, pageSize, totalCount));
    }
}
