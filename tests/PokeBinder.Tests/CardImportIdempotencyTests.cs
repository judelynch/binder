using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PokeBinder.Infrastructure;
using PokeBinder.Infrastructure.Cards.Import;
using Xunit;

namespace PokeBinder.Tests;

public class CardImportIdempotencyTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Server=localhost\\SQLEXPRESS;Database=PokeBinderTest_CardImport;Trusted_Connection=True;TrustServerCertificate=True";

    private static readonly string FixtureRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "CardData");

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static CardDataImporter CreateImporter(PokeBinderDbContext db) =>
        new(db, new LocalDirectoryCardDataSource(FixtureRoot), NullLogger<CardDataImporter>.Instance);

    private static DbContextOptions<PokeBinderDbContext> Options() =>
        new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;

    [Fact]
    public async Task Import_RunTwice_SecondRunAddsAndUpdatesNothing()
    {
        await using (var db = new PokeBinderDbContext(Options()))
        {
            var summary1 = await CreateImporter(db).RunAsync();
            Assert.Equal(2, summary1.SetsAdded);
            Assert.Equal(0, summary1.SetsUpdated);
            Assert.Equal(9, summary1.CardsAdded);
            Assert.Equal(0, summary1.CardsUpdated);
        }

        await using (var db = new PokeBinderDbContext(Options()))
        {
            var summary2 = await CreateImporter(db).RunAsync();
            Assert.Equal(0, summary2.SetsAdded);
            Assert.Equal(0, summary2.SetsUpdated);
            Assert.Equal(0, summary2.CardsAdded);
            Assert.Equal(0, summary2.CardsUpdated);
        }

        await using var verifyDb = new PokeBinderDbContext(Options());
        Assert.Equal(2, await verifyDb.Sets.CountAsync());
        Assert.Equal(9, await verifyDb.Cards.CountAsync());
        Assert.Equal(9, await verifyDb.CardVariants.CountAsync());
        Assert.Equal(6, await verifyDb.VariantTypes.CountAsync());
    }

    [Fact]
    public async Task Import_CreatesNormalVariantForEveryCard()
    {
        await using var db = new PokeBinderDbContext(Options());
        await CreateImporter(db).RunAsync();

        var normalId = await db.VariantTypes.Where(v => v.Name == "Normal").Select(v => v.Id).SingleAsync();
        var cardIds = await db.Cards.Select(c => c.Id).ToListAsync();

        foreach (var cardId in cardIds)
        {
            var hasNormal = await db.CardVariants.AnyAsync(v => v.CardId == cardId && v.VariantTypeId == normalId);
            Assert.True(hasNormal, $"Card {cardId} is missing its Normal variant.");
        }
    }
}
