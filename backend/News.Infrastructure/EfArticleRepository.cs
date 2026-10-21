using Microsoft.EntityFrameworkCore;
using News.Domain;

namespace News.Infrastructure;

public sealed class EfArticleRepository(NewsDbContext db) : IArticleRepository
{
    public async Task<ArticlePage> GetPageAsync(ArticleFilter filter, CancellationToken ct = default)
    {
        var query = db.Articles.AsNoTracking().Where(a => !a.IsDeleted);
        if (!filter.IncludeUnpublished) query = query.Where(a => a.Status == ArticleStatus.Published);
        if (filter.AuthorId is { } author) query = query.Where(a => a.AuthorId == author);
        if (filter.Status is { } status) query = query.Where(a => a.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            var slug = filter.Category.Trim().ToLowerInvariant();
            query = query.Where(a => a.CategorySlug == slug);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = SearchNormalizer.Normalize(filter.Search.Trim());
            query = query.Where(a => a.SearchText.Contains(term));
        }
        var count = await query.CountAsync(ct);
        var offset = ((long)filter.Page - 1) * filter.PageSize;
        if (offset >= count) return new(Array.Empty<Article>(), count);
        var ordered = filter.IncludeUnpublished
            ? query.OrderByDescending(a => a.UpdatedAt).ThenByDescending(a => a.Id)
            : query.OrderByDescending(a => a.PublishedAt).ThenByDescending(a => a.Id);
        return new(await ordered.Skip((int)offset).Take(filter.PageSize).ToArrayAsync(ct), count);
    }

    public Task<Article?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Articles.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct);

    public async Task<IReadOnlyList<CategoryCount>> GetCategoriesAsync(CancellationToken ct = default) =>
        await db.Categories.AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new CategoryCount(c.Slug, c.Name,
                db.Articles.Count(a => a.CategorySlug == c.Slug && a.Status == ArticleStatus.Published && !a.IsDeleted)))
            .ToArrayAsync(ct);

    public Task<Category?> FindCategoryAsync(string slug, CancellationToken ct = default) =>
        db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug, ct);
    public void Add(Article article) => db.Articles.Add(article);
    public async Task SaveChangesAsync(CancellationToken ct = default) => await db.SaveChangesAsync(ct);
}
