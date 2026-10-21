namespace News.Domain;

public sealed record ArticleFilter(string? Search, string? Category, int Page, int PageSize,
    bool IncludeUnpublished = false, Guid? AuthorId = null, ArticleStatus? Status = null);
public sealed record ArticlePage(IReadOnlyList<Article> Items, int TotalCount);
public sealed record CategoryCount(string Slug, string Name, int Count);

public interface IArticleRepository
{
    Task<ArticlePage> GetPageAsync(ArticleFilter filter, CancellationToken ct = default);
    Task<Article?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CategoryCount>> GetCategoriesAsync(CancellationToken ct = default);
    Task<Category?> FindCategoryAsync(string slug, CancellationToken ct = default);
    void Add(Article article);
    Task SaveChangesAsync(CancellationToken ct = default);
}
