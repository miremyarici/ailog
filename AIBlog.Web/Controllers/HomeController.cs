using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIBlog.Web.Data;
using AIBlog.Web.Models;

namespace AIBlog.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private const int PageSize = 15;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _logger = logger;
        _context = context;
        _environment = environment;
    }

    private int GetCurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId") ?? 0;
    }

    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        
        // Auth guard: redirect to login if not authenticated
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }
        
        var author = _context.Authors.FirstOrDefault(a => a.Id == userId.Value);
        ViewBag.CurrentUserAvatar = author?.Avatar;
        ViewBag.CurrentUserName = author?.Name ?? "User";
    }

    public async Task<IActionResult> Index()
    {
        // Get current user's interests
        var authorInterests = await _context.AuthorInterests
            .Where(ai => ai.AuthorId == GetCurrentUserId())
            .Select(ai => ai.CategoryId)
            .ToListAsync();

        // Query for recommended blogs
        var query = _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.IsPublished);

        // Filter by interests if any are selected
        if (authorInterests.Any())
        {
            query = query.Where(b => authorInterests.Contains(b.CategoryId));
        }

        var recommendedBlogs = await query
            .OrderByDescending(b => b.CreatedAt)
            .Take(PageSize)
            .ToListAsync();

        var mostReadBlogs = await _context.BlogPosts
            .Include(b => b.Author)
            .Where(b => b.IsPublished)
            .OrderByDescending(b => b.ReadCount)
            .Take(3)
            .ToListAsync();

        var currentUserId = GetCurrentUserId();
        var followedIds = await _context.Follows
            .Where(f => f.FollowerId == currentUserId)
            .Select(f => f.FollowingId)
            .ToListAsync();

        var recommendedAuthors = await _context.Authors
            .Where(a => a.Id != currentUserId && !followedIds.Contains(a.Id))
            .OrderByDescending(a => a.FollowersCount)
            .Take(5)
            .ToListAsync();

        var totalPosts = await _context.BlogPosts.CountAsync(b => b.IsPublished);

        var viewModel = new HomeViewModel
        {
            RecommendedBlogs = recommendedBlogs,
            MostReadBlogs = mostReadBlogs,
            RecommendedAuthors = recommendedAuthors,
            CurrentUserName = HttpContext.Session.GetString("UserName") ?? "User",
            HasMorePosts = totalPosts > PageSize,
            CurrentPage = 1,
            TotalPosts = totalPosts
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> LoadMorePosts(int page = 2)
    {
        var skip = (page - 1) * PageSize;
        
        // Get current user's interests
        var authorInterests = await _context.AuthorInterests
            .Where(ai => ai.AuthorId == GetCurrentUserId())
            .Select(ai => ai.CategoryId)
            .ToListAsync();

        var query = _context.BlogPosts
            .Include(b => b.Author)
            .Where(b => b.IsPublished);

        // Filter by interests if any are selected
        if (authorInterests.Any())
        {
            query = query.Where(b => authorInterests.Contains(b.CategoryId));
        }

        var posts = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip(skip)
            .Take(PageSize)
            .Select(b => new {
                id = b.Id,
                title = b.Title,
                authorId = b.AuthorId,
                authorName = b.Author != null ? b.Author.Name : "Unknown",
                summary = b.Summary
            })
            .ToListAsync();

        var totalPosts = await query.CountAsync();
        var hasMore = totalPosts > skip + PageSize;

        return Json(new { posts, hasMore, currentPage = page });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // Write Blog Page
    public async Task<IActionResult> Write(int? id)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == GetCurrentUserId());
        var categories = await _context.Categories.ToListAsync();

        var viewModel = new WriteViewModel
        {
            CurrentUserName = author?.Name ?? "User",
            CurrentUserAvatar = author?.Avatar,
            AllCategories = categories
        };

        // If editing an existing draft
        if (id.HasValue)
        {
            var existingPost = await _context.BlogPosts.FirstOrDefaultAsync(b => b.Id == id.Value && b.AuthorId == GetCurrentUserId());
            if (existingPost != null)
            {
                viewModel.EditingPostId = existingPost.Id;
                viewModel.ExistingTitle = existingPost.Title;
                viewModel.ExistingContent = existingPost.Content;
                viewModel.ExistingCategoryId = existingPost.CategoryId;
            }
        }

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> SaveBlog([FromBody] SaveBlogRequest request)
    {
        try
        {
            // Create summary from content (first 200 characters of plain text)
            var plainText = System.Text.RegularExpressions.Regex.Replace(request.Content, "<.*?>", " ");
            var summary = plainText.Length > 200 ? plainText.Substring(0, 200) + "..." : plainText;

            // Create slug from title
            var slug = request.Title.ToLower()
                .Replace(" ", "-")
                .Replace("'", "")
                .Replace("\"", "");

            BlogPost? blogPost;

            // Update existing post or create new
            if (request.Id.HasValue)
            {
                blogPost = await _context.BlogPosts.FirstOrDefaultAsync(b => b.Id == request.Id.Value && b.AuthorId == GetCurrentUserId());
                if (blogPost == null)
                {
                    return Ok(new { success = false, error = "Post not found." });
                }

                blogPost.Title = request.Title;
                blogPost.Content = request.Content;
                blogPost.Summary = summary.Trim();
                blogPost.Slug = slug;
                blogPost.CategoryId = request.CategoryId;
                blogPost.IsPublished = request.IsPublished;
            }
            else
            {
                blogPost = new BlogPost
                {
                    Title = request.Title,
                    Content = request.Content,
                    Summary = summary.Trim(),
                    Slug = slug,
                    AuthorId = GetCurrentUserId(),
                    CategoryId = request.CategoryId,
                    IsPublished = request.IsPublished,
                    CreatedAt = DateTime.Now,
                    ReadCount = 0
                };

                _context.BlogPosts.Add(blogPost);
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, id = blogPost.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving blog");
            return Ok(new { success = false, error = ex.Message });
        }
    }

    public async Task<IActionResult> Archive(
        string? search,
        string? timePeriod,
        int? categoryId,
        string sortBy = "newest",
        string tab = "read",
        int page = 1)
    {
        // Get all categories for filter
        var categories = await _context.Categories.ToListAsync();

        // Base query - get blogs that user has read (using UserId = 1 for demo)
        var query = _context.ReadHistories
            .Include(r => r.BlogPost)
                .ThenInclude(b => b!.Author)
            .Include(r => r.BlogPost)
                .ThenInclude(b => b!.Category)
            .Where(r => r.UserId == GetCurrentUserId() && r.BlogPost != null)
            .Select(r => r.BlogPost!);

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(b => b.Title.Contains(search) || b.Content.Contains(search));
        }

        // Apply time period filter
        if (!string.IsNullOrEmpty(timePeriod))
        {
            var now = DateTime.Now;
            query = timePeriod switch
            {
                "this-week" => query.Where(b => b.CreatedAt >= now.AddDays(-7)),
                "this-month" => query.Where(b => b.CreatedAt >= now.AddMonths(-1)),
                "this-year" => query.Where(b => b.CreatedAt >= now.AddYears(-1)),
                _ => query
            };
        }

        // Apply category filter
        if (categoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == categoryId.Value);
        }

        // Apply sorting
        query = sortBy switch
        {
            "oldest" => query.OrderBy(b => b.CreatedAt),
            "a-z" => query.OrderBy(b => b.Title),
            "z-a" => query.OrderByDescending(b => b.Title),
            "shortest" => query.OrderBy(b => b.Content.Length),
            "longest" => query.OrderByDescending(b => b.Content.Length),
            "most-popular" => query.OrderByDescending(b => b.ReadCount),
            _ => query.OrderByDescending(b => b.CreatedAt) // newest (default)
        };

        var totalPosts = await query.CountAsync();
        var blogs = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        // Get user's draft (unpublished) blog posts
        var draftBlogs = await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.AuthorId == GetCurrentUserId() && !b.IsPublished)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var viewModel = new ArchiveViewModel
        {
            ReadBlogs = blogs,
            DraftBlogs = draftBlogs,
            Categories = categories,
            CurrentUserName = HttpContext.Session.GetString("UserName") ?? "User",
            ActiveTab = tab,
            SearchQuery = search,
            TimePeriod = timePeriod,
            CategoryId = categoryId,
            SortBy = sortBy,
            HasMorePosts = totalPosts > page * PageSize,
            CurrentPage = page,
            TotalPosts = totalPosts
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> LoadMoreArchive(
        string? search,
        string? timePeriod,
        int? categoryId,
        string sortBy = "newest",
        int page = 2)
    {
        var query = _context.ReadHistories
            .Include(r => r.BlogPost)
                .ThenInclude(b => b!.Author)
            .Where(r => r.UserId == GetCurrentUserId() && r.BlogPost != null)
            .Select(r => r.BlogPost!);

        // Apply same filters
        if (!string.IsNullOrEmpty(search))
            query = query.Where(b => b.Title.Contains(search) || b.Content.Contains(search));

        if (!string.IsNullOrEmpty(timePeriod))
        {
            var now = DateTime.Now;
            query = timePeriod switch
            {
                "this-week" => query.Where(b => b.CreatedAt >= now.AddDays(-7)),
                "this-month" => query.Where(b => b.CreatedAt >= now.AddMonths(-1)),
                "this-year" => query.Where(b => b.CreatedAt >= now.AddYears(-1)),
                _ => query
            };
        }

        if (categoryId.HasValue)
            query = query.Where(b => b.CategoryId == categoryId.Value);

        query = sortBy switch
        {
            "oldest" => query.OrderBy(b => b.CreatedAt),
            "a-z" => query.OrderBy(b => b.Title),
            "z-a" => query.OrderByDescending(b => b.Title),
            "shortest" => query.OrderBy(b => b.Content.Length),
            "longest" => query.OrderByDescending(b => b.Content.Length),
            "most-popular" => query.OrderByDescending(b => b.ReadCount),
            _ => query.OrderByDescending(b => b.CreatedAt)
        };

        var totalPosts = await query.CountAsync();
        var posts = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(b => new {
                id = b.Id,
                title = b.Title,
                authorId = b.AuthorId,
                authorName = b.Author != null ? b.Author.Name : "Unknown",
                summary = b.Summary
            })
            .ToListAsync();

        var hasMore = totalPosts > page * PageSize;

        return Json(new { posts, hasMore, currentPage = page });
    }

    public async Task<IActionResult> Explore(
        string? search,
        string? timePeriod,
        string? customDate,
        int? categoryId,
        string sortBy = "most-popular",
        int page = 1)
    {
        // Get all categories for filter
        var categories = await _context.Categories.ToListAsync();

        // Base query - get ALL published blogs (unlike Archive which shows only read blogs)
        var query = _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.IsPublished);

        // Apply search filter - searches across ALL blogs
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(b => b.Title.Contains(search) || b.Content.Contains(search));
        }

        // Apply time period filter
        if (!string.IsNullOrEmpty(timePeriod))
        {
            var now = DateTime.Now;
            if (timePeriod == "custom" && !string.IsNullOrEmpty(customDate))
            {
                // Parse custom date (format: yyyy-MM-dd)
                if (DateTime.TryParse(customDate, out var selectedDate))
                {
                    query = query.Where(b => b.CreatedAt.Date == selectedDate.Date);
                }
            }
            else
            {
                query = timePeriod switch
                {
                    "this-week" => query.Where(b => b.CreatedAt >= now.AddDays(-7)),
                    "this-month" => query.Where(b => b.CreatedAt >= now.AddMonths(-1)),
                    "this-year" => query.Where(b => b.CreatedAt >= now.AddYears(-1)),
                    _ => query
                };
            }
        }

        // Apply category filter
        if (categoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == categoryId.Value);
        }

        // Apply sorting - default is most-popular (ReadCount descending)
        query = sortBy switch
        {
            "newest" => query.OrderByDescending(b => b.CreatedAt),
            "oldest" => query.OrderBy(b => b.CreatedAt),
            "a-z" => query.OrderBy(b => b.Title),
            "z-a" => query.OrderByDescending(b => b.Title),
            "shortest" => query.OrderBy(b => b.Content.Length),
            "longest" => query.OrderByDescending(b => b.Content.Length),
            _ => query.OrderByDescending(b => b.ReadCount) // most-popular (default)
        };

        var totalPosts = await query.CountAsync();
        var blogs = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var viewModel = new ExploreViewModel
        {
            TrendingBlogs = blogs,
            Categories = categories,
            CurrentUserName = HttpContext.Session.GetString("UserName") ?? "User",
            SearchQuery = search,
            TimePeriod = timePeriod,
            CategoryId = categoryId,
            SortBy = sortBy,
            HasMorePosts = totalPosts > page * PageSize,
            CurrentPage = page,
            TotalPosts = totalPosts
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> LoadMoreExplore(
        string? search,
        string? timePeriod,
        int? categoryId,
        string sortBy = "most-popular",
        int page = 2)
    {
        var query = _context.BlogPosts
            .Include(b => b.Author)
            .Where(b => b.IsPublished);

        // Apply same filters
        if (!string.IsNullOrEmpty(search))
            query = query.Where(b => b.Title.Contains(search) || b.Content.Contains(search));

        if (!string.IsNullOrEmpty(timePeriod))
        {
            var now = DateTime.Now;
            query = timePeriod switch
            {
                "this-week" => query.Where(b => b.CreatedAt >= now.AddDays(-7)),
                "this-month" => query.Where(b => b.CreatedAt >= now.AddMonths(-1)),
                "this-year" => query.Where(b => b.CreatedAt >= now.AddYears(-1)),
                _ => query
            };
        }

        if (categoryId.HasValue)
            query = query.Where(b => b.CategoryId == categoryId.Value);

        query = sortBy switch
        {
            "newest" => query.OrderByDescending(b => b.CreatedAt),
            "oldest" => query.OrderBy(b => b.CreatedAt),
            "a-z" => query.OrderBy(b => b.Title),
            "z-a" => query.OrderByDescending(b => b.Title),
            "shortest" => query.OrderBy(b => b.Content.Length),
            "longest" => query.OrderByDescending(b => b.Content.Length),
            _ => query.OrderByDescending(b => b.ReadCount)
        };

        var totalPosts = await query.CountAsync();
        var posts = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(b => new {
                id = b.Id,
                title = b.Title,
                authorId = b.AuthorId,
                authorName = b.Author != null ? b.Author.Name : "Unknown",
                summary = b.Summary
            })
            .ToListAsync();

        var hasMore = totalPosts > page * PageSize;

        return Json(new { posts, hasMore, currentPage = page });
    }

    // Profile Page
    public async Task<IActionResult> Profile()
    {
        // Get current author (simulating logged-in user)
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == GetCurrentUserId());

        if (author == null)
        {
            return RedirectToAction("Index");
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

        var recommendedAuthors = await _context.Authors
            .Where(a => a.Id != GetCurrentUserId()) // Exclude current user from recommendations
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

    public async Task<IActionResult> Settings()
    {
        // Get current author (simulating logged-in user)
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == GetCurrentUserId());
        
        if (author == null)
        {
            return RedirectToAction("Index");
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
        
        // Get user's selected interests
        var selectedInterests = await _context.AuthorInterests
            .Where(ai => ai.AuthorId == GetCurrentUserId())
            .Select(ai => ai.CategoryId)
            .ToListAsync();

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
            SelectedCategoryIds = selectedInterests
        };

        return View(viewModel);
    }

    // ========================================
    // Settings Update API Endpoints
    // ========================================

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

    // Blog Detail Page
    public async Task<IActionResult> BlogDetail(int id)
    {
        var blogPost = await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (blogPost == null)
        {
            return NotFound();
        }

        // Increment read count
        blogPost.ReadCount++;

        // Record read history
        var userId = GetCurrentUserId();
        var existingHistory = await _context.ReadHistories
            .FirstOrDefaultAsync(r => r.UserId == userId && r.BlogPostId == id);
        if (existingHistory == null)
        {
            _context.ReadHistories.Add(new ReadHistory
            {
                UserId = userId,
                BlogPostId = id,
                ReadAt = DateTime.Now,
                ReadProgress = 100
            });
        }
        else
        {
            existingHistory.ReadAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        // Get top-level comments with replies
        var comments = await _context.Comments
            .Include(c => c.Author)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Author)
            .Where(c => c.BlogPostId == id && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var viewModel = new BlogDetailViewModel
        {
            BlogPost = blogPost,
            Comments = comments,
            CurrentUserId = userId
        };

        return View(viewModel);
    }

    // Add Comment
    [HttpPost]
    public async Task<IActionResult> AddComment([FromBody] AddCommentRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var comment = new Comment
        {
            BlogPostId = request.BlogPostId,
            AuthorId = userId,
            Content = request.Content,
            ParentCommentId = request.ParentCommentId,
            CreatedAt = DateTime.Now
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == userId);

        return Ok(new
        {
            success = true,
            comment = new
            {
                id = comment.Id,
                content = comment.Content,
                authorName = author?.Name ?? "User",
                authorAvatar = author?.Avatar,
                blogPostId = comment.BlogPostId,
                createdAt = comment.CreatedAt.ToString("MMM dd, yyyy")
            }
        });
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

// Request models
public class UpdateEmailRequest { public string Email { get; set; } = ""; }
public class UpdatePhoneRequest { public string Phone { get; set; } = ""; }
public class UpdatePasswordRequest { public string CurrentPassword { get; set; } = ""; public string NewPassword { get; set; } = ""; }
public class UpdateTwoFactorRequest { public bool Enabled { get; set; } }
public class UpdateVisibilityRequest { public string Visibility { get; set; } = "Public"; }
public class UpdateSearchVisibilityRequest { public bool Visible { get; set; } }
public class UpdateInterestsRequest { public List<int> CategoryIds { get; set; } = new(); }
public class SaveBlogRequest 
{ 
    public int? Id { get; set; }
    public string Title { get; set; } = ""; 
    public string Content { get; set; } = ""; 
    public int CategoryId { get; set; }
    public bool IsPublished { get; set; }
}
public class AddCommentRequest
{
    public int BlogPostId { get; set; }
    public string Content { get; set; } = "";
    public int? ParentCommentId { get; set; }
}
public class ToggleFollowRequest
{
    public int AuthorId { get; set; }
}

