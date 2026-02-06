namespace AIBlog.Web.Models;

public class AuthorInterest
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public int CategoryId { get; set; }
    
    public Author? Author { get; set; }
    public Category? Category { get; set; }
}
