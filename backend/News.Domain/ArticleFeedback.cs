namespace News.Domain;

public sealed class ArticleComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ArticleId { get; set; }
    public Guid? UserId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ArticleRating
{
    public int ArticleId { get; set; }
    public string VisitorKey { get; set; } = "";
    public int Stars { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed record CommentPage(IReadOnlyList<ArticleComment> Items, int TotalCount);
public sealed record RatingStatistics(double Average, int Count, int? MyRating, IReadOnlyList<int> Distribution);

public interface IArticleFeedbackRepository
{
    Task<CommentPage> GetCommentsAsync(int articleId, int page, int pageSize, CancellationToken ct);
    Task<ArticleComment> AddCommentAsync(ArticleComment comment, CancellationToken ct);
    Task<RatingStatistics> GetRatingsAsync(int articleId, string? visitorKey, CancellationToken ct);
    Task SetRatingAsync(int articleId, string visitorKey, int stars, CancellationToken ct);
}
