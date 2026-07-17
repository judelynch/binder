using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PokeBinder.Api;
using PokeBinder.Core.Identity;
using PokeBinder.Core.Pricing;
using PokeBinder.Infrastructure;
using PokeBinder.Infrastructure.Cards.Import;
using PokeBinder.Infrastructure.Identity;
using PokeBinder.Infrastructure.Pricing;
using PokeBinder.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PokeBinderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<PokeBinderDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<ITokenService, TokenService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<CardDataImportOptions>(builder.Configuration.GetSection(CardDataImportOptions.SectionName));
builder.Services.AddHttpClient();
builder.Services.AddScoped<ICardDataSource>(sp =>
{
    var options = sp.GetRequiredService<IOptions<CardDataImportOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.LocalPath))
    {
        return new LocalDirectoryCardDataSource(options.LocalPath);
    }

    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    httpClient.Timeout = TimeSpan.FromMinutes(5);
    return new GitHubTarballCardDataSource(httpClient, options.TarballUrl);
});
builder.Services.AddScoped<CardDataImporter>();

// Pricing pipeline (Phase 8). IPriceSourceProvider is MockPriceSourceProvider for now - the
// pipeline downstream of it doesn't know or care, so swapping in a real provider later is a
// one-line registration change, nothing else.
builder.Services.Configure<ClassifierOptions>(builder.Configuration.GetSection("Pricing:Classifier"));
builder.Services.Configure<PricingScrapeOptions>(builder.Configuration.GetSection("Pricing:Scrape"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ClassifierOptions>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<PricingScrapeOptions>>().Value);
builder.Services.AddScoped<IPriceSourceProvider, MockPriceSourceProvider>();
builder.Services.AddScoped<IScrapeScopeProvider, BinderScrapeScopeProvider>();
builder.Services.AddScoped<IListingClassifier, ListingClassifier>();
builder.Services.AddScoped<IPriceAggregator, PriceAggregator>();
builder.Services.AddScoped<PriceReaggregationService>();
builder.Services.AddScoped<PricingScrapeOrchestrator>();

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddHangfireServer();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (args.Contains("seed"))
{
    using var seedScope = app.Services.CreateScope();
    var importer = seedScope.ServiceProvider.GetRequiredService<CardDataImporter>();
    var summary = await importer.RunAsync();
    Console.WriteLine($"Sets added: {summary.SetsAdded}, updated: {summary.SetsUpdated}");
    Console.WriteLine($"Cards added: {summary.CardsAdded}, updated: {summary.CardsUpdated}");
    Console.WriteLine($"Elapsed: {summary.Elapsed}");
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await DbInitializer.SeedAsync(roleManager, userManager, configuration);
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new LocalhostOnlyDashboardAuthFilter() },
});

using (var recurringScope = app.Services.CreateScope())
{
    var recurringJobs = recurringScope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobs.AddOrUpdate<PricingScrapeOrchestrator>(
        "nightly-price-scrape",
        orchestrator => orchestrator.RunAsync(ScrapeTrigger.Nightly, null, null, CancellationToken.None),
        "30 2 * * *");
}

app.MapControllers();

app.Run();

public partial class Program
{
}
