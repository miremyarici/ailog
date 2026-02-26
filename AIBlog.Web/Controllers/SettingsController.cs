using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIBlog.Web.Data;
using AIBlog.Web.Models;
using AIBlog.Web.Models.Requests;

namespace AIBlog.Web.Controllers;

public class SettingsController : BaseController
{
    public SettingsController(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IActionResult> Settings()
    {
        // Get current author (simulating logged-in user)
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == GetCurrentUserId());
        
        if (author == null)
        {
            return RedirectToAction("Index", "Home");
        }

        // Mask email: show first 3 chars + ••• + @domain
        var maskedEmail = "No email set";
        if (!string.IsNullOrEmpty(author.Email) && author.Email.Contains("@"))
        {
            var emailParts = author.Email.Split('@');
            maskedEmail = emailParts[0].Length > 3 
                ? emailParts[0].Substring(0, 3) + "•••" + "@" + emailParts[1]
                : emailParts[0] + "•••@" + emailParts[1];
        }

        // Mask phone: show first 2 chars + ••• + last 3 digits
        var maskedPhone = "";
        if (!string.IsNullOrEmpty(author.PhoneNumber) && author.PhoneNumber.Length > 5)
        {
            maskedPhone = author.PhoneNumber.Substring(0, 2) + "•••" + author.PhoneNumber.Substring(author.PhoneNumber.Length - 3);
        }

        var categories = await _context.Categories.ToListAsync();
        
        var selectedInterests = await _context.AuthorInterests
            .Where(ai => ai.AuthorId == GetCurrentUserId())
            .Select(ai => ai.CategoryId)
            .ToListAsync();

        var activeSessions = await _context.AuthorSessions
            .Where(s => s.AuthorId == GetCurrentUserId() && !s.IsRevoked)
            .OrderByDescending(s => s.LastActive)
            .ToListAsync();

        var currentSessionId = HttpContext.Session.GetInt32("CurrentSessionId");
        if (currentSessionId.HasValue)
        {
            foreach (var session in activeSessions)
            {
                session.IsCurrentDevice = session.Id == currentSessionId.Value;
            }
        }

        var viewModel = new SettingsViewModel
        {
            CurrentUserName = author.Name,
            CurrentUserAvatar = author.Avatar,
            MaskedEmail = maskedEmail,
            MaskedPhone = maskedPhone,
            MaskedPassword = "••••••••",
            TwoFactorEnabled = author.TwoFactorEnabled ?? false,
            ProfileVisibility = author.ProfileVisibility ?? "Public",
            SearchEngineVisibility = author.SearchEngineVisibility ?? true,
            AllCategories = categories,
            SelectedCategoryIds = selectedInterests,
            ActiveSessions = activeSessions
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequest request)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == GetCurrentUserId());
        if (author == null) return NotFound();

        author.Email = request.Email;
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePhone([FromBody] UpdatePhoneRequest request)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == GetCurrentUserId());
        if (author == null) return NotFound();

        author.PhoneNumber = request.Phone;
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == GetCurrentUserId());
        if (author == null) return NotFound();

        // In production, verify current password and hash new password
        author.PasswordHash = request.NewPassword; // Should be hashed in production
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTwoFactor([FromBody] UpdateTwoFactorRequest request)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == GetCurrentUserId());
        if (author == null) return NotFound();

        author.TwoFactorEnabled = request.Enabled;
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfileVisibility([FromBody] UpdateVisibilityRequest request)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == GetCurrentUserId());
        if (author == null) return NotFound();

        author.ProfileVisibility = request.Visibility;
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSearchVisibility([FromBody] UpdateSearchVisibilityRequest request)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == GetCurrentUserId());
        if (author == null) return NotFound();

        author.SearchEngineVisibility = request.Visible;
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateInterests([FromBody] UpdateInterestsRequest request)
    {
        // Remove existing interests
        var existingInterests = await _context.AuthorInterests
            .Where(ai => ai.AuthorId == GetCurrentUserId())
            .ToListAsync();
        _context.AuthorInterests.RemoveRange(existingInterests);

        // Add new interests
        if (request.CategoryIds != null && request.CategoryIds.Any())
        {
            foreach (var categoryId in request.CategoryIds)
            {
                _context.AuthorInterests.Add(new AuthorInterest
                {
                    AuthorId = GetCurrentUserId(),
                    CategoryId = categoryId
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }
}
