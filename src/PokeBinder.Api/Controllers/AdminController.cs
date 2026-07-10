using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokeBinder.Core.Cards;
using PokeBinder.Core.Identity;
using PokeBinder.Infrastructure.Cards.Import;

namespace PokeBinder.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController : ControllerBase
{
    private readonly CardDataImporter _importer;

    public AdminController(CardDataImporter importer)
    {
        _importer = importer;
    }

    [HttpPost("sync")]
    public async Task<ActionResult<CardImportSummary>> Sync(CancellationToken ct)
    {
        var summary = await _importer.RunAsync(ct);
        return Ok(summary);
    }
}
