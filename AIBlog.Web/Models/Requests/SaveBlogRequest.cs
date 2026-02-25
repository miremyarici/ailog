namespace AIBlog.Web.Models.Requests;

public class SaveBlogRequest 
{ 
    public int? Id { get; set; }
    public string Title { get; set; } = string.Empty; 
    public string Content { get; set; } = string.Empty; 
    public int CategoryId { get; set; }
    public bool IsPublished { get; set; }
}
