namespace AIBlog.Web.Models;

public class Comment
{
    public int Id { get; set; }
    public int BlogPostId { get; set; }
    public int AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Self-referencing for replies
    public int? ParentCommentId { get; set; }
    
    // Navigation properties
    public BlogPost? BlogPost { get; set; }
    public Author? Author { get; set; }
    public Comment? ParentComment { get; set; }
    public List<Comment> Replies { get; set; } = new();
}
