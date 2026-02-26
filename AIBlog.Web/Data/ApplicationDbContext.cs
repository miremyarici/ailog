using Microsoft.EntityFrameworkCore;
using AIBlog.Web.Models;

namespace AIBlog.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ReadHistory> ReadHistories { get; set; }
    public DbSet<AuthorInterest> AuthorInterests { get; set; }
    public DbSet<AuthorSession> AuthorSessions { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    public DbSet<Comment> Comments { get; set; }
    public DbSet<Follow> Follows { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Assembly içindeki tüm IEntityTypeConfiguration sınıflarını otomatik bulur ve uygular
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
