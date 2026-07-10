using Microsoft.AspNetCore.Identity;

namespace PokeBinder.Core.Identity;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
