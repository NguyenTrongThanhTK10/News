using System.ComponentModel.DataAnnotations;
using News.Domain;

namespace News.Application;

public class ArticleQuery
{
    [StringLength(200)] public string? Search { get; init; }
    [StringLength(80)] public string? Category { get; init; }
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 30)] public int PageSize { get; init; } = 9;
}

public sealed class AdminArticleQuery : ArticleQuery
{
    public ArticleStatus? Status { get; init; }
}

public record ArticleSummary(int Id, string Title, string Summary, string CategorySlug, string Category,
    string ImageUrl, DateTime? PublishedAt, string Author, int ReadingMinutes)
{
    public static ArticleSummary FromArticle(Article article) => new(article.Id, article.Title, article.Summary,
        article.CategorySlug, article.Category, article.ImageUrl, Utc(article.PublishedAt), article.Author, article.ReadingMinutes);
    public static DateTime? Utc(DateTime? date) => date is null ? null : DateTime.SpecifyKind(date.Value, DateTimeKind.Utc);
}

public sealed record ArticleDetail(int Id, string Title, string Summary, string Content, string CategorySlug,
    string Category, string ImageUrl, DateTime? PublishedAt, string Author, int ReadingMinutes)
{
    public static ArticleDetail FromArticle(Article a) => new(a.Id, a.Title, a.Summary, a.Content, a.CategorySlug,
        a.Category, a.ImageUrl, ArticleSummary.Utc(a.PublishedAt), a.Author, a.ReadingMinutes);
}

public sealed record AdminArticleDto(ArticleDetail Article, ArticleStatus Status, Guid AuthorId,
    Guid Version, string ReviewNote, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static AdminArticleDto FromArticle(Article a) => new(ArticleDetail.FromArticle(a), a.Status,
        a.AuthorId, a.Version, a.ReviewNote, DateTime.SpecifyKind(a.CreatedAt, DateTimeKind.Utc),
        DateTime.SpecifyKind(a.UpdatedAt, DateTimeKind.Utc));
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
public sealed record CategoryDto(string Slug, string Name, int Count);
public sealed record StaffActor(Guid Id, string Name, string Role)
{
    public bool CanReview => Role is Roles.Admin or Roles.Editor;
}

public sealed class SaveArticleRequest
{
    [Required, StringLength(200, MinimumLength = 8)] public string Title { get; init; } = "";
    [Required, StringLength(600, MinimumLength = 20)] public string Summary { get; init; } = "";
    [Required, StringLength(100_000)] public string Content { get; init; } = "";
    [Required, StringLength(80)] public string CategorySlug { get; init; } = "";
    [Required, StringLength(2000), Url] public string ImageUrl { get; init; } = "";
    [Range(1, 60)] public int ReadingMinutes { get; init; } = 3;
    public Guid? Version { get; init; }
}

public sealed class TransitionRequest
{
    [Required] public Guid? Version { get; init; }
    [StringLength(1000)] public string? Note { get; init; }
}

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; init; } = "";
    [Required, StringLength(200)] public string Password { get; init; } = "";
}
public sealed record UserDto(Guid Id, string Name, string Email, string Role);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);
public sealed record AuthSessionResult(AuthResponse Response, string RefreshToken, DateTime RefreshExpiresAt);
