namespace AIBlog.Web.Models;

public class ReadHistory
{
    public int Id { get; set; }
    public int UserId { get; set; } // Simplified - using 1 as default user for now
    public int BlogPostId { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.Now;
    public int ReadProgress { get; set; } = 100; // Percentage of article read

    // Navigation property
    public BlogPost? BlogPost { get; set; }
}
