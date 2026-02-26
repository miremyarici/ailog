using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIBlog.Web.Data;

namespace AIBlog.Web.Controllers;

public class NotificationController : BaseController
{
    public NotificationController(ApplicationDbContext context) : base(context)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == 0) return Json(new { count = 0 });

        var count = await _context.Notifications
            .CountAsync(n => n.UserId == currentUserId && !n.IsRead);

        return Json(new { count });
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(int skip = 0, int take = 3)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == 0) return Unauthorized();

        var notifications = await _context.Notifications
            .Where(n => n.UserId == currentUserId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        // Mark fetched notifications as read
        var unreadNotifications = notifications.Where(n => !n.IsRead).ToList();
        if (unreadNotifications.Any())
        {
            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }

        var formattedNotifications = notifications.Select(n => new {
            n.Id,
            n.Message,
            n.Type,
            n.ReferenceLink,
            IsRead = unreadNotifications.Contains(n) ? false : n.IsRead, // Return previous state to frontend so it can highlight if needed
            CreatedAt = n.CreatedAt.ToString("MMM dd, yyyy HH:mm")
        }).ToList();

        return Json(new { notifications = formattedNotifications });
    }
}
