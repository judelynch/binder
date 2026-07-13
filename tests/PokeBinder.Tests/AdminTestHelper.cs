using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PokeBinder.Core.Identity;

namespace PokeBinder.Tests;

public static class AdminTestHelper
{
    /// <summary>Promotes an already-registered user to the Admin role via the real Identity APIs (mirrors DbInitializer's own approach).</summary>
    public static async Task PromoteToAdminAsync(CustomWebApplicationFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
        }

        var user = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"User {email} not found.");

        if (!await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            await userManager.AddToRoleAsync(user, Roles.Admin);
        }
    }
}
