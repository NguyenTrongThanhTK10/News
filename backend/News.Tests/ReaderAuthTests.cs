using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using News.Application;
using News.Domain;
using News.Infrastructure;
using Xunit;

namespace News.Tests;

public sealed class ReaderAuthTests : IDisposable
{
    private readonly NewsApiFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private static async Task<AuthResponse> Register(HttpClient client, string email = "reader@example.com")
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new {
            name = "  Minh An  ", email, password = "PrivateReaderPassword!2026", role = "Admin", roleId = "Admin",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", session.AccessToken);
        return session;
    }

    [Fact]
    public async Task RegistrationCreatesReaderSessionAndCommentsUseAccountIdentity()
    {
        using var client = _factory.CreateClient();
        var session = await Register(client, "READER@EXAMPLE.COM");
        Assert.Equal("Reader", session.User.Role);
        Assert.Equal("Độc giả", session.User.RoleName);
        Assert.Equal("reader@example.com", session.User.Email);
        Assert.Equal("Minh An", session.User.Name);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/articles")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/users")).StatusCode);
        var response = await client.PostAsJsonAsync("/api/articles/1/comments", new {
            content = "  Bình luận của tài khoản đã đăng ký.  ", displayName = "Mạo danh", userId = Guid.NewGuid(),
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var comment = (await response.Content.ReadFromJsonAsync<CommentDto>())!;
        Assert.Equal("Minh An", comment.DisplayName);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NewsDbContext>();
        Assert.Equal(session.User.Id, (await db.Comments.SingleAsync()).UserId);
        var user = await db.Users.SingleAsync(u => u.Id == session.User.Id);
        Assert.Equal(PasswordVerificationResult.Success, new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, "PrivateReaderPassword!2026"));
        Assert.Null(user.GoogleSubject);
        var refreshed = await client.PostAsync("/api/auth/refresh", null);
        refreshed.EnsureSuccessStatusCode();
        Assert.Equal(session.User.Id, (await refreshed.Content.ReadFromJsonAsync<AuthResponse>())!.User.Id);
        await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/articles/1/comments", new { content = "Đã đăng xuất tài khoản" })).StatusCode);
    }

    [Fact]
    public async Task RegistrationRejectsDuplicateEmailAndInvalidData()
    {
        using var client = _factory.CreateClient();
        await Register(client);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", new { name = "Tên khác", email = "READER@example.com", password = "PrivateReaderPassword!2026" })).StatusCode);
        foreach (var input in new[] {
            new { name = " a ", email = "short-name@example.com", password = "PrivateReaderPassword!2026" },
            new { name = "Hợp lệ", email = "invalid", password = "PrivateReaderPassword!2026" },
            new { name = "Hợp lệ", email = "short@example.com", password = "short" },
        }) Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", input)).StatusCode);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register") {
            Content = JsonContent.Create(new { name = "Tên hợp lệ", email = "cross@example.com", password = "PrivateReaderPassword!2026" }),
        };
        request.Headers.Add("Origin", "https://untrusted.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task GoogleIsUnavailableUntilConfigured()
    {
        using var client = _factory.CreateClient();
        var options = await client.GetFromJsonAsync<GoogleOptions>("/api/auth/google/options");
        Assert.Null(options!.ClientId);
        Assert.Null(options.Nonce);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/auth/google", new { credential = "forged-token" })).StatusCode);
    }

    [Fact]
    public async Task GoogleVerifierRejectsMalformedCredentials()
    {
        var verifier = new GoogleIdentityVerifier(Options.Create(new GoogleAuthOptions { ClientId = "test.apps.googleusercontent.com" }));
        Assert.Null(await verifier.VerifyAsync("not-a-jwt", new string('a', 64), default));
    }

    private WebApplicationFactory<Program> WithGoogle(FakeGoogleVerifier verifier) => _factory.WithWebHostBuilder(builder => {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> {
            ["Google:ClientId"] = "integration-test.apps.googleusercontent.com",
        }));
        builder.ConfigureTestServices(services => {
            services.RemoveAll<IGoogleIdentityVerifier>();
            services.AddSingleton<IGoogleIdentityVerifier>(verifier);
        });
    });

    [Fact]
    public async Task GoogleRequiresNonceAndVerifiedIdentityThenReusesSubjectWithoutPasswordLogin()
    {
        var verifier = new FakeGoogleVerifier();
        using var factory = WithGoogle(verifier);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/google", new { credential = "verified-token" })).StatusCode);
        var options = (await client.GetFromJsonAsync<GoogleOptions>("/api/auth/google/options"))!;
        Assert.Equal(64, options.Nonce!.Length);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/google", new { credential = "forged-token" })).StatusCode);
        var response = await client.PostAsJsonAsync("/api/auth/google", new { credential = "verified-token" });
        response.EnsureSuccessStatusCode();
        var session = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal(options.Nonce, verifier.LastNonce);
        Assert.Equal("Reader", session.User.Role);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/google", new { credential = "verified-token" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email = session.User.Email, password = "DummyPasswordForTimingOnly!2026" })).StatusCode);
        await client.GetAsync("/api/auth/google/options");
        var repeat = await client.PostAsJsonAsync("/api/auth/google", new { credential = "verified-token" });
        repeat.EnsureSuccessStatusCode();
        Assert.Equal(session.User.Id, (await repeat.Content.ReadFromJsonAsync<AuthResponse>())!.User.Id);
        await using var scope = factory.Services.CreateAsyncScope();
        var users = await scope.ServiceProvider.GetRequiredService<NewsDbContext>().Users.Where(u => u.GoogleSubject != null).ToArrayAsync();
        Assert.Equal("google-subject", Assert.Single(users).GoogleSubject);
    }

    [Fact]
    public async Task GoogleDoesNotAutomaticallyLinkExistingStaffByEmail()
    {
        using var factory = WithGoogle(new FakeGoogleVerifier { Email = "admin@tin24.local" });
        using var client = factory.CreateClient();
        await client.GetAsync("/api/auth/google/options");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/google", new { credential = "verified-token" })).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var admin = await scope.ServiceProvider.GetRequiredService<NewsDbContext>().Users.SingleAsync(u => u.Email == "admin@tin24.local");
        Assert.Null(admin.GoogleSubject);
        Assert.Equal("Admin", admin.Role);
    }

    private sealed record GoogleOptions(string? ClientId, string? Nonce);
    private sealed class FakeGoogleVerifier : IGoogleIdentityVerifier
    {
        public string Email { get; init; } = "google@example.com";
        public string? LastNonce { get; private set; }
        public Task<GoogleIdentity?> VerifyAsync(string credential, string nonce, CancellationToken ct)
        {
            LastNonce = nonce;
            return Task.FromResult(credential == "verified-token" ? new GoogleIdentity("google-subject", Email, "Độc giả Google") : null);
        }
    }
}
