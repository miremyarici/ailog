namespace AIBlog.Web.Models;

public class AuthorProfileViewModel
{
    public Author Author { get; set; } = null!;
    public List<BlogPost> UserPosts { get; set; } = new();
    public List<BlogPost> MostReadBlogs { get; set; } = new();
    public List<Author> RecommendedAuthors { get; set; } = new();
    public bool IsFollowing { get; set; }
    public bool IsOwnProfile { get; set; }
}
