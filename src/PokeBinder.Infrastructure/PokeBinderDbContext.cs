using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Core.Cards;
using PokeBinder.Core.Identity;

namespace PokeBinder.Infrastructure;

public class PokeBinderDbContext : IdentityDbContext<ApplicationUser>
{
    public PokeBinderDbContext(DbContextOptions<PokeBinderDbContext> options)
        : base(options)
    {
    }

    public DbSet<Set> Sets { get; set; } = null!;
    public DbSet<Card> Cards { get; set; } = null!;
    public DbSet<CardPokedexNumber> CardPokedexNumbers { get; set; } = null!;
    public DbSet<VariantType> VariantTypes { get; set; } = null!;
    public DbSet<CardVariant> CardVariants { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(PokeBinderDbContext).Assembly);
    }
}
