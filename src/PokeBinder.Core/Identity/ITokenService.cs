namespace PokeBinder.Core.Identity;

public interface ITokenService
{
    string CreateAccessToken(ApplicationUser user, IList<string> roles);
}
