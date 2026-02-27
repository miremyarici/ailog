using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIBlog.Web.Data;
using AIBlog.Web.Models;
using AIBlog.Web.Models.Requests;

namespace AIBlog.Web.Controllers;

public class ProfileController : BaseController
{
    private readonly ILogger<ProfileController> _logger;
    private readonly IWebHostEnvironment _environment;

    public ProfileController(ILogger<ProfileController> logger, ApplicationDbContext context, IWebHostEnvironment environment) : base(context)
    {
        _logger = logger;
        _environment = environment;
    }

    // Profile Page
    public async Task<IActionResult> Profile()
    {
        // Get current author (simulating logged-in user)
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == GetCurrentUserId());

        if (author == null)
        {
            return RedirectToAction("Index", "Home");
        }

        // Get user's published blog posts
        var userPosts = await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.AuthorId == GetCurrentUserId() && b.IsPublished)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        // Right sidebar data (same as Home page)
        var mostReadBlogs = await _context.BlogPosts
            .Include(b => b.Author)
            .Where(b => b.IsPublished)
            .OrderByDescending(b => b.ReadCount)
            .Take(3)
            .ToListAsync();

        // Get followed authors to exclude from recommendations
        var followedIds = await _context.Follows
            .Where(f => f.FollowerId == GetCurrentUserId())
            .Select(f => f.FollowingId)
            .ToListAsync();

        var recommendedAuthors = await _context.Authors
            .Where(a => a.Id != GetCurrentUserId() && !followedIds.Contains(a.Id)) // Exclude current user and followed authors from recommendations
            .OrderByDescending(a => a.FollowersCount)
            .Take(5)
            .ToListAsync();

        var viewModel = new ProfileViewModel
        {
            Author = author,
            UserPosts = userPosts,
            MostReadBlogs = mostReadBlogs,
            RecommendedAuthors = recommendedAuthors
        };

        return View(viewModel);
    }

    // Author Public Profile
    public async Task<IActionResult> AuthorProfile(int id)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == id);
        if (author == null)
        {
            return NotFound();
        }

        var currentUserId = GetCurrentUserId();
        var isOwnProfile = currentUserId == id;

        // If it's own profile, redirect to editable Profile page
        if (isOwnProfile)
        {
            return RedirectToAction("Profile");
        }

        var isFollowing = await _context.Follows.AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == id);

        var userPosts = await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.AuthorId == id && b.IsPublished)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var mostReadBlogs = await _context.BlogPosts
            .Include(b => b.Author)
            .Where(b => b.IsPublished)
            .OrderByDescending(b => b.ReadCount)
            .Take(4)
            .ToListAsync();

        var followedIds = await _context.Follows
            .Where(f => f.FollowerId == currentUserId)
            .Select(f => f.FollowingId)
            .ToListAsync();

        var recommendedAuthors = await _context.Authors
            .Where(a => a.Id != currentUserId && a.Id != id && !followedIds.Contains(a.Id))
            .OrderByDescending(a => a.FollowersCount)
            .Take(5)
            .ToListAsync();

        var viewModel = new AuthorProfileViewModel
        {
            Author = author,
            UserPosts = userPosts,
            MostReadBlogs = mostReadBlogs,
            RecommendedAuthors = recommendedAuthors,
            IsFollowing = isFollowing,
            IsOwnProfile = false
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfile(IFormFile? avatar, IFormFile? coverPhoto, string? bio)
    {
        try
        {
            var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == GetCurrentUserId());
            if (author == null) return NotFound();

            string? avatarUrl = null;
            string? coverPhotoUrl = null;

            // Save avatar if provided
            if (avatar != null && avatar.Length > 0)
            {
                var avatarsDir = Path.Combine(_environment.WebRootPath, "images", "avatars");
                if (!Directory.Exists(avatarsDir))
                {
                    Directory.CreateDirectory(avatarsDir);
                }
                var avatarFileName = $"avatar_{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}";
                var avatarPath = Path.Combine(avatarsDir, avatarFileName);
                using (var stream = new FileStream(avatarPath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }
                avatarUrl = $"/images/avatars/{avatarFileName}";
                author.Avatar = avatarUrl;
            }

            // Save cover photo if provided
            if (coverPhoto != null && coverPhoto.Length > 0)
            {
                var headersDir = Path.Combine(_environment.WebRootPath, "images", "headers");
                if (!Directory.Exists(headersDir))
                {
                    Directory.CreateDirectory(headersDir);
                }
                var coverFileName = $"cover_{Guid.NewGuid()}{Path.GetExtension(coverPhoto.FileName)}";
                var coverPath = Path.Combine(headersDir, coverFileName);
                using (var stream = new FileStream(coverPath, FileMode.Create))
                {
                    await coverPhoto.CopyToAsync(stream);
                }
                coverPhotoUrl = $"/images/headers/{coverFileName}";
                author.CoverPhoto = coverPhotoUrl;
            }

            // Update bio
            if (bio != null)
            {
                author.Bio = bio;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, avatarUrl, coverPhotoUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile");
            return Ok(new { success = false, error = ex.Message });
        }
    }

    // Toggle Follow
    [HttpPost]
    public async Task<IActionResult> ToggleFollow([FromBody] ToggleFollowRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == 0) return Unauthorized();

        var existingFollow = await _context.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == request.AuthorId);

        bool isFollowing;

        if (existingFollow != null)
        {
            _context.Follows.Remove(existingFollow);
            isFollowing = false;

            var targetAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.Id == request.AuthorId);
            var currentAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.Id == currentUserId);
            if (targetAuthor != null) targetAuthor.FollowersCount = Math.Max(0, targetAuthor.FollowersCount - 1);
            if (currentAuthor != null) currentAuthor.FollowingCount = Math.Max(0, currentAuthor.FollowingCount - 1);
        }
        else
        {
            _context.Follows.Add(new Follow
            {
                FollowerId = currentUserId,
                FollowingId = request.AuthorId,
                CreatedAt = DateTime.Now
            });
            isFollowing = true;

            var targetAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.Id == request.AuthorId);
            var currentAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.Id == currentUserId);
            if (targetAuthor != null) targetAuthor.FollowersCount++;
            if (currentAuthor != null) currentAuthor.FollowingCount++;
        }

        await _context.SaveChangesAsync();

        var updatedAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.Id == request.AuthorId);

        return Ok(new
        {
            success = true,
            isFollowing,
            followersCount = updatedAuthor?.FollowersCount ?? 0
        });
    }

    // Get Random Author (for recommended authors replacement after follow)
    [HttpGet]
    public async Task<IActionResult> GetRandomAuthor(string excludeIds)
    {
        var currentUserId = GetCurrentUserId();
        var excludeList = new List<int> { currentUserId };

        if (!string.IsNullOrEmpty(excludeIds))
        {
            excludeList.AddRange(excludeIds.Split(',').Select(int.Parse));
        }

        // Get followed authors
        var followedIds = await _context.Follows
            .Where(f => f.FollowerId == currentUserId)
            .Select(f => f.FollowingId)
            .ToListAsync();

        excludeList.AddRange(followedIds);

        var randomAuthor = await _context.Authors
            .Where(a => !excludeList.Contains(a.Id))
            .OrderBy(a => Guid.NewGuid()) // Random order
            .FirstOrDefaultAsync();

        if (randomAuthor == null)
        {
            return Ok(new { success = false });
        }

        return Ok(new
        {
            success = true,
            author = new
            {
                id = randomAuthor.Id,
                name = randomAuthor.Name,
                avatar = randomAuthor.Avatar
            }
        });
    }

    // Get Followers/Following list for popup
    [HttpGet]
    public async Task<IActionResult> GetFollowersList(int authorId, string type)
    {
        List<object> users;

        if (type == "followers")
        {
            users = await _context.Follows
                .Where(f => f.FollowingId == authorId)
                .Include(f => f.Follower)
                .Select(f => new {
                    id = f.Follower!.Id,
                    name = f.Follower.Name,
                    avatar = f.Follower.Avatar
                })
                .Cast<object>()
                .ToListAsync();
        }
        else // following
        {
            users = await _context.Follows
                .Where(f => f.FollowerId == authorId)
                .Include(f => f.Following)
                .Select(f => new {
                    id = f.Following!.Id,
                    name = f.Following.Name,
                    avatar = f.Following.Avatar
                })
                .Cast<object>()
                .ToListAsync();
        }

        return Ok(new { success = true, users, type });
    }
}
