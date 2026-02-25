namespace AIBlog.Web.Models.Requests;

public class AddCommentRequest
{
    public int BlogPostId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? ParentCommentId { get; set; }
}
