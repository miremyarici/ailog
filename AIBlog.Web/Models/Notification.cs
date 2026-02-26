using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIBlog.Web.Models;

public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public Author? User { get; set; }

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    // e.g., "Follow", "Reply", "NewPost"
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    // Optional URL or ID to redirect the user to when clicking the notification
    [StringLength(255)]
    public string? ReferenceLink { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
