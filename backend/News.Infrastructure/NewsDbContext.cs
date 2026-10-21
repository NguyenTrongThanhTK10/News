using Microsoft.EntityFrameworkCore;
using News.Domain;

namespace News.Infrastructure;

public sealed class NewsDbContext(DbContextOptions<NewsDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RoleDefinition> RoleDefinitions => Set<RoleDefinition>();
    public DbSet<LoginSession> Sessions => Set<LoginSession>();
    public DbSet<ArticleComment> Comments => Set<ArticleComment>();
    public DbSet<ArticleRating> Ratings => Set<ArticleRating>();

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
        user.Property(u => u.GoogleSubject).HasMaxLength(255);
        user.HasIndex(u => u.GoogleSubject).IsUnique().HasFilter("[GoogleSubject] IS NOT NULL");
        user.Property(u => u.Role).HasMaxLength(20).IsRequired();
        user.Property(u => u.RoleDefinitionId).HasMaxLength(80);
        user.HasOne<RoleDefinition>().WithMany().HasForeignKey(u => u.RoleDefinitionId).OnDelete(DeleteBehavior.Restrict);
        user.Property(u => u.Version).IsConcurrencyToken();

        var role = model.Entity<RoleDefinition>();
        role.HasKey(r => r.Id);
        role.Property(r => r.Id).HasMaxLength(80);
        role.Property(r => r.Name).HasMaxLength(80).IsRequired();
        role.HasIndex(r => r.Name).IsUnique();
        role.Property(r => r.BaseRole).HasMaxLength(20).IsRequired();
        role.HasData(
            new RoleDefinition { Id = Roles.Reader, Name = "Độc giả", BaseRole = Roles.Reader },
            new RoleDefinition { Id = Roles.Admin, Name = "Quản trị viên", BaseRole = Roles.Admin },
            new RoleDefinition { Id = Roles.Editor, Name = "Biên tập viên", BaseRole = Roles.Editor },
            new RoleDefinition { Id = Roles.Author, Name = "Tác giả", BaseRole = Roles.Author });

        var session = model.Entity<LoginSession>();
        session.HasKey(s => s.Id);
        session.Property(s => s.RefreshTokenHash).HasMaxLength(64).IsRequired();
        session.HasIndex(s => s.RefreshTokenHash).IsUnique();
        session.Property(s => s.Version).IsConcurrencyToken();
        session.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);

        var comment = model.Entity<ArticleComment>();
        comment.HasKey(c => c.Id);
        comment.Property(c => c.DisplayName).HasMaxLength(100).IsRequired();
        comment.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
        comment.Property(c => c.Content).HasMaxLength(2000).IsRequired();
        comment.HasIndex(c => new { c.ArticleId, c.CreatedAt, c.Id });
        comment.HasOne<Article>().WithMany().HasForeignKey(c => c.ArticleId).OnDelete(DeleteBehavior.Cascade);

        var rating = model.Entity<ArticleRating>();
        rating.HasKey(r => new { r.ArticleId, r.VisitorKey });
        rating.Property(r => r.VisitorKey).HasMaxLength(64);
        rating.HasOne<Article>().WithMany().HasForeignKey(r => r.ArticleId).OnDelete(DeleteBehavior.Cascade);
        rating.ToTable(t => t.HasCheckConstraint("CK_Ratings_Stars", "[Stars] BETWEEN 1 AND 5"));
    }
}
