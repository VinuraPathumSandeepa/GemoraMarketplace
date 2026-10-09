
using System.Security.Claims;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gemora.API.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Buyer)]
[Route("api/wishlist")]
public sealed class WishlistController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IMarketplaceService _marketplace;
    private readonly ILogger<WishlistController> _logger;

    public WishlistController(
        ApplicationDbContext db,
        IMarketplaceService marketplace,
        ILogger<WishlistController> logger)
    {
        _db = db;
        _marketplace = marketplace;
        _logger = logger;
    }

    private Guid? CurrentBuyerId =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var id
        ) ? id : null;

    // GET /api/wishlist
    [HttpGet]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        if (CurrentBuyerId is not Guid buyerId)
            return Unauthorized(
                new { message = "Please sign in again." });

        var saved = await _db.WishlistItems
            .AsNoTracking()
            .Where(x => x.UserId == buyerId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new {
                x.GemListingId,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var result = new List<object>(saved.Count);

        foreach (var item in saved)
        {
            var gem = await _marketplace.GetByIdAsync(
                item.GemListingId);

            result.Add(new
            {
                gemListingId = item.GemListingId,
                savedAt = item.CreatedAt,
                gem
            });
        }

        return Ok(result);
    }

    // PUT /api/wishlist/{gemId}
    [HttpPut("{gemId:int}")]
    public async Task<IActionResult> Add(
        int gemId,
        CancellationToken cancellationToken)
    {
        if (CurrentBuyerId is not Guid buyerId)
            return Unauthorized(
                new { message = "Please sign in again." });

        if (gemId <= 0)
            return BadRequest(
                new { message = "Invalid gemstone ID." });

        var buyerExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == buyerId &&
                     x.Role == UserRoles.Buyer,
                cancellationToken
            );

        if (!buyerExists)
            return Unauthorized(new {
                message = "Buyer account was not found. Please sign in again."
            });

        var gem = await _marketplace.GetByIdAsync(gemId);

        if (gem is null)
            return NotFound(new {
                message = "This gemstone is not an approved listing."
            });

        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "WishlistItems"
                ("UserId", "GemListingId", "CreatedAt")
                VALUES ({buyerId}, {gemId}, NOW())
                ON CONFLICT ("UserId", "GemListingId")
                DO NOTHING;
                """, cancellationToken);

            return NoContent();
        }
        catch (PostgresException ex) when (
            ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            _logger.LogWarning(
                ex,
                "Wishlist foreign-key validation failed for buyer {BuyerId}, gem {GemId}",
                buyerId,
                gemId
            );

            return Conflict(new {
                message = "The account or gemstone has changed. Refresh the page and try again."
            });
        }
        catch (PostgresException ex) when (
            ex.SqlState == PostgresErrorCodes.UndefinedTable ||
            ex.SqlState == PostgresErrorCodes.UndefinedColumn ||
            ex.SqlState == PostgresErrorCodes.InvalidColumnReference)
        {
            _logger.LogError(
                ex,
                "Wishlist database schema does not match the application: SQLSTATE {SqlState}",
                ex.SqlState
            );

            return StatusCode(503, new {
                message = "Wishlist database schema is not ready. Check the wishlist table migration and restart the API."
            });
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Could not save gemstone {GemId} to buyer {BuyerId} wishlist",
                gemId,
                buyerId
            );

            return StatusCode(500, new {
                message = "Could not save this gem. Check the backend log for the database error."
            });
        }
    }

    // DELETE /api/wishlist/{gemId}
    [HttpDelete("{gemId:int}")]
    public async Task<IActionResult> Remove(
        int gemId,
        CancellationToken cancellationToken)
    {
        if (CurrentBuyerId is not Guid buyerId)
            return Unauthorized(
                new { message = "Please sign in again." });

        if (gemId <= 0)
            return BadRequest(
                new { message = "Invalid gemstone ID." });

        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM "WishlistItems"
                WHERE "UserId" = {buyerId}
                AND "GemListingId" = {gemId};
                """, cancellationToken);

            return NoContent();
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Could not remove gemstone {GemId} from buyer {BuyerId} wishlist",
                gemId,
                buyerId
            );

            return StatusCode(500, new {
                message = "Could not remove this gem. Check the backend log for the database error."
            });
        }
    }
}
  