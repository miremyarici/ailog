namespace AIBlog.Web.Models;

public class BlogDetailViewModel
{
    public BlogPost BlogPost { get; set; } = null!;
    public List<Comment> Comments { get; set; } = new();
    public int CurrentUserId { get; set; }
}
