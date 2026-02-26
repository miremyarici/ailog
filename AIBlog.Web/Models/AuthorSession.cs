namespace AIBlog.Web.Models;

public class AuthorSession
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime LastActive { get; set; } = DateTime.Now;
    public bool IsCurrentDevice { get; set; } = false;
    public bool IsRevoked { get; set; } = false;
    
    // Navigation property
    public Author? Author { get; set; }
}
