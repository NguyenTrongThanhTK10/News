namespace News.Domain;

public enum ArticleStatus { Draft, PendingReview, Approved, Published, Rejected, Archived }

public static class Roles
{
    public const string Admin = "Admin";
    public const string Editor = "Editor";
    public const string Author = "Author";
    public const string Staff = Admin + "," + Editor + "," + Author;
}

public sealed class Article
{
    private Article() { }

    // Constructor cho bộ dữ liệu minh họa; bài tạo từ quản trị luôn bắt đầu ở Draft.
    public Article(int id, string title, string summary, string content, string categorySlug,
        string category, string imageUrl, DateTimeOffset publishedAt, string author, int readingMinutes)
    {
        Id = id;
        Title = title; Summary = summary; Content = content; CategorySlug = categorySlug;
        Category = category; ImageUrl = imageUrl; PublishedAt = publishedAt.UtcDateTime;
        Author = author; ReadingMinutes = readingMinutes; Status = ArticleStatus.Published;
        CreatedAt = publishedAt.UtcDateTime; UpdatedAt = CreatedAt;
        SearchText = SearchNormalizer.Normalize($"{title} {summary} {category}");
    }

    public int Id { get; private set; }
    public string Title { get; private set; } = "";
    public string Summary { get; private set; } = "";
    public string Content { get; private set; } = "";
    public string CategorySlug { get; private set; } = "";
    public string Category { get; private set; } = "";
    public string ImageUrl { get; private set; } = "";
    public DateTime? PublishedAt { get; private set; }
    public string Author { get; private set; } = "";
    public Guid AuthorId { get; private set; }
    public int ReadingMinutes { get; private set; }
    public ArticleStatus Status { get; private set; } = ArticleStatus.Draft;
    public string ReviewNote { get; private set; } = "";
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public Guid Version { get; private set; } = Guid.NewGuid();
    public bool IsDeleted { get; private set; }
    public string SearchText { get; private set; } = "";

    public static Article Draft(Guid authorId, string author) => new() { AuthorId = authorId, Author = author };
    public void AssignSeedAuthor(Guid authorId) => AuthorId = authorId;

    public void Edit(string title, string summary, string content, string slug, string category, string imageUrl, int minutes)
    {
        Title = title; Summary = summary; Content = content; CategorySlug = slug; Category = category;
        ImageUrl = imageUrl; ReadingMinutes = minutes;
        SearchText = SearchNormalizer.Normalize($"{title} {summary} {category}");
        // Sửa bài đã duyệt bắt buộc đi qua duyệt lại.
        Status = ArticleStatus.Draft;
        ReviewNote = "";
        Touch();
    }

    public void Transition(ArticleStatus next, string note = "")
    {
        var valid = (Status, next) switch
        {
            (ArticleStatus.Draft or ArticleStatus.Rejected, ArticleStatus.PendingReview) => true,
            (ArticleStatus.PendingReview, ArticleStatus.Approved or ArticleStatus.Rejected) => true,
            (ArticleStatus.Approved, ArticleStatus.Published) => true,
            (ArticleStatus.Published, ArticleStatus.Draft or ArticleStatus.Archived) => true,
            (ArticleStatus.Archived, ArticleStatus.Draft) => true,
            _ => false,
        };
        if (!valid) throw new BusinessException(409, "Trạng thái bài viết đã thay đổi hoặc thao tác không hợp lệ.");
        Status = next;
        ReviewNote = note;
        if (next == ArticleStatus.Published) PublishedAt = DateTime.UtcNow;
        Touch();
    }

    public void Delete() { IsDeleted = true; Touch(); }
    private void Touch() { UpdatedAt = DateTime.UtcNow; Version = Guid.NewGuid(); }
}

public sealed class BusinessException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
