using System.Text.RegularExpressions;
using News.Domain;

namespace News.Application;

public sealed class ArticleManagementService(IArticleRepository repository, IHtmlContentSanitizer sanitizer)
{
    public async Task<PagedResult<AdminArticleDto>> ListAsync(AdminArticleQuery query, StaffActor actor, CancellationToken ct)
    {
        ArticleService.ValidateQuery(query);
        var page = await repository.GetPageAsync(new(query.Search?.Trim(), query.Category, query.Page, query.PageSize,
            true, actor.CanReview ? null : actor.Id, query.Status), ct);
        return new(page.Items.Select(AdminArticleDto.FromArticle).ToArray(), page.TotalCount, query.Page, query.PageSize);
    }

    public async Task<AdminArticleDto> GetAsync(int id, StaffActor actor, CancellationToken ct) =>
        AdminArticleDto.FromArticle(await FindAsync(id, actor, ct));

    public async Task<AdminArticleDto> SaveAsync(int? id, SaveArticleRequest request, StaffActor actor, CancellationToken ct)
    {
        if (request.Title.Trim().Length < 8 || request.Summary.Trim().Length < 20)
            throw new BusinessException(400, "Tiêu đề cần ít nhất 8 ký tự và mô tả cần ít nhất 20 ký tự sau khi bỏ khoảng trắng thừa.");
        var article = id is null ? Article.Draft(actor.Id, actor.Name) : await FindAsync(id.Value, actor, ct);
        if (id is not null)
        {
            CheckVersion(article, request.Version);
            if (article.Status is ArticleStatus.Published or ArticleStatus.PendingReview or ArticleStatus.Archived)
                throw new BusinessException(409, "Chỉ sửa bài nháp, bài bị trả lại hoặc bài đã duyệt. Hãy gỡ xuất bản trước khi sửa.");
        }
        var category = await repository.FindCategoryAsync(request.CategorySlug, ct)
            ?? throw new BusinessException(400, "Chuyên mục không tồn tại.");
        if (!Uri.TryCreate(request.ImageUrl, UriKind.Absolute, out var image) || image.Scheme != "https")
            throw new BusinessException(400, "Ảnh minh họa cần là đường dẫn HTTPS.");
        var html = sanitizer.Sanitize(request.Content);
        var text = System.Net.WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]*>", " ", RegexOptions.None, TimeSpan.FromSeconds(1)));
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 30)
            throw new BusinessException(400, "Nội dung bài viết cần ít nhất 30 ký tự văn bản.");
        article.Edit(request.Title.Trim(), request.Summary.Trim(), html, category.Slug, category.Name, request.ImageUrl.Trim(), request.ReadingMinutes);
        if (id is null) repository.Add(article);
        await repository.SaveChangesAsync(ct);
        return AdminArticleDto.FromArticle(article);
    }

    public async Task<AdminArticleDto> TransitionAsync(int id, string action, TransitionRequest request, StaffActor actor, CancellationToken ct)
    {
        var article = await FindAsync(id, actor, ct);
        CheckVersion(article, request.Version);
        if (action != "submit" && !actor.CanReview) throw new BusinessException(403, "Bạn không có quyền duyệt hoặc xuất bản.");
        if (action == "reject" && string.IsNullOrWhiteSpace(request.Note))
            throw new BusinessException(400, "Hãy ghi lý do trả lại bài viết.");
        var next = action switch
        {
            "submit" => ArticleStatus.PendingReview,
            "approve" => ArticleStatus.Approved,
            "reject" => ArticleStatus.Rejected,
            "publish" => ArticleStatus.Published,
            "unpublish" or "restore" => ArticleStatus.Draft,
            "archive" => ArticleStatus.Archived,
            _ => throw new BusinessException(400, "Thao tác không tồn tại."),
        };
        article.Transition(next, request.Note?.Trim() ?? "");
        await repository.SaveChangesAsync(ct);
        return AdminArticleDto.FromArticle(article);
    }

    public async Task DeleteAsync(int id, Guid version, StaffActor actor, CancellationToken ct)
    {
        var article = await FindAsync(id, actor, ct);
        if (actor.Role != Roles.Admin && (article.AuthorId != actor.Id || article.Status is not (ArticleStatus.Draft or ArticleStatus.Rejected)))
            throw new BusinessException(403, "Chỉ quản trị viên hoặc tác giả của bài nháp được xóa bài.");
        CheckVersion(article, version);
        article.Delete();
        await repository.SaveChangesAsync(ct);
    }

    private async Task<Article> FindAsync(int id, StaffActor actor, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(id, ct);
        if (article is null || article.IsDeleted) throw new BusinessException(404, "Không tìm thấy bài viết.");
        if (!actor.CanReview && article.AuthorId != actor.Id) throw new BusinessException(403, "Bạn chỉ được quản lý bài viết của mình.");
        return article;
    }
    private static void CheckVersion(Article article, Guid? version)
    {
        if (version is null || version == Guid.Empty) throw new BusinessException(400, "Thiếu phiên bản bài viết.");
        if (article.Version != version) throw new BusinessException(409, "Bài viết đã được người khác thay đổi. Hãy tải lại trước khi thao tác.");
    }
}
