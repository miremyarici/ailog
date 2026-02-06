namespace AIBlog.Web.Models;

public class HomeViewModel
{
    public List<BlogPost> RecommendedBlogs { get; set; } = new();
    public List<BlogPost> MostReadBlogs { get; set; } = new();
    public List<Author> RecommendedAuthors { get; set; } = new();
    public string CurrentUserName { get; set; } = "Diane Merlotte";
    public string? CurrentUserAvatar { get; set; }
    public bool HasMorePosts { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPosts { get; set; }
}
