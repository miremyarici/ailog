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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // BlogPost configuration
        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.ToTable("BlogPosts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(500);
            
            entity.HasOne(e => e.Author)
                  .WithMany()
                  .HasForeignKey(e => e.AuthorId);
                  
            entity.HasOne(e => e.Category)
                  .WithMany()
                  .HasForeignKey(e => e.CategoryId);
        });

        // Author configuration
        modelBuilder.Entity<Author>(entity =>
        {
            entity.ToTable("Authors");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Slug).HasMaxLength(100);
        });

        // ReadHistory configuration
        modelBuilder.Entity<ReadHistory>(entity =>
        {
            entity.ToTable("ReadHistories");
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.BlogPost)
                  .WithMany()
                  .HasForeignKey(e => e.BlogPostId);
        });

        // AuthorInterest configuration
        modelBuilder.Entity<AuthorInterest>(entity =>
        {
            entity.ToTable("AuthorInterests");
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Author)
                  .WithMany()
                  .HasForeignKey(e => e.AuthorId);
                  
            entity.HasOne(e => e.Category)
                  .WithMany()
                  .HasForeignKey(e => e.CategoryId);
        });
    }
}
