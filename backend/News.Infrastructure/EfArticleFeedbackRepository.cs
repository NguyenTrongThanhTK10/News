using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using News.Domain;

namespace News.Infrastructure;

public sealed class EfArticleFeedbackRepository(NewsDbContext db) : IArticleFeedbackRepository
{
    public async Task<CommentPage> GetCommentsAsync(int articleId, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Comments.AsNoTracking().Where(c => c.ArticleId == articleId);
        var count = await query.CountAsync(ct);
        var offset = ((long)page - 1) * pageSize;
        if (offset >= count) return new(Array.Empty<ArticleComment>(), count);
        var items = await query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
            .Skip((int)offset).Take(pageSize).ToArrayAsync(ct);
        return new(items, count);
    }

    public async Task<ArticleComment> AddCommentAsync(ArticleComment comment, CancellationToken ct)
    {
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);
        return comment;
    }

    public async Task<RatingStatistics> GetRatingsAsync(int articleId, string? visitorKey, CancellationToken ct)
    {
        var query = db.Ratings.AsNoTracking().Where(r => r.ArticleId == articleId);
        var groups = await query.GroupBy(r => r.Stars).Select(g => new { Stars = g.Key, Count = g.Count() }).ToArrayAsync(ct);
        var distribution = Enumerable.Range(1, 5).Select(stars => groups.FirstOrDefault(g => g.Stars == stars)?.Count ?? 0).ToArray();
        var count = distribution.Sum();
        var average = count == 0 ? 0 : Math.Round(groups.Sum(g => (double)g.Stars * g.Count) / count, 1);
        var myRating = visitorKey is null ? null : await query.Where(r => r.VisitorKey == visitorKey).Select(r => (int?)r.Stars).SingleOrDefaultAsync(ct);
        return new(average, count, myRating, distribution);
    }

    public async Task SetRatingAsync(int articleId, string visitorKey, int stars, CancellationToken ct)
    {
        var rating = await db.Ratings.FindAsync([articleId, visitorKey], ct);
        if (rating is null)
        {
            rating = new ArticleRating { ArticleId = articleId, VisitorKey = visitorKey };
            db.Ratings.Add(rating);
        }
        rating.Stars = stars;
        rating.UpdatedAt = DateTime.UtcNow;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is SqliteException { SqliteErrorCode: 19 }
            or SqlException { Number: 2601 or 2627 })
        {
            // Two tabs may submit the first rating concurrently; retry as an update.
            db.Entry(rating).State = EntityState.Detached;
            var existing = await db.Ratings.FindAsync([articleId, visitorKey], ct);
            if (existing is null) throw;
            existing.Stars = stars;
            existing.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
