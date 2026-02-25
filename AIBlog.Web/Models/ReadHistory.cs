namespace AIBlog.Web.Models;

public class ReadHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BlogPostId { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.Now;
    public int ReadProgress { get; set; } = 100;

    // Navigation property
    public BlogPost? BlogPost { get; set; }
}
