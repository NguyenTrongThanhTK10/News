using News.Application;
using News.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using News.Domain;
using Xunit;

namespace News.Tests;

public sealed class ArticleServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private NewsDbContext _db = null!;
    private ArticleService _service = null!;
    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _db = new(new DbContextOptionsBuilder<NewsDbContext>().UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Seed:DemoData"] = "true" }).Build();
        await DatabaseSeeder.SeedAsync(_db, new PasswordHasher<User>(), config, true);
        _service = new(new EfArticleRepository(_db));
    }
    public async Task DisposeAsync() { await _db.DisposeAsync(); await _connection.DisposeAsync(); }

    [Theory]
    [InlineData("tri tue nhan tao", 2)]
    [InlineData("TRÍ TUỆ NHÂN TẠO", 2)]
    [InlineData("da lat", 3)]
    [InlineData("  dap xe  ", 12)]
    public async Task SearchSupportsVietnameseWithOrWithoutAccents(string term, int expectedId)
    {
        var result = await _service.GetArticlesAsync(new ArticleQuery { Search = term });
        Assert.Contains(result.Items, article => article.Id == expectedId);
    }

    [Fact]
    public async Task FilteringHappensBeforePagination()
    {
        var result = await _service.GetArticlesAsync(new ArticleQuery { Category = "cong-nghe", PageSize = 1, Page = 2 });
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(8, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task PagesHaveStableDescendingOrderAndNoDuplicates()
    {
        var first = await _service.GetArticlesAsync(new ArticleQuery { PageSize = 9 });
        var second = await _service.GetArticlesAsync(new ArticleQuery { PageSize = 9, Page = 2 });
        Assert.Equal(12, first.TotalCount);
        Assert.Equal(9, first.Items.Count);
        Assert.Equal(3, second.Items.Count);
        Assert.Empty(first.Items.Select(article => article.Id).Intersect(second.Items.Select(article => article.Id)));
        Assert.Equal(Enumerable.Range(1, 12), first.Items.Concat(second.Items).Select(article => article.Id));
    }

    [Fact]
    public async Task LargePageDoesNotOverflow()
    {
        var result = await _service.GetArticlesAsync(new ArticleQuery { Page = int.MaxValue, PageSize = 30 });
        Assert.Empty(result.Items);
        Assert.Equal(12, result.TotalCount);
    }

    [Fact]
    public async Task CategoryAndSearchCombine()
    {
        var result = await _service.GetArticlesAsync(new ArticleQuery { Category = "doi-song", Search = "nhan tao" });
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task CategoriesCoverAllArticles()
    {
        var categories = await _service.GetCategoriesAsync();
        Assert.Equal(15, categories.Count);
        Assert.Equal(12, categories.Sum(category => category.Count));
        Assert.Equal(6, categories.Count(category => category.Count > 0));
        Assert.All(categories, category => Assert.Contains(category.Count, new[] { 0, 2 }));
    }

    [Theory]
    [InlineData(0, 9)]
    [InlineData(1, 0)]
    [InlineData(1, 31)]
    public async Task InvalidPaginationIsRejected(int page, int size) =>
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.GetArticlesAsync(new ArticleQuery { Page = page, PageSize = size }));
}
