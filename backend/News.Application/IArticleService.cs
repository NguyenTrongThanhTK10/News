namespace News.Application;

public interface IArticleService
{
    Task<PagedResult<ArticleSummary>> GetArticlesAsync(ArticleQuery query, CancellationToken ct = default);
    Task<ArticleDetail?> GetArticleAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
}

public interface IHtmlContentSanitizer
{
    string Sanitize(string html);
}

public interface IAuthService
{
    Task<AuthSessionResult> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthSessionResult> GoogleLoginAsync(GoogleLoginRequest request, string nonce, CancellationToken ct);
    Task<AuthSessionResult?> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthSessionResult?> RefreshAsync(string token, CancellationToken ct);
    Task RevokeAsync(string? token, Guid? sessionId, CancellationToken ct);
    Task<UserDto?> ValidateSessionAsync(Guid sessionId, Guid userId, CancellationToken ct);
}
