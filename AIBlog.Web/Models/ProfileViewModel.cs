namespace AIBlog.Web.Models;

public class ProfileViewModel
{
    // Current user info
    public Author Author { get; set; } = new();
    
    // User's published blog posts
    public List<BlogPost> UserPosts { get; set; } = new();
    
    // Right sidebar data (same as Home page)
    public List<BlogPost> MostReadBlogs { get; set; } = new();
    public List<Author> RecommendedAuthors { get; set; } = new();
}
