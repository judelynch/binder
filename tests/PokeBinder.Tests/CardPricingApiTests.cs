using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Pricing;
using PokeBinder.Infrastructure;
using Xunit;

namespace PokeBinder.Tests;

public class CardPricingApiTests : IAsyncLifetime
{
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor("PokeBinderTest_CardPricing");

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
            _cardVariantIds = await CardFixture.SeedCardVariantsAsync(db, 3);
        }

        _factory = new CustomWebApplicationFactory(ConnectionString);
        _client = _factory.CreateClient();
        var email = $"card-pricing-{Guid.NewGuid():N}@pokebinder.test";
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

    private void SeedPricePoint(Guid cardVariantId, decimal itemOnlyGbp, decimal deliveredGbp)
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        using var db = new PokeBinderDbContext(options);
        db.PricePoints.Add(new PricePoint
        {
            Id = Guid.NewGuid(),
            CardVariantId = cardVariantId,
            GradedStatus = GradedStatus.Raw,
            Condition = RawConditionClassification.NM,
            WindowDays = 30,
            ItemOnlyMedianGbp = itemOnlyGbp,
            DeliveredMedianGbp = deliveredGbp,
            SampleCount = 5,
            MinGbp = itemOnlyGbp,
            MaxGbp = itemOnlyGbp,
            LastSaleDate = DateTime.UtcNow.AddDays(-1),
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task GetCardPrices_ReturnsOneEntryPerVariant_EvenWithoutPriceData()
    {
        var response = await _client.GetAsync("/api/cards/fixture-set-1/prices");
        response.EnsureSuccessStatusCode();
        var prices = await response.Content.ReadFromJsonAsync<List<CardVariantPriceDto>>();

        var entry = Assert.Single(prices!);
        Assert.Equal(_cardVariantIds[0], entry.CardVariantId);
        Assert.Null(entry.BestAvailableItemOnlyGbp);
        Assert.Empty(entry.RawBuckets);
    }

    [Fact]
    public async Task GetCardPrices_IncludesPriceData_WhenPresent()
    {
        SeedPricePoint(_cardVariantIds[0], 9.99m, 11.50m);

        var response = await _client.GetAsync("/api/cards/fixture-set-1/prices");
        response.EnsureSuccessStatusCode();
        var prices = await response.Content.ReadFromJsonAsync<List<CardVariantPriceDto>>();

        var entry = Assert.Single(prices!);
        Assert.Equal(9.99m, entry.BestAvailableItemOnlyGbp);
        Assert.Equal(11.50m, entry.BestAvailableDeliveredGbp);
        Assert.Single(entry.RawBuckets);
    }

    [Fact]
    public async Task GetSetPrices_ReturnsOnlyVariantsWithPriceData()
    {
        SeedPricePoint(_cardVariantIds[0], 9.99m, 11.50m);
        // _cardVariantIds[1] and [2] are left unpriced - the set-prices endpoint should omit them,
        // unlike the single-card endpoint which always returns one entry per variant.

        var response = await _client.GetAsync("/api/sets/fixture-set/prices");
        response.EnsureSuccessStatusCode();
        var prices = await response.Content.ReadFromJsonAsync<List<CardVariantPriceDto>>();

        var entry = Assert.Single(prices!);
        Assert.Equal(_cardVariantIds[0], entry.CardVariantId);
        Assert.Equal(9.99m, entry.BestAvailableItemOnlyGbp);
    }

    [Fact]
    public async Task GetCardPrices_UnknownCard_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/cards/does-not-exist/prices");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetVariantPriceHistory_ReturnsOnlyAcceptedEnglishNonBestOfferSales_OldestFirst()
    {
        var variantId = _cardVariantIds[0];
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using (var db = new PokeBinderDbContext(options))
        {
            void AddListing(string listingId, decimal price, DateTime soldDate, ClassificationStatus status, bool bestOffer = false, string language = "English")
            {
                var raw = new RawListing
                {
                    Id = Guid.NewGuid(),
                    CardVariantId = variantId,
                    ListingId = listingId,
                    SourceProvider = "Mock",
                    Title = "Test listing",
                    ItemPriceGbp = price,
                    PostagePriceGbp = 1.00m,
                    SoldDate = soldDate,
                    ListingFormat = ListingFormat.BuyItNow,
                };
                db.RawListings.Add(raw);
                db.ListingClassifications.Add(new ListingClassification
                {
                    Id = Guid.NewGuid(),
                    RawListingId = raw.Id,
                    ResolvedCardVariantId = variantId,
                    Status = status,
                    BestOfferAccepted = bestOffer,
                    Language = language,
                    ConfidenceScore = 100,
                });
            }

            AddListing("l1", 10.00m, DateTime.UtcNow.AddDays(-10), ClassificationStatus.AutoAccepted);
            AddListing("l2", 12.00m, DateTime.UtcNow.AddDays(-5), ClassificationStatus.AutoAccepted);
            AddListing("l3", 999.00m, DateTime.UtcNow.AddDays(-3), ClassificationStatus.Quarantined);
            AddListing("l4", 999.00m, DateTime.UtcNow.AddDays(-2), ClassificationStatus.AutoAccepted, bestOffer: true);
            AddListing("l5", 999.00m, DateTime.UtcNow.AddDays(-1), ClassificationStatus.AutoAccepted, language: "German");

            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/cards/fixture-set-1/variants/{variantId}/price-history");
        response.EnsureSuccessStatusCode();
        var history = await response.Content.ReadFromJsonAsync<List<PriceHistoryPointDto>>();

        Assert.Equal(2, history!.Count);
        Assert.Equal(10.00m, history[0].ItemPriceGbp);
        Assert.Equal(11.00m, history[0].DeliveredPriceGbp);
        Assert.Equal(12.00m, history[1].ItemPriceGbp);
    }

    [Fact]
    public async Task GetVariantPriceHistory_UnknownVariant_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/cards/fixture-set-1/variants/{Guid.NewGuid()}/price-history");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record TokenOnly(string Token);
}
