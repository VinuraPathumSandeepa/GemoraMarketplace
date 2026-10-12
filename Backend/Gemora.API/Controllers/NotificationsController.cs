using System.Security.Claims;
using Gemora.Application.DTOs.Notifications;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(
        ApplicationDbContext context)
    {
        _context = context;
    }


    // ============================================================
    // GET CURRENT USER NOTIFICATIONS
    // ============================================================

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>>
        GetNotifications()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var notifications =
            await _context
                .Set<Notification>()
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Type = n.Type,
                    Title = n.Title,
                    Message = n.Message,
                    RelatedEntityType = n.RelatedEntityType,
                    RelatedEntityId = n.RelatedEntityId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

        return Ok(notifications);
    }


    // ============================================================
    // GET UNREAD COUNT
    // ============================================================

    [HttpGet("unread-count")]
    public async Task<IActionResult>
        GetUnreadCount()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var unreadCount =
            await _context
                .Set<Notification>()
                .CountAsync(
                    n =>
                        n.UserId == userId &&
                        !n.IsRead);

        return Ok(new
        {
            unreadCount
        });
    }


    // ============================================================
    // MARK ONE NOTIFICATION AS READ
    // ============================================================

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult>
        MarkAsRead(Guid id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var notification =
            await _context
                .Set<Notification>()
                .FirstOrDefaultAsync(
                    n =>
                        n.Id == id &&
                        n.UserId == userId);

        if (notification == null)
        {
            return NotFound(new
            {
                message = "Notification was not found."
            });
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;

            await _context.SaveChangesAsync();
        }

        return NoContent();
    }


    // ============================================================
    // MARK ALL CURRENT USER NOTIFICATIONS AS READ
    // ============================================================

    [HttpPut("read-all")]
    public async Task<IActionResult>
        MarkAllAsRead()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var unreadNotifications =
            await _context
                .Set<Notification>()
                .Where(
                    n =>
                        n.UserId == userId &&
                        !n.IsRead)
                .ToListAsync();

        if (unreadNotifications.Count == 0)
        {
            return NoContent();
        }

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }


    // ============================================================
    // CURRENT AUTHENTICATED USER
    // ============================================================

    private bool TryGetCurrentUserId(
        out Guid userId)
    {
        var claimValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            claimValue,
            out userId);
    }
}