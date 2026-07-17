using Microsoft.EntityFrameworkCore;
using PokeBinder.Core.Binders;
using PokeBinder.Core.Cards;
using PokeBinder.Infrastructure;
using PokeBinder.Infrastructure.Pricing;
using Xunit;

namespace PokeBinder.Tests;

public class ScrapeScopeProviderTests : IAsyncLifetime
{
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor("PokeBinderTest_ScrapeScope");

    private Guid _assignedVariantId;
    private Guid _unassignedVariantId;
    private Guid _slotId;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        var set = new Set { Id = "scope-set", Name = "Scope Set", Series = "Scope", PrintedTotal = 2, Total = 2, ReleaseDate = new DateOnly(2022, 1, 1), UpdatedAt = DateTime.UtcNow };
        db.Sets.Add(set);

        var variantType = new VariantType { Id = Guid.NewGuid(), Name = "Normal" };
        db.VariantTypes.Add(variantType);

        var cardA = new Card { Id = "scope-1", SetId = set.Id, Name = "Bulbasaur", Supertype = "Pokémon", Number = "1" };
        var cardB = new Card { Id = "scope-2", SetId = set.Id, Name = "Ivysaur", Supertype = "Pokémon", Number = "2" };
        db.Cards.AddRange(cardA, cardB);

        _assignedVariantId = Guid.NewGuid();
        _unassignedVariantId = Guid.NewGuid();
        db.CardVariants.Add(new CardVariant { Id = _assignedVariantId, CardId = cardA.Id, VariantTypeId = variantType.Id });
        db.CardVariants.Add(new CardVariant { Id = _unassignedVariantId, CardId = cardB.Id, VariantTypeId = variantType.Id });

        var binder = new Binder { Id = Guid.NewGuid(), OwnerId = "scope-owner", Name = "Scope Binder", ColourHex = "#000000", Rows = 3, Columns = 3, CreatedAt = DateTime.UtcNow };
        db.Binders.Add(binder);
        var page = new BinderPage { Id = Guid.NewGuid(), BinderId = binder.Id, PageNumber = 1 };
        db.BinderPages.Add(page);

        _slotId = Guid.NewGuid();
        db.BinderSlots.Add(new BinderSlot { Id = _slotId, PageId = page.Id, Position = 0, CardVariantId = _assignedVariantId, Owned = false });
        db.BinderSlots.Add(new BinderSlot { Id = Guid.NewGuid(), PageId = page.Id, Position = 1, CardVariantId = null });

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private PokeBinderDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options);

    [Fact]
    public async Task OnlyReturnsVariantsAssignedToABinderSlot()
    {
        await using var db = CreateContext();
        var scope = new BinderScrapeScopeProvider(db);

        var ids = await scope.GetInScopeCardVariantIdsAsync(CancellationToken.None);

        Assert.Contains(_assignedVariantId, ids);
        Assert.DoesNotContain(_unassignedVariantId, ids);
    }

    [Fact]
    public async Task UnassigningASlot_RemovesTheVariantFromScope()
    {
        await using (var db = CreateContext())
        {
            var slot = await db.BinderSlots.FindAsync(_slotId);
            slot!.CardVariantId = null;
            await db.SaveChangesAsync();
        }

        await using var verifyDb = CreateContext();
        var scope = new BinderScrapeScopeProvider(verifyDb);
        var ids = await scope.GetInScopeCardVariantIdsAsync(CancellationToken.None);

        Assert.DoesNotContain(_assignedVariantId, ids);
    }

    [Fact]
    public async Task SameVariantAssignedToMultipleSlots_AppearsOnlyOnce()
    {
        await using (var db = CreateContext())
        {
            var page = await db.BinderPages.FirstAsync();
            db.BinderSlots.Add(new BinderSlot { Id = Guid.NewGuid(), PageId = page.Id, Position = 2, CardVariantId = _assignedVariantId, Owned = true });
            await db.SaveChangesAsync();
        }

        await using var verifyDb = CreateContext();
        var scope = new BinderScrapeScopeProvider(verifyDb);
        var ids = await scope.GetInScopeCardVariantIdsAsync(CancellationToken.None);

        Assert.Single(ids, id => id == _assignedVariantId);
    }
}
