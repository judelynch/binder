using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Pricing;
using PokeBinder.Infrastructure;
using Xunit;

namespace PokeBinder.Tests;

public class BinderPricingApiTests : IAsyncLifetime
{
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor("PokeBinderTest_BinderPricing");

    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private List<Guid> _cardVariantIds = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using (var db = new PokeBinderDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            _cardVariantIds = await CardFixture.SeedCardVariantsAsync(db, 5);
        }

        _factory = new CustomWebApplicationFactory(ConnectionString);
        _client = _factory.CreateClient();
        var email = $"pricing-owner-{Guid.NewGuid():N}@pokebinder.test";
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "TestPass123!" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenOnly>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<Guid> CreateBinderAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/binders", new { name = "Pricing Binder", colourHex = "#336699", rows = 2, columns = 2, initialPageCount = 2 });
        response.EnsureSuccessStatusCode();
        var binder = await response.Content.ReadFromJsonAsync<BinderSummaryDto>();
        return binder!.Id;
    }

    private async Task<List<BinderSlotDto>> GetFirstPageSlotsAsync(Guid binderId)
    {
        var response = await _client.GetAsync($"/api/binders/{binderId}/spread/0");
        response.EnsureSuccessStatusCode();
        var spread = await response.Content.ReadFromJsonAsync<SpreadResponseDto>();
        return spread!.RightPanel.Slots!.ToList();
    }

    private void SeedPricePoint(Guid cardVariantId, GradedStatus gradedStatus, string? grader, decimal? grade,
        RawConditionClassification? condition, int windowDays, decimal itemOnlyGbp, decimal deliveredGbp, int sampleCount = 5, string? quarantinedReason = null)
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        using var db = new PokeBinderDbContext(options);
        db.PricePoints.Add(new PricePoint
        {
            Id = Guid.NewGuid(),
            CardVariantId = cardVariantId,
            GradedStatus = gradedStatus,
            Grader = grader,
            Grade = grade,
            Condition = condition,
            WindowDays = windowDays,
            ItemOnlyMedianGbp = itemOnlyGbp,
            DeliveredMedianGbp = deliveredGbp,
            SampleCount = sampleCount,
            MinGbp = itemOnlyGbp,
            MaxGbp = itemOnlyGbp,
            LastSaleDate = DateTime.UtcNow.AddDays(-1),
            QuarantinedReason = quarantinedReason,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task GetPrices_NoPriceData_ReturnsEmptyWithNullTotals()
    {
        var binderId = await CreateBinderAsync();
        var slots = await GetFirstPageSlotsAsync(binderId);
        await _client.PutAsJsonAsync($"/api/binders/{binderId}/slots/{slots[0].SlotId}", new { cardVariantId = _cardVariantIds[0] });

        var response = await _client.GetAsync($"/api/binders/{binderId}/prices");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<BinderPriceSummaryDto>();

        Assert.NotNull(summary);
        Assert.Null(summary!.OwnedValueGbp);
        Assert.Null(summary.MissingCostGbp);
        Assert.Empty(summary.Prices);
    }

    [Fact]
    public async Task GetPrices_PicksCheapestRawBucketAsBestAvailable()
    {
        var binderId = await CreateBinderAsync();
        var slots = await GetFirstPageSlotsAsync(binderId);
        var variantId = _cardVariantIds[0];
        await _client.PutAsJsonAsync($"/api/binders/{binderId}/slots/{slots[0].SlotId}", new { cardVariantId = variantId });

        SeedPricePoint(variantId, GradedStatus.Raw, null, null, RawConditionClassification.NM, 30, itemOnlyGbp: 12.00m, deliveredGbp: 14.00m);
        SeedPricePoint(variantId, GradedStatus.Raw, null, null, RawConditionClassification.Unspecified, 30, itemOnlyGbp: 8.50m, deliveredGbp: 10.00m);
        SeedPricePoint(variantId, GradedStatus.Graded, "PSA", 10m, null, 30, itemOnlyGbp: 120.00m, deliveredGbp: 122.00m);

        var response = await _client.GetAsync($"/api/binders/{binderId}/prices");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<BinderPriceSummaryDto>();

        var priced = Assert.Single(summary!.Prices);
        Assert.Equal(variantId, priced.CardVariantId);
        Assert.Equal(8.50m, priced.BestAvailableItemOnlyGbp);
        Assert.Equal(10.00m, priced.BestAvailableDeliveredGbp);
        Assert.Equal(2, priced.RawBuckets.Count);
        Assert.Single(priced.GradedBuckets);
    }

    [Fact]
    public async Task GetPrices_QuarantinedBucket_ExcludedFromEverything()
    {
        var binderId = await CreateBinderAsync();
        var slots = await GetFirstPageSlotsAsync(binderId);
        var variantId = _cardVariantIds[0];
        await _client.PutAsJsonAsync($"/api/binders/{binderId}/slots/{slots[0].SlotId}", new { cardVariantId = variantId });

        SeedPricePoint(variantId, GradedStatus.Raw, null, null, RawConditionClassification.NM, 30, itemOnlyGbp: 1.00m, deliveredGbp: 1.50m, quarantinedReason: "sanity check failed");

        var response = await _client.GetAsync($"/api/binders/{binderId}/prices");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<BinderPriceSummaryDto>();

        Assert.Empty(summary!.Prices);
    }

    [Fact]
    public async Task GetPrices_OwnedAndMissingSlots_ComputeSeparateTotals()
    {
        var binderId = await CreateBinderAsync();
        var slots = await GetFirstPageSlotsAsync(binderId);
        var ownedVariant = _cardVariantIds[0];
        var missingVariant = _cardVariantIds[1];

        await _client.PutAsJsonAsync($"/api/binders/{binderId}/slots/{slots[0].SlotId}", new { cardVariantId = ownedVariant });
        await _client.PatchAsJsonAsync($"/api/binders/{binderId}/slots/{slots[0].SlotId}", new { owned = true, quantity = 3 });
        await _client.PutAsJsonAsync($"/api/binders/{binderId}/slots/{slots[1].SlotId}", new { cardVariantId = missingVariant });

        SeedPricePoint(ownedVariant, GradedStatus.Raw, null, null, RawConditionClassification.NM, 30, itemOnlyGbp: 10.00m, deliveredGbp: 11.00m);
        SeedPricePoint(missingVariant, GradedStatus.Raw, null, null, RawConditionClassification.Unspecified, 30, itemOnlyGbp: 4.00m, deliveredGbp: 5.00m);

        var response = await _client.GetAsync($"/api/binders/{binderId}/prices");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<BinderPriceSummaryDto>();

        Assert.Equal(30.00m, summary!.OwnedValueGbp); // 10.00 * quantity 3
        Assert.Equal(4.00m, summary.MissingCostGbp);
    }

    [Fact]
    public async Task GetPrices_UnknownOrUnownedBinder_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/binders/{Guid.NewGuid()}/prices");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_ComputesPortfolioValue_FromOwnedSlotsAcrossAllBinders()
    {
        var binderId = await CreateBinderAsync();
        var slots = await GetFirstPageSlotsAsync(binderId);
        var ownedVariant = _cardVariantIds[0];
        var missingVariant = _cardVariantIds[1];

        await _client.PutAsJsonAsync($"/api/binders/{binderId}/slots/{slots[0].SlotId}", new { cardVariantId = ownedVariant });
        await _client.PatchAsJsonAsync($"/api/binders/{binderId}/slots/{slots[0].SlotId}", new { owned = true, quantity = 2 });
        await _client.PutAsJsonAsync($"/api/binders/{binderId}/slots/{slots[1].SlotId}", new { cardVariantId = missingVariant });
        // assigned but not owned - must not contribute to portfolio value

        SeedPricePoint(ownedVariant, GradedStatus.Raw, null, null, RawConditionClassification.NM, 30, itemOnlyGbp: 15.00m, deliveredGbp: 16.00m);
        SeedPricePoint(missingVariant, GradedStatus.Raw, null, null, RawConditionClassification.Unspecified, 30, itemOnlyGbp: 999.00m, deliveredGbp: 1000.00m);

        var response = await _client.GetAsync("/api/dashboard");
        response.EnsureSuccessStatusCode();
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponseDto>();

        Assert.Equal(30.00m, dashboard!.PortfolioValueGbp); // 15.00 * quantity 2
    }

    [Fact]
    public async Task Dashboard_TopValuableCards_RankedByPerUnitPriceNotQuantity()
    {
        var binderId = await CreateBinderAsync();
        var slots = await GetFirstPageSlotsAsync(binderId);
        var cheapButManyCopies = _cardVariantIds[0];
        var expensiveSingleCopy = _cardVariantIds[1];

        await _client.PutAsJsonAsync($"/api/binders/{binderId}/slots/{slots[0].SlotId}", new { cardVariantId = cheapButManyCopies });
        await _client.PatchAsJsonAsync($"/api/binders/{binderId}/slots/{slots[0].SlotId}", new { owned = true, quantity = 50 });
        await _client.PutAsJsonAsync($"/api/binders/{binderId}/slots/{slots[1].SlotId}", new { cardVariantId = expensiveSingleCopy });
        await _client.PatchAsJsonAsync($"/api/binders/{binderId}/slots/{slots[1].SlotId}", new { owned = true, quantity = 1 });

        SeedPricePoint(cheapButManyCopies, GradedStatus.Raw, null, null, RawConditionClassification.NM, 30, itemOnlyGbp: 1.00m, deliveredGbp: 2.00m);
        SeedPricePoint(expensiveSingleCopy, GradedStatus.Raw, null, null, RawConditionClassification.NM, 30, itemOnlyGbp: 200.00m, deliveredGbp: 201.00m);

        var response = await _client.GetAsync("/api/dashboard");
        response.EnsureSuccessStatusCode();
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponseDto>();

        Assert.Equal(expensiveSingleCopy, dashboard!.TopValuableCards.First().CardVariantId);
        Assert.Equal(200.00m, dashboard.TopValuableCards.First().PriceGbp);
    }

    private record TokenOnly(string Token);
}
