using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIBlog.Web.Data;
using AIBlog.Web.Models;

namespace AIBlog.Web.Controllers;

public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private const int PageSize = 15;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context) : base(context)
    {
        _logger = logger;
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
