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

public sealed class AuthService(NewsDbContext db, IPasswordHasher<User> hasher, IOptions<JwtOptions> options,
    IGoogleIdentityVerifier google) : IAuthService
{
    private readonly JwtOptions _jwt = options.Value;
    // Hash giả giúp giảm khác biệt thời gian giữa tài khoản tồn tại và không tồn tại.
    private static readonly User DummyUser = new();
    private static readonly string DummyHash = new PasswordHasher<User>().HashPassword(DummyUser, "DummyPasswordForTimingOnly!2026");

    public async Task<AuthSessionResult> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (name.Length < 2) throw new BusinessException(400, "Họ tên cần tối thiểu 2 ký tự.");
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct)) throw new BusinessException(409, "Email đã được đăng ký. Vui lòng đăng nhập.");
        var user = new User { Name = name, Email = email, Role = Roles.Reader, RoleDefinitionId = Roles.Reader };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        return await CreateSessionAsync(user, ct);
    }

    public async Task<AuthSessionResult> GoogleLoginAsync(GoogleLoginRequest request, string nonce, CancellationToken ct)
    {
        var identity = await google.VerifyAsync(request.Credential, nonce, ct)
            ?? throw new BusinessException(401, "Không xác minh được tài khoản Google. Vui lòng thử lại.");
        var user = await db.Users.SingleOrDefaultAsync(u => u.GoogleSubject == identity.Subject, ct);
        if (user is null)
        {
            // Không ghép theo email: tài khoản có sẵn phải xác thực bằng phương thức đã đăng ký.
            if (await db.Users.AnyAsync(u => u.Email == identity.Email, ct))
                throw new BusinessException(409, "Email này đã có tài khoản. Vui lòng đăng nhập bằng mật khẩu.");
            user = new User { Email = identity.Email, Name = identity.Name, GoogleSubject = identity.Subject,
                Role = Roles.Reader, RoleDefinitionId = Roles.Reader };
            db.Users.Add(user);
        }
        if (user.LockedUntil > DateTime.UtcNow) throw new BusinessException(401, "Tài khoản đang tạm khóa. Vui lòng thử lại sau.");
        return await CreateSessionAsync(user, ct);
    }

    public async Task<AuthSessionResult?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
        var hash = string.IsNullOrEmpty(user?.PasswordHash) ? DummyHash : user.PasswordHash;
        var verification = hasher.VerifyHashedPassword(user ?? DummyUser, hash, request.Password);
        if (user is null || string.IsNullOrEmpty(user.PasswordHash) || user.LockedUntil > DateTime.UtcNow) return null;
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
        return await CreateSessionAsync(user, ct);
    }

    private async Task<AuthSessionResult> CreateSessionAsync(User user, CancellationToken ct)
    {
        user.Version = Guid.NewGuid();
        var expired = await db.Sessions.Where(s => s.UserId == user.Id && s.ExpiresAt <= DateTime.UtcNow).ToArrayAsync(ct);
        db.Sessions.RemoveRange(expired);
        var refresh = GenerateRefresh();
        var session = new LoginSession { UserId = user.Id, RefreshTokenHash = Hash(refresh), ExpiresAt = DateTime.UtcNow.AddHours(8) };
        db.Sessions.Add(session);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is
            Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 19, SqliteExtendedErrorCode: 1555 or 2067 }
            or Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        { throw new BusinessException(409, "Tài khoản đã được tạo trong yêu cầu khác. Vui lòng đăng nhập lại."); }
        return await IssueAsync(user, session, refresh, ct);
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
        return await IssueAsync(user, session, refresh, ct);
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
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Role,
                db.RoleDefinitions.Where(r => r.Id == (u.RoleDefinitionId ?? u.Role)).Select(r => r.Name).FirstOrDefault())).SingleOrDefaultAsync(ct);
    }

    private async Task<AuthSessionResult> IssueAsync(User user, LoginSession session, string refresh, CancellationToken ct)
    {
        var roleName = await db.RoleDefinitions.AsNoTracking().Where(r => r.Id == (user.RoleDefinitionId ?? user.Role))
            .Select(r => r.Name).SingleOrDefaultAsync(ct);
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        Claim[] claims = [new("sub", user.Id.ToString()), new("sid", session.Id.ToString()),
            new("name", user.Name), new("email", user.Email), new("role", user.Role), new("jti", Guid.NewGuid().ToString())];
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, DateTime.UtcNow, expires,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey)), SecurityAlgorithms.HmacSha256));
        return new(new(new JwtSecurityTokenHandler().WriteToken(token), expires, new(user.Id, user.Name, user.Email, user.Role, roleName)), refresh, session.ExpiresAt);
    }
    private static string GenerateRefresh() => Convert.ToHexString(RandomNumberGenerator.GetBytes(48));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
