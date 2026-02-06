namespace AIBlog.Web.Models;

public class ArchiveViewModel
{
    public List<BlogPost> ReadBlogs { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public string CurrentUserName { get; set; } = "Diane Merlotte";
    public string? CurrentUserAvatar { get; set; }
    
    // Filter parameters
    public string? SearchQuery { get; set; }
    public string? TimePeriod { get; set; } // this-week, this-month, this-year, custom
    public int? CategoryId { get; set; }
    
    // Sort parameter
    public string SortBy { get; set; } = "newest"; // newest, oldest, a-z, z-a, shortest, longest, most-popular
    
    public bool HasMorePosts { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPosts { get; set; }
}
