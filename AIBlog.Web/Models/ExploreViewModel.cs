namespace AIBlog.Web.Models;

public class ExploreViewModel
{
    public List<BlogPost> TrendingBlogs { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public string CurrentUserName { get; set; } = "Diane Merlotte";
    public string? CurrentUserAvatar { get; set; }
    
    // Filter parameters
    public string? SearchQuery { get; set; }
    public string? TimePeriod { get; set; } // this-week, this-month, this-year, custom
    public int? CategoryId { get; set; }
    
    // Sort parameter - default to most-popular for trending
    public string SortBy { get; set; } = "most-popular"; // most-popular, newest, oldest, a-z, z-a, shortest, longest
    
    public bool HasMorePosts { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPosts { get; set; }
}
