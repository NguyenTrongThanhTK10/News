using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace News.Tests;

public sealed class ApiTests(NewsApiFactory factory) : IClassFixture<NewsApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ListReturnsPageMetadataWithoutFullArticleContent()
    {
        var response = await _client.GetFromJsonAsync<JsonElement>("/api/articles?pageSize=2");
        Assert.Equal(12, response.GetProperty("totalCount").GetInt32());
        Assert.Equal(6, response.GetProperty("totalPages").GetInt32());
        var items = response.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.False(items[0].TryGetProperty("content", out _));
    }

    [Fact]
    public async Task DetailReturnsFullArticleAndTimezone()
    {
        var article = await _client.GetFromJsonAsync<JsonElement>("/api/articles/1");
        Assert.Contains("<p>", article.GetProperty("content").GetString());
        Assert.EndsWith("Z", article.GetProperty("publishedAt").GetString());
    }

    [Theory]
    [InlineData("/api/articles/999")]
    [InlineData("/api/articles/0")]
    [InlineData("/api/articles/not-a-number")]
    public async Task MissingArticlesReturnProblemDetails(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=31")]
    [InlineData("pageSize=-1")]
    [InlineData("page=abc")]
    public async Task InvalidQueryReturnsValidationProblem(string query)
    {
        var response = await _client.GetAsync($"/api/articles?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OverlongSearchIsRejected()
    {
        var response = await _client.GetAsync($"/api/articles?search={new string('a', 201)}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CorsAllowsConfiguredFrontendOnly()
    {
        using var allowed = new HttpRequestMessage(HttpMethod.Get, "/api/categories");
        allowed.Headers.Add("Origin", "http://localhost:5173");
        var response = await _client.SendAsync(allowed);
        Assert.Equal("http://localhost:5173", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        using var other = new HttpRequestMessage(HttpMethod.Get, "/api/categories");
        other.Headers.Add("Origin", "https://other.example");
        var rejected = await _client.SendAsync(other);
        Assert.False(rejected.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task HealthEndpointIsAvailable() =>
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health")).StatusCode);
}
