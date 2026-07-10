using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Infrastructure;
using Xunit;

namespace PokeBinder.Tests;

public class AuthFlowTests : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>()
            .UseSqlServer(CustomWebApplicationFactory.ConnectionString)
            .Options;

        await using (var db = new PokeBinderDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }

        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Register_Login_Me_ReturnsAuthenticatedUser()
    {
        var email = $"smoke-{Guid.NewGuid():N}@pokebinder.test";
        const string password = "SmokeTest123!";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        Assert.True(registerResponse.IsSuccessStatusCode, $"Register failed: {(int)registerResponse.StatusCode} {registerContent}");
        var registerBody = JsonSerializer.Deserialize<AuthResponse>(registerContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(registerBody);
        Assert.Contains("User", registerBody!.Roles);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(loginBody);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.Token);

        var meResponse = await _client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();
        var meBody = await meResponse.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(meBody);
        Assert.Equal(email, meBody!.Email, ignoreCase: true);
    }
}
