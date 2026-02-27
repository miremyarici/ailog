using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIBlog.Web.Data;
using AIBlog.Web.Models;
using AIBlog.Web.Models.Requests;

namespace AIBlog.Web.Controllers;

public class BlogController : BaseController
{
    private readonly ILogger<BlogController> _logger;
    private const int PageSize = 15;

    public BlogController(ILogger<BlogController> logger, ApplicationDbContext context) : base(context)
    {
        _logger = logger;
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
            var plainText = request.Content.Replace("&nbsp;", " ").Replace("\u00A0", " ");
            plainText = System.Text.RegularExpressions.Regex.Replace(plainText, "<.*?>", " ");
            plainText = System.Net.WebUtility.HtmlDecode(plainText);
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

                // If published directly, notify followers
                if (request.IsPublished)
                {
                    var authorName = HttpContext.Session.GetString("UserName") ?? "Someone";
                    var currentUserId = GetCurrentUserId();
                    var followerIds = await _context.Follows
                        .Where(f => f.FollowingId == currentUserId)
                        .Select(f => f.FollowerId)
                        .ToListAsync();

                    var notifications = followerIds.Select(fId => new Notification
                    {
                        UserId = fId,
                        Message = $"{authorName} published a new post: {request.Title}",
                        Type = "NewPost",
                        ReferenceLink = $"/Blog/BlogDetail/{blogPost.Id}",
                        CreatedAt = DateTime.Now
                    });

                    _context.Notifications.AddRange(notifications);
                }
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

        // Optional: Notify if it's a reply
        if (request.ParentCommentId.HasValue)
        {
            var parentComment = await _context.Comments.FindAsync(request.ParentCommentId.Value);
            if (parentComment != null && parentComment.AuthorId != userId)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = parentComment.AuthorId,
                    Message = $"{author?.Name ?? "Someone"} replied to your comment.",
                    Type = "Reply",
                    ReferenceLink = $"/Blog/BlogDetail/{request.BlogPostId}",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
        }


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

    [HttpGet]
    public async Task<IActionResult> CheckAiStatus([FromServices] AIBlog.Web.Services.WordPredictionService aiService)
    {
        var isHealthy = await aiService.IsServiceHealthyAsync();
        return Json(new { isHealthy });
    }

    [HttpPost]
    public async Task<IActionResult> GetAiPrediction([FromBody] AIBlog.Web.Models.Requests.AiPredictionRequest request, [FromServices] AIBlog.Web.Services.WordPredictionService aiService)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return Json(new { success = false, predictions = new List<string>() });

        var response = await aiService.GetPredictionsAsync(request.Text, request.Count);
        
        return Json(new { 
            success = response.Success, 
            predictions = response.Predictions 
        });
    }
}
