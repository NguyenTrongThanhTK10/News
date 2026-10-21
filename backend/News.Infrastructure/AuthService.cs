using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using News.Application;
using News.Domain;

namespace News.Infrastructure;

public sealed class AuthService(NewsDbContext db, IPasswordHasher<User> hasher, IOptions<JwtOptions> options) : IAuthService
{
    private readonly JwtOptions _jwt = options.Value;
    // Hash giả giúp giảm khác biệt thời gian giữa tài khoản tồn tại và không tồn tại.
    private static readonly User DummyUser = new();
    private static readonly string DummyHash = new PasswordHasher<User>().HashPassword(DummyUser, "DummyPasswordForTimingOnly!2026");

    public async Task<AuthSessionResult?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
        var verification = hasher.VerifyHashedPassword(user ?? DummyUser, user?.PasswordHash ?? DummyHash, request.Password);
        if (user is null || user.LockedUntil > DateTime.UtcNow) return null;
        if (verification == PasswordVerificationResult.Failed)
        {
            user.FailedLogins++;
            if (user.FailedLogins >= 5) { user.LockedUntil = DateTime.UtcNow.AddMinutes(5); user.FailedLogins = 0; }
            user.Version = Guid.NewGuid();
            await db.SaveChangesAsync(ct);
            return null;
        }
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = hasher.HashPassword(user, request.Password);
        user.FailedLogins = 0; user.LockedUntil = null; user.Version = Guid.NewGuid();
        var expired = await db.Sessions.Where(s => s.UserId == user.Id && s.ExpiresAt <= DateTime.UtcNow).ToArrayAsync(ct);
        db.Sessions.RemoveRange(expired);
        var refresh = GenerateRefresh();
        var session = new LoginSession { UserId = user.Id, RefreshTokenHash = Hash(refresh), ExpiresAt = DateTime.UtcNow.AddHours(8) };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);
        return Issue(user, session, refresh);
    }

    public async Task<AuthSessionResult?> RefreshAsync(string token, CancellationToken ct)
    {
        var hash = Hash(token);
        var session = await db.Sessions.SingleOrDefaultAsync(s => s.RefreshTokenHash == hash, ct);
        if (session is null || session.ExpiresAt <= DateTime.UtcNow) return null;
        var user = await db.Users.FindAsync([session.UserId], ct);
        if (user is null) return null;
        var refresh = GenerateRefresh();
        session.RefreshTokenHash = Hash(refresh);
        session.Version = Guid.NewGuid();
        // Không kéo dài thời hạn tuyệt đối của phiên khi refresh.
        await db.SaveChangesAsync(ct);
        return Issue(user, session, refresh);
    }

    public async Task RevokeAsync(string? token, Guid? sessionId, CancellationToken ct)
    {
        var hash = token is null ? "" : Hash(token);
        var sessions = await db.Sessions.Where(s => s.Id == sessionId || s.RefreshTokenHash == hash).ToArrayAsync(ct);
        db.Sessions.RemoveRange(sessions);
        await db.SaveChangesAsync(ct);
    }

    public async Task<UserDto?> ValidateSessionAsync(Guid sessionId, Guid userId, CancellationToken ct)
    {
        if (!await db.Sessions.AsNoTracking().AnyAsync(s => s.Id == sessionId && s.UserId == userId && s.ExpiresAt > DateTime.UtcNow, ct)) return null;
        return await db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Role)).SingleOrDefaultAsync(ct);
    }

    private AuthSessionResult Issue(User user, LoginSession session, string refresh)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        Claim[] claims = [new("sub", user.Id.ToString()), new("sid", session.Id.ToString()),
            new("name", user.Name), new("email", user.Email), new("role", user.Role), new("jti", Guid.NewGuid().ToString())];
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, DateTime.UtcNow, expires,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey)), SecurityAlgorithms.HmacSha256));
        return new(new(new JwtSecurityTokenHandler().WriteToken(token), expires, new(user.Id, user.Name, user.Email, user.Role)), refresh, session.ExpiresAt);
    }
    private static string GenerateRefresh() => Convert.ToHexString(RandomNumberGenerator.GetBytes(48));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
