using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AIBlog.Web.Models;

namespace AIBlog.Web.Data.Configurations;

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("BlogPosts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Content).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(500);
        
        builder.HasOne(e => e.Author)
               .WithMany()
               .HasForeignKey(e => e.AuthorId);
              
        builder.HasOne(e => e.Category)
               .WithMany()
               .HasForeignKey(e => e.CategoryId);
    }
}