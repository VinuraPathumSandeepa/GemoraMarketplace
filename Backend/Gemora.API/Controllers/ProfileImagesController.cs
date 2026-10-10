using Gemora.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/profile-images")]
public class ProfileImagesController(ApplicationDbContext db) : ControllerBase
{
    // Avatar URLs are public, like the existing storage URLs, with random identifiers.
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var image = await db.ProfileImages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (image == null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "public,max-age=3600";
        return File(image.Content, image.ContentType);
    }
}
