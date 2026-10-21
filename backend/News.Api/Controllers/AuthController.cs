using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using News.Application;

namespace News.Api.Controllers;
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth, IConfiguration config, IWebHostEnvironment environment) : ControllerBase
{
    private const string CookieName = "tin24.refresh";
    private const string GoogleNonceCookie = "tin24.google-nonce";

    [HttpPost("register")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        VerifyOrigin();
        Response.Headers.CacheControl = "no-store";
        var result = await auth.RegisterAsync(request, ct);
        SetCookie(result);
        return StatusCode(StatusCodes.Status201Created, result.Response);
    }

    [HttpGet("google/options")]
    public IActionResult GoogleOptions()
    {
        Response.Headers.CacheControl = "no-store";
        var clientId = config["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId)) return Ok(new { clientId = (string?)null, nonce = (string?)null });
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var options = Options;
        options.MaxAge = TimeSpan.FromMinutes(10);
        Response.Cookies.Append(GoogleNonceCookie, nonce, options);
        return Ok(new { clientId, nonce });
    }

    [HttpPost("google")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin(GoogleLoginRequest request, CancellationToken ct)
    {
        VerifyOrigin();
        Response.Headers.CacheControl = "no-store";
        if (string.IsNullOrWhiteSpace(config["Google:ClientId"]))
            return Problem(statusCode: 503, title: "Đăng nhập Google hiện chưa khả dụng. Vui lòng dùng email và mật khẩu.");
        var nonce = Request.Cookies[GoogleNonceCookie];
        if (nonce is null || nonce.Length != 64 || !nonce.All(char.IsAsciiHexDigit))
            return Problem(statusCode: 401, title: "Phiên đăng nhập Google đã hết hạn. Vui lòng mở lại biểu mẫu.");
        var result = await auth.GoogleLoginAsync(request, nonce, ct);
        Response.Cookies.Delete(GoogleNonceCookie, Options);
        SetCookie(result);
        return Ok(result.Response);
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        VerifyOrigin();
        Response.Headers.CacheControl = "no-store";
        var result = await auth.LoginAsync(request, ct);
        if (result is null) return Problem(statusCode: 401, title: "Email hoặc mật khẩu không đúng, hoặc tài khoản tạm khóa.");
        SetCookie(result);
        return Ok(result.Response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        VerifyOrigin();
        var token = Request.Cookies[CookieName];
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var result = await auth.RefreshAsync(token, ct);
        if (result is null) { ClearCookie(); return Unauthorized(); }
        SetCookie(result);
        return Ok(result.Response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        VerifyOrigin();
        Guid? sessionId = Guid.TryParse(User.FindFirstValue("sid"), out var id) ? id : null;
        await auth.RevokeAsync(Request.Cookies[CookieName], sessionId, ct);
        ClearCookie();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct) => Ok(await auth.ValidateSessionAsync(
        Guid.Parse(User.FindFirstValue("sid")!), Guid.Parse(User.FindFirstValue("sub")!), ct));

    private CookieOptions Options => new()
    {
        HttpOnly = true, Secure = !environment.IsDevelopment() && !environment.IsEnvironment("Testing"),
        SameSite = SameSiteMode.Strict, Path = "/api/auth", IsEssential = true,
    };
    private void SetCookie(AuthSessionResult result)
    {
        var options = Options;
        options.Expires = new DateTimeOffset(DateTime.SpecifyKind(result.RefreshExpiresAt, DateTimeKind.Utc));
        Response.Cookies.Append(CookieName, result.RefreshToken, options);
    }
    private void ClearCookie() => Response.Cookies.Delete(CookieName, Options);
    private void VerifyOrigin()
    {
        // Cookie thay đổi phiên cần chống yêu cầu phát sinh từ website khác.
        var origin = Request.Headers.Origin.ToString();
        if (origin.Length == 0) return; // CLI cùng cơ chế cookie, không có browser Origin.
        var allowed = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var ownOrigin = $"{Request.Scheme}://{Request.Host}";
        if (origin != ownOrigin && !allowed.Contains(origin, StringComparer.OrdinalIgnoreCase))
            throw new News.Domain.BusinessException(403, "Nguồn yêu cầu không được phép.");
    }
}
