using Microsoft.EntityFrameworkCore;
using News.Domain;

namespace News.Infrastructure;

public sealed class NewsDbContext(DbContextOptions<NewsDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();
    public DbSet<LoginSession> Sessions => Set<LoginSession>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var article = model.Entity<Article>();
        article.HasKey(a => a.Id);
        article.Property(a => a.Title).HasMaxLength(200).IsRequired();
        article.Property(a => a.Summary).HasMaxLength(600).IsRequired();
        article.Property(a => a.Content).IsRequired();
        article.Property(a => a.CategorySlug).HasMaxLength(80).IsRequired();
        article.Property(a => a.Category).HasMaxLength(80).IsRequired();
        article.Property(a => a.ImageUrl).HasMaxLength(2000).IsRequired();
        article.Property(a => a.Author).HasMaxLength(100).IsRequired();
        article.Property(a => a.SearchText).HasMaxLength(1200).IsRequired();
        article.Property(a => a.ReviewNote).HasMaxLength(1000).IsRequired();
        article.Property(a => a.Status).HasConversion<string>().HasMaxLength(30);
        article.Property(a => a.Version).IsConcurrencyToken();
        article.HasIndex(a => new { a.IsDeleted, a.Status, a.PublishedAt });
        article.HasIndex(a => new { a.AuthorId, a.Status });
        article.HasOne<Category>().WithMany().HasForeignKey(a => a.CategorySlug).OnDelete(DeleteBehavior.Restrict);
        article.HasOne<User>().WithMany().HasForeignKey(a => a.AuthorId).OnDelete(DeleteBehavior.Restrict);

        var category = model.Entity<Category>();
        category.HasKey(c => c.Slug);
        category.Property(c => c.Slug).HasMaxLength(80);
        category.Property(c => c.Name).HasMaxLength(80).IsRequired();

        var user = model.Entity<User>();
        user.HasKey(u => u.Id);
        user.Property(u => u.Email).HasMaxLength(254).IsRequired();
        user.HasIndex(u => u.Email).IsUnique();
        user.Property(u => u.Name).HasMaxLength(100).IsRequired();
        user.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        user.Property(u => u.Role).HasMaxLength(20).IsRequired();
        user.Property(u => u.Version).IsConcurrencyToken();

        var session = model.Entity<LoginSession>();
        session.HasKey(s => s.Id);
        session.Property(s => s.RefreshTokenHash).HasMaxLength(64).IsRequired();
        session.HasIndex(s => s.RefreshTokenHash).IsUnique();
        session.Property(s => s.Version).IsConcurrencyToken();
        session.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
