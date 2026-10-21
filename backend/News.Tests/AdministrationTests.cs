using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using News.Application;
using News.Domain;
using News.Infrastructure;
using Xunit;

namespace News.Tests;

public sealed class AdministrationTests(NewsApiFactory factory) : IClassFixture<NewsApiFactory>
{
    private async Task<HttpClient> Login(string email = "admin@tin24.local", string password = "Tin24@Admin2026!")
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.AccessToken);
        return client;
    }

    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    [Fact]
    public async Task AdministrationRequiresAdminForReadsAndWrites()
    {
        using var anonymous = factory.CreateClient();
        using var editor = await Login("editor@tin24.local", "Tin24@Editor2026!");
        using var author = await Login("author@tin24.local", "Tin24@Author2026!");
        foreach (var (client, status) in new[] { (anonymous, HttpStatusCode.Unauthorized), (editor, HttpStatusCode.Forbidden), (author, HttpStatusCode.Forbidden) })
        {
            foreach (var resource in new[] { "users", "roles", "categories" })
            {
                Assert.Equal(status, (await client.GetAsync($"/api/admin/{resource}")).StatusCode);
                Assert.Equal(status, (await client.PostAsJsonAsync($"/api/admin/{resource}", new { })).StatusCode);
            }
            Assert.Equal(status, (await client.PutAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}/role", new { })).StatusCode);
        }
    }

    [Fact]
    public async Task CustomRoleCreatesUsableAccountWithInheritedPermissionsAndHashedPassword()
    {
        using var admin = await Login();
        var role = await Read<RoleDefinitionDto>(await admin.PostAsJsonAsync("/api/admin/roles", new { name = "Biên tập chuyên đề", baseRole = "Editor" }));
        var response = await admin.PostAsJsonAsync("/api/admin/users", new { name = "  Nhân sự mới  ", email = "CUSTOM@EXAMPLE.COM", password = "PrivatePassword!2026", roleId = role.Id });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        var user = JsonSerializer.Deserialize<ManagedUserDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal("custom@example.com", user.Email);
        Assert.Equal("Nhân sự mới", user.Name);
        Assert.Equal("Editor", user.Role);
        Assert.Equal(role.Id, user.RoleId);
        using var custom = await Login(user.Email, "PrivatePassword!2026");
        Assert.Equal(HttpStatusCode.OK, (await custom.GetAsync("/api/admin/articles")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await custom.GetAsync("/api/admin/users")).StatusCode);
        var listed = await admin.GetFromJsonAsync<PagedResult<ManagedUserDto>>("/api/admin/users?search=CUSTOM&pageSize=1");
        Assert.Equal(user.Id, Assert.Single(listed!.Items).Id);
        var roles = await admin.GetFromJsonAsync<RoleDefinitionDto[]>("/api/admin/roles");
        Assert.Equal(1, Assert.Single(roles!, r => r.Id == role.Id).UserCount);
        await using var scope = factory.Services.CreateAsyncScope();
        var stored = await scope.ServiceProvider.GetRequiredService<NewsDbContext>().Users.SingleAsync(u => u.Id == user.Id);
        Assert.Equal(PasswordVerificationResult.Success, new PasswordHasher<User>().VerifyHashedPassword(stored, stored.PasswordHash, "PrivatePassword!2026"));
    }

    [Fact]
    public async Task DuplicateAndInvalidInputsAreRejected()
    {
        using var admin = await Login();
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/admin/users", new { name = "Trùng email", email = "ADMIN@TIN24.LOCAL", password = "PrivatePassword!2026", roleId = "Author" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/users", new { name = "Mật khẩu ngắn", email = "short@example.com", password = "short", roleId = "Author" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/users", new { name = "Vai trò sai", email = "invalid@example.com", password = "PrivatePassword!2026", roleId = "missing" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/roles", new { name = "Sai quyền", baseRole = "SuperAdmin" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/roles", new { name = "   ", baseRole = "Author" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/admin/roles", new { name = "Tác giả", baseRole = "Author" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/admin/users?page=0&pageSize=100")).StatusCode);
    }

    [Fact]
    public async Task AssigningRoleRevokesSessionsRejectsStaleChangesAndProtectsOwnAdmin()
    {
        using var admin = await Login();
        var user = await Read<ManagedUserDto>(await admin.PostAsJsonAsync("/api/admin/users", new { name = "Đổi vai trò", email = "reassign@example.com", password = "PrivatePassword!2026", roleId = "Author" }));
        using var staff = await Login(user.Email, "PrivatePassword!2026");
        var list = await admin.GetFromJsonAsync<PagedResult<ManagedUserDto>>($"/api/admin/users?search={user.Email}");
        user = Assert.Single(list!.Items);
        var updated = await Read<ManagedUserDto>(await admin.PutAsJsonAsync($"/api/admin/users/{user.Id}/role", new { roleId = "Editor", version = user.Version }));
        Assert.Equal("Editor", updated.Role);
        Assert.Equal(HttpStatusCode.Unauthorized, (await staff.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await staff.PostAsync("/api/auth/refresh", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/admin/users/{user.Id}/role", new { roleId = "Author", version = user.Version })).StatusCode);
        using var signedInAgain = await Login(user.Email, "PrivatePassword!2026");
        Assert.Equal("Editor", (await signedInAgain.GetFromJsonAsync<UserDto>("/api/auth/me"))!.Role);
        var admins = await admin.GetFromJsonAsync<PagedResult<ManagedUserDto>>("/api/admin/users?search=admin@tin24.local");
        var own = Assert.Single(admins!.Items);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/admin/users/{own.Id}/role", new { roleId = "Author", version = own.Version })).StatusCode);
    }

    [Fact]
    public async Task CreatedCategoryIsPublicAndCanBeUsedInArticleDraft()
    {
        using var admin = await Login();
        using var publicClient = factory.CreateClient();
        var category = await Read<CategoryDto>(await admin.PostAsJsonAsync("/api/admin/categories", new { name = "  Kinh tế mới  ", slug = "kinh-te-moi" }));
        Assert.Equal("Kinh tế mới", category.Name);
        Assert.Contains((await publicClient.GetFromJsonAsync<CategoryDto[]>("/api/categories"))!, c => c.Slug == category.Slug);
        var article = await admin.PostAsJsonAsync("/api/admin/articles", new {
            title = "Bài viết thuộc danh mục mới", summary = "Kiểm tra danh mục vừa được tạo có thể dùng để soạn và lưu bài viết.",
            content = "<p>Nội dung kiểm tra danh mục mới đã được sử dụng để lưu bản nháp vào cơ sở dữ liệu.</p>",
            categorySlug = category.Slug, imageUrl = "https://example.com/image.jpg", readingMinutes = 3,
        });
        article.EnsureSuccessStatusCode();
        var categories = await admin.GetFromJsonAsync<CategoryDto[]>("/api/admin/categories");
        Assert.Equal(1, Assert.Single(categories!, c => c.Slug == category.Slug).Count);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/admin/categories", new { name = "Tên khác", slug = category.Slug })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/admin/categories", new { name = category.Name, slug = "slug-khac" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/categories", new { name = "Slug sai", slug = "Có dấu /" })).StatusCode);
    }
}
