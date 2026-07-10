using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Core.Identity;

namespace PokeBinder.Infrastructure;

public class PokeBinderDbContext : IdentityDbContext<ApplicationUser>
{
    public PokeBinderDbContext(DbContextOptions<PokeBinderDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
