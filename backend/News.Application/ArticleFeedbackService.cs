using System.ComponentModel.DataAnnotations;
using News.Domain;

namespace News.Application;

public sealed class CommentQuery
{
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 30)] public int PageSize { get; init; } = 10;
}

public sealed class CreateCommentRequest
{
    [Required, StringLength(80, MinimumLength = 2)] public string DisplayName { get; init; } = "";
    [Required, StringLength(2000, MinimumLength = 5)] public string Content { get; init; } = "";
}

public sealed class RateArticleRequest
{
    [Range(1, 5)] public int Stars { get; init; }
}

public sealed record CommentDto(Guid Id, string DisplayName, string Content, DateTime CreatedAt)
{
    public static CommentDto FromComment(ArticleComment comment) => new(comment.Id, comment.DisplayName,
        comment.Content, DateTime.SpecifyKind(comment.CreatedAt, DateTimeKind.Utc));
}

public sealed class ArticleFeedbackService(IArticleRepository articles, IArticleFeedbackRepository feedback)
{
    private async Task RequirePublishedArticle(int id, CancellationToken ct)
    {
        if (await articles.GetByIdAsync(id, ct) is not { Status: ArticleStatus.Published, IsDeleted: false })
            throw new BusinessException(404, "Không tìm thấy bài viết hoặc bài viết chưa công khai.");
    }

    public async Task<PagedResult<CommentDto>> GetCommentsAsync(int id, CommentQuery query, CancellationToken ct)
    {
        await RequirePublishedArticle(id, ct);
        if (query.Page < 1 || query.PageSize is < 1 or > 30)
            throw new BusinessException(400, "Phân trang bình luận không hợp lệ.");
        var page = await feedback.GetCommentsAsync(id, query.Page, query.PageSize, ct);
        return new(page.Items.Select(CommentDto.FromComment).ToArray(), page.TotalCount, query.Page, query.PageSize);
    }

    public async Task<CommentDto> AddCommentAsync(int id, CreateCommentRequest request, CancellationToken ct)
    {
        await RequirePublishedArticle(id, ct);
        var name = request.DisplayName?.Trim() ?? "";
        var content = request.Content?.Trim() ?? "";
        if (name.Length is < 2 or > 80 || content.Length is < 5 or > 2000)
            throw new BusinessException(400, "Tên hiển thị cần 2–80 ký tự, bình luận cần 5–2000 ký tự sau khi bỏ khoảng trắng.");
        var comment = await feedback.AddCommentAsync(new ArticleComment { ArticleId = id, DisplayName = name, Content = content }, ct);
        return CommentDto.FromComment(comment);
    }

    public async Task<RatingStatistics> GetRatingsAsync(int id, string? visitorKey, CancellationToken ct)
    {
        await RequirePublishedArticle(id, ct);
        return await feedback.GetRatingsAsync(id, visitorKey, ct);
    }

    public async Task<RatingStatistics> RateAsync(int id, string visitorKey, RateArticleRequest request, CancellationToken ct)
    {
        await RequirePublishedArticle(id, ct);
        if (request.Stars is < 1 or > 5) throw new BusinessException(400, "Đánh giá cần từ 1 đến 5 sao.");
        await feedback.SetRatingAsync(id, visitorKey, request.Stars, ct);
        return await feedback.GetRatingsAsync(id, visitorKey, ct);
    }
}
