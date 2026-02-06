namespace AIBlog.Web.Models;

public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public int CategoryId { get; set; }
    public int ReadCount { get; set; }
    public string Slug { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Author? Author { get; set; }
    public Category? Category { get; set; }

    // Summary stored in database (can be set when creating blog)
    public string? Summary { get; set; }

    // Helper property to get first 30 words for homepage display (if Summary is empty)
    public string DisplaySummary => string.IsNullOrEmpty(Summary) ? GetFirst30Words(Content) : Summary;

    private static string GetFirst30Words(string content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var first30 = words.Take(30);
        var result = string.Join(" ", first30);
        
        if (words.Length > 30)
            result += "...";
            
        return result;
    }
}
