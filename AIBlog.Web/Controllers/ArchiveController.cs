using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIBlog.Web.Data;
using AIBlog.Web.Models;

namespace AIBlog.Web.Controllers;

public class ArchiveController : Controller
{
    private readonly ApplicationDbContext _context;
    private const int PageSize = 15;

    public ArchiveController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId") ?? 0;
    }

    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
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

    public async Task<IActionResult> Index(
        string? search,
        string? timePeriod,
        int? categoryId,
        string sortBy = "newest",
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

        var viewModel = new ArchiveViewModel
        {
            ReadBlogs = blogs,
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
    public async Task<IActionResult> LoadMore(
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
                authorName = b.Author != null ? b.Author.Name : "Unknown",
                summary = b.Summary
            })
            .ToListAsync();

        var hasMore = totalPosts > page * PageSize;

        return Json(new { posts, hasMore, currentPage = page });
    }
}
