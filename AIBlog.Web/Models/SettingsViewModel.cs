namespace AIBlog.Web.Models;

public class SettingsViewModel
{
    // User info
    public string CurrentUserName { get; set; } = string.Empty;
    public string? CurrentUserAvatar { get; set; }
    
    // Account info (masked for display)
    public string MaskedEmail { get; set; } = string.Empty;
    public string MaskedPhone { get; set; } = string.Empty;
    public string MaskedPassword { get; set; } = "••••••••";
    
    // Settings
    public string Theme { get; set; } = "System Default";
    public bool TwoFactorEnabled { get; set; }
    public string ProfileVisibility { get; set; } = "Public";
    public bool SearchEngineVisibility { get; set; } = true;
    
    // Categories for interests
    public List<Category> AllCategories { get; set; } = new();
    public List<int> SelectedCategoryIds { get; set; } = new();

    public List<AuthorSession> ActiveSessions { get; set; } = new();
}