using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using News.Application;
using News.Domain;
using News.Infrastructure;
using Xunit;

namespace News.Tests;

public sealed class FeedbackTests : IDisposable
{
    private readonly NewsApiFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> SignedIn()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "author@tin24.local", password = "Tin24@Author2026!" });
        response.EnsureSuccessStatusCode();
        var session = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", session.AccessToken);
        return client;
    }

    [Fact]
    public async Task AnonymousCanReadButCannotPostComments()
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/articles/1/comments")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/articles/1/comments", new { content = "Chưa có tài khoản" })).StatusCode);
    }

    [Fact]
    public async Task CommentsPersistAndRemainPlainTextAcrossClients()
    {
        using var client = await SignedIn();
        var text = "<script>alert('xss')</script> Một góc nhìn đáng suy ngẫm.";
        var response = await client.PostAsJsonAsync("/api/articles/1/comments", new { displayName = "  Minh An  ", content = $"  {text}  " });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var comment = await response.Content.ReadFromJsonAsync<CommentDto>();
        Assert.Equal("Tác giả", comment!.DisplayName);
        await using var scope = _factory.Services.CreateAsyncScope();
        var savedAuthorId = await scope.ServiceProvider.GetRequiredService<NewsDbContext>().Comments.Select(c => c.UserId).SingleAsync();
        Assert.NotNull(savedAuthorId);
        Assert.Equal(text, comment.Content);
        Assert.Equal(DateTimeKind.Utc, comment.CreatedAt.Kind);
        using var reader = _factory.CreateClient();
        var saved = await reader.GetFromJsonAsync<PagedResult<CommentDto>>("/api/articles/1/comments");
        Assert.Equal(comment, Assert.Single(saved!.Items));
        Assert.Equal(1, saved.TotalCount);
        Assert.Empty((await reader.GetFromJsonAsync<PagedResult<CommentDto>>("/api/articles/2/comments"))!.Items);
    }

    [Fact]
    public async Task CommentsPaginateWithoutDuplicatesAndLargePagesDoNotOverflow()
    {
        using var client = await SignedIn();
        for (var i = 0; i < 4; i++)
            (await client.PostAsJsonAsync("/api/articles/1/comments", new { displayName = "Độc giả", content = $"Bình luận số {i}" })).EnsureSuccessStatusCode();
        var first = await client.GetFromJsonAsync<PagedResult<CommentDto>>("/api/articles/1/comments?pageSize=2");
        var second = await client.GetFromJsonAsync<PagedResult<CommentDto>>("/api/articles/1/comments?pageSize=2&page=2");
        Assert.Equal(4, first!.TotalCount);
        Assert.Equal(2, first.TotalPages);
        Assert.Equal(2, second!.Items.Count);
        Assert.Empty(first.Items.Select(c => c.Id).Intersect(second.Items.Select(c => c.Id)));
        Assert.Empty((await client.GetFromJsonAsync<PagedResult<CommentDto>>("/api/articles/1/comments?page=2147483647&pageSize=30"))!.Items);
    }

    [Theory]
    [InlineData("Độc giả", "   ")]
    [InlineData("Độc giả", "  ab  ")]
    public async Task CommentValidationUsesTrimmedContent(string displayName, string content)
    {
        using var client = await SignedIn();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/articles/1/comments", new { displayName, content })).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<PagedResult<CommentDto>>("/api/articles/1/comments"))!.Items);
    }

    [Fact]
    public async Task OversizedInputAndInvalidPaginationAreRejected()
    {
        using var client = await SignedIn();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/articles/1/comments", new { displayName = "Độc giả", content = new string('a', 2001) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/articles/1/comments?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/articles/1/comments?pageSize=31")).StatusCode);
    }

    [Fact]
    public async Task RatingUpdatesOneBrowserVoteAndAggregatesOtherBrowsers()
    {
        using var first = _factory.CreateClient();
        var initial = await first.GetFromJsonAsync<RatingStatistics>("/api/articles/1/ratings");
        Assert.Equal(0, initial!.Count);
        Assert.Null(initial.MyRating);
        var vote = await first.PostAsJsonAsync("/api/articles/1/ratings", new { stars = 5 });
        vote.EnsureSuccessStatusCode();
        var cookie = Assert.Single(vote.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        var changed = await first.PostAsJsonAsync("/api/articles/1/ratings", new { stars = 2 });
        changed.EnsureSuccessStatusCode();
        var single = await changed.Content.ReadFromJsonAsync<RatingStatistics>();
        Assert.Equal(1, single!.Count);
        Assert.Equal(2, single.Average);
        using var second = _factory.CreateClient();
        (await second.PostAsJsonAsync("/api/articles/1/ratings", new { stars = 4 })).EnsureSuccessStatusCode();
        var response = await first.GetAsync("/api/articles/1/ratings");
        var summary = await response.Content.ReadFromJsonAsync<RatingStatistics>();
        Assert.Equal(2, summary!.Count);
        Assert.Equal(3, summary.Average);
        Assert.Equal(2, summary.MyRating);
        Assert.Equal(new[] { 0, 1, 0, 1, 0 }, summary.Distribution);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Equal(0, (await first.GetFromJsonAsync<RatingStatistics>("/api/articles/2/ratings"))!.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task InvalidStarsDoNotCreateRatingsOrCookies(int stars)
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/articles/1/ratings", new { stars });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.Equal(0, (await client.GetFromJsonAsync<RatingStatistics>("/api/articles/1/ratings"))!.Count);
    }

    [Fact]
    public async Task ConcurrentFirstRatingsFromSameBrowserStayOneVote()
    {
        using var client = _factory.CreateClient();
        // Establish the browser cookie on another article, then race the first vote on article 1.
        (await client.PostAsJsonAsync("/api/articles/2/ratings", new { stars = 3 })).EnsureSuccessStatusCode();
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/articles/1/ratings", new { stars = 4 }),
            client.PostAsJsonAsync("/api/articles/1/ratings", new { stars = 5 }));
        Assert.All(responses, response => response.EnsureSuccessStatusCode());
        var result = await client.GetFromJsonAsync<RatingStatistics>("/api/articles/1/ratings");
        Assert.Equal(1, result!.Count);
        Assert.Contains(result.MyRating!.Value, new[] { 4, 5 });
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("draft")]
    [InlineData("deleted")]
    public async Task NonpublicArticlesCannotExposeOrReceiveFeedback(string kind)
    {
        using var client = await SignedIn();
        if (kind != "missing")
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<NewsDbContext>();
            var article = await db.Articles.FindAsync(1);
            if (kind == "draft") article!.Transition(ArticleStatus.Draft);
            else article!.Delete();
            await db.SaveChangesAsync();
        }
        var id = kind == "missing" ? 999 : 1;
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/articles/{id}/comments")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/articles/{id}/ratings")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/articles/{id}/comments", new { displayName = "Độc giả", content = "Một góc nhìn khác" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/articles/{id}/ratings", new { stars = 5 })).StatusCode);
    }

    [Fact]
    public async Task FeedbackWritesAreRateLimited()
    {
        using var client = _factory.CreateClient();
        for (var i = 0; i < 12; i++)
            (await client.PostAsJsonAsync("/api/articles/1/ratings", new { stars = 4 })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/articles/1/ratings", new { stars = 5 })).StatusCode);
    }

    [Fact]
    public async Task ExistingSqliteDatabaseUpgradesWithoutLosingArticlesAndCanUpgradeTwice()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new NewsDbContext(new DbContextOptionsBuilder<NewsDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Seed:DemoData"] = "true" }).Build();
        await DatabaseSeeder.SeedAsync(db, new PasswordHasher<User>(), config, true);
        await db.Database.ExecuteSqlRawAsync("DROP TABLE Comments; DROP TABLE Ratings;");
        await SqliteFeedbackSchema.UpgradeAsync(db);
        await SqliteReaderSchema.UpgradeAsync(db);
        var repository = new EfArticleFeedbackRepository(db);
        await repository.AddCommentAsync(new ArticleComment { ArticleId = 1, DisplayName = "Độc giả", Content = "Giữ nguyên dữ liệu hiện có." }, default);
        await repository.SetRatingAsync(1, "browser", 5, default);
        await SqliteFeedbackSchema.UpgradeAsync(db);
        Assert.Equal(12, await db.Articles.CountAsync());
        Assert.Equal(3, await db.Users.CountAsync());
        Assert.Equal(1, await db.Comments.CountAsync());
        Assert.Equal(1, await db.Ratings.CountAsync());
    }
}
