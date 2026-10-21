using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using News.Application;
using News.Domain;

namespace News.Api.Controllers;

[ApiController]
[Route("api/articles/{id:int:min(1)}")]
public sealed class ArticleFeedbackController(ArticleFeedbackService service, IWebHostEnvironment environment) : ControllerBase
{
    private const string VisitorCookie = "tin24.reader";

    [HttpGet("comments")]
    public async Task<ActionResult<PagedResult<CommentDto>>> GetComments(int id, [FromQuery] CommentQuery query, CancellationToken ct) =>
        Ok(await service.GetCommentsAsync(id, query, ct));

    [HttpPost("comments")]
    [Authorize]
    [EnableRateLimiting("feedback")]
    public async Task<ActionResult<CommentDto>> AddComment(int id, CreateCommentRequest request, CancellationToken ct)
    {
        var author = new CommentAuthor(Guid.Parse(User.FindFirstValue("sub")!), User.Identity!.Name!);
        var comment = await service.AddCommentAsync(id, request, author, ct);
        return StatusCode(StatusCodes.Status201Created, comment);
    }

    [HttpGet("ratings")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<RatingStatistics>> GetRatings(int id, CancellationToken ct) =>
        Ok(await service.GetRatingsAsync(id, GetVisitorKey(ReadVisitorCookie()), ct));

    [HttpPost("ratings")]
    [EnableRateLimiting("feedback")]
    public async Task<ActionResult<RatingStatistics>> Rate(int id, RateArticleRequest request, CancellationToken ct)
    {
        var existing = ReadVisitorCookie();
        var visitor = existing ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var result = await service.RateAsync(id, GetVisitorKey(visitor)!, request, ct);
        if (existing is null)
            Response.Cookies.Append(VisitorCookie, visitor, new CookieOptions
            {
                HttpOnly = true, SameSite = SameSiteMode.Strict,
                Secure = !environment.IsDevelopment() && !environment.IsEnvironment("Testing"),
                Path = "/api/articles", MaxAge = TimeSpan.FromDays(365), IsEssential = true,
            });
        Response.Headers.CacheControl = "no-store";
        return Ok(result);
    }

    private string? ReadVisitorCookie() => Request.Cookies.TryGetValue(VisitorCookie, out var value)
        && value.Length == 64 && value.All(char.IsAsciiHexDigit) ? value : null;

    private static string? GetVisitorKey(string? visitor) => visitor is null ? null
        : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(visitor)));
}
