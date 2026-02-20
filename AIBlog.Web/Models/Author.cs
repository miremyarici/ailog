namespace AIBlog.Web.Models;

public class Author
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public string? CoverPhoto { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Account fields
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public string? PhoneNumber { get; set; }
    
    // Settings fields
    public bool? TwoFactorEnabled { get; set; } = false;
    public string? ProfileVisibility { get; set; } = "Public";
    public bool? SearchEngineVisibility { get; set; } = true;
}
