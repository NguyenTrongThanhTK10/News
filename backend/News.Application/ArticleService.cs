using News.Domain;

namespace News.Application;

public sealed class ArticleService(IArticleRepository repository) : IArticleService
{
    public async Task<PagedResult<ArticleSummary>> GetArticlesAsync(ArticleQuery query, CancellationToken ct = default)
    {
        ValidateQuery(query);
        var result = await repository.GetPageAsync(new(query.Search?.Trim(), query.Category?.Trim(), query.Page, query.PageSize), ct);
        return new(result.Items.Select(ArticleSummary.FromArticle).ToArray(), result.TotalCount, query.Page, query.PageSize);
    }

    public async Task<ArticleDetail?> GetArticleAsync(int id, CancellationToken ct = default)
    {
        var article = await repository.GetByIdAsync(id, ct);
        return article is { Status: ArticleStatus.Published, IsDeleted: false } ? ArticleDetail.FromArticle(article) : null;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default) =>
        (await repository.GetCategoriesAsync(ct)).Select(c => new CategoryDto(c.Slug, c.Name, c.Count)).ToArray();

    public static void ValidateQuery(ArticleQuery query)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 30) throw new ArgumentOutOfRangeException(nameof(query));
    }
}
