using System.ComponentModel.DataAnnotations;

namespace News.Application;

public sealed class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)] public string Name { get; init; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; init; } = "";
    [Required, StringLength(200, MinimumLength = 12)] public string Password { get; init; } = "";
}

public sealed class GoogleLoginRequest
{
    [Required, StringLength(10_000)] public string Credential { get; init; } = "";
}

public sealed record GoogleIdentity(string Subject, string Email, string Name);
public sealed record CommentAuthor(Guid Id, string Name);

public interface IGoogleIdentityVerifier
{
    Task<GoogleIdentity?> VerifyAsync(string credential, string nonce, CancellationToken ct);
}
