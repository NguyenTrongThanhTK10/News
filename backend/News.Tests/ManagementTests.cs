using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using News.Application;
using Xunit;

namespace News.Tests;

public sealed class ManagementTests(NewsApiFactory factory) : IClassFixture<NewsApiFactory>
{
    private async Task<HttpClient> Login(string role)
    {
        var client = factory.CreateClient();
        var result = await client.PostAsJsonAsync("/api/auth/login", new { email = $"{role.ToLowerInvariant()}@tin24.local", password = $"Tin24@{role}2026!" });
        result.EnsureSuccessStatusCode();
        var auth = await result.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth!.AccessToken);
        return client;
    }
    private static object Draft(Guid? version = null, string? content = null) => new
    {
        title = "Câu chuyện thử nghiệm quản trị", summary = "Bài viết kiểm tra đầy đủ quy trình từ bản nháp đến xuất bản.",
        content = content ?? "<p>Đây là nội dung kiểm thử đủ dài để lưu vào database và xuất bản cho độc giả.</p>",
        categorySlug = "cong-nghe", imageUrl = "https://images.unsplash.com/photo-1498050108023-c5249f4df085", readingMinutes = 3, version,
    };
    private static int Id(JsonElement item) => item.GetProperty("article").GetProperty("id").GetInt32();
    private static Guid Version(JsonElement item) => item.GetProperty("version").GetGuid();
    private static async Task<JsonElement> Read(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task AnonymousRequestsCannotManageArticles()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/articles")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/admin/articles", Draft())).StatusCode);
    }

    [Fact]
    public async Task InvalidCredentialsDoNotIssueTokens()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "unknown@example.com", password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task WorkflowEnforcesRolesAndHidesUnpublishedContent()
    {
        using var author = await Login("Author");
        using var editor = await Login("Editor");
        using var publicClient = factory.CreateClient();
        var item = await Read(await author.PostAsJsonAsync("/api/admin/articles", Draft()));
        var id = Id(item);
        Assert.Equal("Draft", item.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await publicClient.GetAsync($"/api/articles/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await author.PostAsJsonAsync($"/api/admin/articles/{id}/publish", new { version = Version(item) })).StatusCode);
        item = await Read(await author.PostAsJsonAsync($"/api/admin/articles/{id}/submit", new { version = Version(item) }));
        Assert.Equal(HttpStatusCode.Conflict, (await editor.PostAsJsonAsync($"/api/admin/articles/{id}/publish", new { version = Version(item) })).StatusCode);
        item = await Read(await editor.PostAsJsonAsync($"/api/admin/articles/{id}/approve", new { version = Version(item) }));
        item = await Read(await editor.PostAsJsonAsync($"/api/admin/articles/{id}/publish", new { version = Version(item) }));
        Assert.Equal(HttpStatusCode.OK, (await publicClient.GetAsync($"/api/articles/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await editor.PutAsJsonAsync($"/api/admin/articles/{id}", Draft(Version(item)))).StatusCode);
        item = await Read(await editor.PostAsJsonAsync($"/api/admin/articles/{id}/unpublish", new { version = Version(item) }));
        Assert.Equal(HttpStatusCode.NotFound, (await publicClient.GetAsync($"/api/articles/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await author.DeleteAsync($"/api/admin/articles/{id}?version={Version(item)}")).StatusCode);
    }

    [Fact]
    public async Task AuthorsCannotReadOrEditAnotherAuthorsDraft()
    {
        using var admin = await Login("Admin");
        using var author = await Login("Author");
        var item = await Read(await admin.PostAsJsonAsync("/api/admin/articles", Draft()));
        Assert.Equal(HttpStatusCode.Forbidden, (await author.GetAsync($"/api/admin/articles/{Id(item)}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await author.PutAsJsonAsync($"/api/admin/articles/{Id(item)}", Draft(Version(item)))).StatusCode);
        var list = await author.GetFromJsonAsync<JsonElement>("/api/admin/articles?pageSize=30");
        Assert.DoesNotContain(list.GetProperty("items").EnumerateArray(), article => Id(article) == Id(item));
    }

    [Fact]
    public async Task RejectionRequiresNoteAndAllowsRevision()
    {
        using var author = await Login("Author");
        using var editor = await Login("Editor");
        var item = await Read(await author.PostAsJsonAsync("/api/admin/articles", Draft()));
        var id = Id(item);
        item = await Read(await author.PostAsJsonAsync($"/api/admin/articles/{id}/submit", new { version = Version(item) }));
        Assert.Equal(HttpStatusCode.BadRequest, (await editor.PostAsJsonAsync($"/api/admin/articles/{id}/reject", new { version = Version(item) })).StatusCode);
        item = await Read(await editor.PostAsJsonAsync($"/api/admin/articles/{id}/reject", new { version = Version(item), note = "Cần bổ sung bối cảnh." }));
        Assert.Equal("Cần bổ sung bối cảnh.", item.GetProperty("reviewNote").GetString());
        item = await Read(await author.PutAsJsonAsync($"/api/admin/articles/{id}", Draft(Version(item))));
        Assert.Equal("Draft", item.GetProperty("status").GetString());
    }

    [Fact]
    public async Task StaleVersionCannotOverwriteNewerContent()
    {
        using var admin = await Login("Admin");
        var item = await Read(await admin.PostAsJsonAsync("/api/admin/articles", Draft()));
        var request = Draft(Version(item));
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/admin/articles/{Id(item)}", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/admin/articles/{Id(item)}", request)).StatusCode);
    }

    [Fact]
    public async Task ContentIsSanitizedAndValidatedOnServer()
    {
        using var admin = await Login("Admin");
        var html = "<p onclick=\"alert(1)\">Nội dung kiểm tra xử lý HTML đủ dài để lưu một bài viết mới.</p><script>alert(1)</script><a href=\"javascript:alert(1)\">Liên kết</a><img src=x onerror=\"alert(1)\">";
        var item = await Read(await admin.PostAsJsonAsync("/api/admin/articles", Draft(content: html)));
        var saved = item.GetProperty("article").GetProperty("content").GetString()!;
        Assert.DoesNotContain("<script", saved);
        Assert.DoesNotContain("onclick", saved);
        Assert.DoesNotContain("javascript:", saved);
        Assert.DoesNotContain("<img", saved);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/articles", Draft(content: "<p>  </p>"))).StatusCode);
    }

    [Fact]
    public async Task RefreshRotatesCookieAndLogoutRevokesAccessToken()
    {
        using var client = await Login("Admin");
        var response = await client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        var oldAccessToken = client.DefaultRequestHeaders.Authorization;
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        client.DefaultRequestHeaders.Authorization = oldAccessToken;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/auth/refresh", null)).StatusCode);
    }

    [Fact]
    public async Task ForgedJwtAndCrossOriginRefreshAreRejected()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "not.a.valid.jwt");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/articles")).StatusCode);
        using var signedIn = await Login("Editor");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Origin", "https://untrusted.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await signedIn.SendAsync(request)).StatusCode);
    }
}
