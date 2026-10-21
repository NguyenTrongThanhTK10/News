using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using News.Application;

namespace News.Infrastructure;

public sealed class GoogleAuthOptions
{
    public string ClientId { get; set; } = "";
}

public sealed class GoogleIdentityVerifier(IOptions<GoogleAuthOptions> options) : IGoogleIdentityVerifier
{
    public async Task<GoogleIdentity?> VerifyAsync(string credential, string nonce, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ClientId)) return null;
        try
        {
            // Thư viện Google kiểm tra chữ ký, issuer, audience và thời hạn bằng khóa công khai Google.
            var payload = await GoogleJsonWebSignature.ValidateAsync(credential,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [options.Value.ClientId] }).WaitAsync(ct);
            if (!payload.EmailVerified || payload.Nonce != nonce || string.IsNullOrWhiteSpace(payload.Subject)
                || payload.Subject.Length > 255 || string.IsNullOrWhiteSpace(payload.Email) || payload.Email.Length > 254)
                return null;
            var name = string.IsNullOrWhiteSpace(payload.Name) ? "Độc giả Google" : payload.Name.Trim();
            return new(payload.Subject, payload.Email.Trim().ToLowerInvariant(), name[..Math.Min(name.Length, 100)]);
        }
        catch (InvalidJwtException) { return null; }
    }
}
