using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Binders;
using PokeBinder.Infrastructure;

namespace PokeBinder.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/overlay-tags")]
public class OverlayTagsController : ControllerBase
{
    private readonly PokeBinderDbContext _db;

    public OverlayTagsController(PokeBinderDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OverlayTagDto>>> List(CancellationToken ct)
    {
        var userId = this.GetUserId();
        var tags = await _db.OverlayTags
            .Where(t => t.OwnerId == userId)
            .OrderBy(t => t.Name)
            .Select(t => new OverlayTagDto(t.Id, t.Name, t.ColourHex))
            .ToListAsync(ct);

        return Ok(tags);
    }

    [HttpPost]
    public async Task<ActionResult<OverlayTagDto>> Create(CreateOverlayTagRequest request, CancellationToken ct)
    {
        var userId = this.GetUserId();

        var nameTaken = await _db.OverlayTags.AnyAsync(t => t.OwnerId == userId && t.Name == request.Name, ct);
        if (nameTaken)
        {
            return Conflict(new { message = "An overlay tag with this name already exists." });
        }

        var tag = new OverlayTag
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Name = request.Name,
            ColourHex = request.ColourHex
        };

        _db.OverlayTags.Add(tag);
        await _db.SaveChangesAsync(ct);

        return Ok(new OverlayTagDto(tag.Id, tag.Name, tag.ColourHex));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<OverlayTagDto>> Update(Guid id, UpdateOverlayTagRequest request, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var tag = await _db.OverlayTags.FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == userId, ct);
        if (tag is null)
        {
            return NotFound();
        }

        if (request.Name is not null)
        {
            var nameTaken = await _db.OverlayTags.AnyAsync(t => t.OwnerId == userId && t.Name == request.Name && t.Id != id, ct);
            if (nameTaken)
            {
                return Conflict(new { message = "An overlay tag with this name already exists." });
            }

            tag.Name = request.Name;
        }

        if (request.ColourHex is not null)
        {
            tag.ColourHex = request.ColourHex;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new OverlayTagDto(tag.Id, tag.Name, tag.ColourHex));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var tag = await _db.OverlayTags.FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == userId, ct);
        if (tag is null)
        {
            return NotFound();
        }

        _db.OverlayTags.Remove(tag);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
