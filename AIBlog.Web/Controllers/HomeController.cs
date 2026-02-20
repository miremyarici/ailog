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
    private const int PageSize = 15;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var author = _context.Authors.FirstOrDefault(a => a.Id == 1);
        ViewBag.CurrentUserAvatar = author?.Avatar;
    }

    public async Task<IActionResult> Index()
    {
        // Get current user's interests
        var authorInterests = await _context.AuthorInterests
            .Where(ai => ai.AuthorId == 1)
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

        var recommendedAuthors = await _context.Authors
            .OrderByDescending(a => a.FollowersCount)
            .Take(5)
            .ToListAsync();

        var totalPosts = await _context.BlogPosts.CountAsync(b => b.IsPublished);

        var viewModel = new HomeViewModel
        {
            RecommendedBlogs = recommendedBlogs,
            MostReadBlogs = mostReadBlogs,
            RecommendedAuthors = recommendedAuthors,
            CurrentUserName = "Diane Merlotte",
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
            .Where(ai => ai.AuthorId == 1)
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
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == 1);
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
            var existingPost = await _context.BlogPosts.FirstOrDefaultAsync(b => b.Id == id.Value && b.AuthorId == 1);
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
                blogPost = await _context.BlogPosts.FirstOrDefaultAsync(b => b.Id == request.Id.Value && b.AuthorId == 1);
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
                    AuthorId = 1, // Current user
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
            .Where(r => r.UserId == 1 && r.BlogPost != null)
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
            .Where(b => b.AuthorId == 1 && !b.IsPublished)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var viewModel = new ArchiveViewModel
        {
            ReadBlogs = blogs,
            DraftBlogs = draftBlogs,
            Categories = categories,
            CurrentUserName = "Diane Merlotte",
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
            .Where(r => r.UserId == 1 && r.BlogPost != null)
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
            CurrentUserName = "Diane Merlotte",
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
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == 1);

        if (author == null)
        {
            return RedirectToAction("Index");
        }

        // Get user's published blog posts
        var userPosts = await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.AuthorId == 1 && b.IsPublished)
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
            .Where(a => a.Id != 1) // Exclude current user from recommendations
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

    public async Task<IActionResult> Settings()
    {
        // Get current author (simulating logged-in user)
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == 1);
        
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
            .Where(ai => ai.AuthorId == 1)
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
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == 1);
        if (author == null) return NotFound();

        author.Email = request.Email;
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePhone([FromBody] UpdatePhoneRequest request)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == 1);
        if (author == null) return NotFound();

        author.PhoneNumber = request.Phone;
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == 1);
        if (author == null) return NotFound();

        // In production, verify current password and hash new password
        author.PasswordHash = request.NewPassword; // Should be hashed in production
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTwoFactor([FromBody] UpdateTwoFactorRequest request)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == 1);
        if (author == null) return NotFound();

        author.TwoFactorEnabled = request.Enabled;
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfileVisibility([FromBody] UpdateVisibilityRequest request)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == 1);
        if (author == null) return NotFound();

        author.ProfileVisibility = request.Visibility;
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSearchVisibility([FromBody] UpdateSearchVisibilityRequest request)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == 1);
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
            .Where(ai => ai.AuthorId == 1)
            .ToListAsync();
        _context.AuthorInterests.RemoveRange(existingInterests);

        // Add new interests
        if (request.CategoryIds != null && request.CategoryIds.Any())
        {
            foreach (var categoryId in request.CategoryIds)
            {
                _context.AuthorInterests.Add(new AuthorInterest
                {
                    AuthorId = 1,
                    CategoryId = categoryId
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
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

