namespace AIBlog.Web.Models;

public class WriteViewModel
{
    public string CurrentUserName { get; set; } = "";
    public string? CurrentUserAvatar { get; set; }
    public List<Category> AllCategories { get; set; } = new();
    public int? EditingPostId { get; set; } // For editing existing posts
    public string? ExistingTitle { get; set; }
    public string? ExistingContent { get; set; }
    public int? ExistingCategoryId { get; set; }
}
