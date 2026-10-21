namespace News.Infrastructure;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "TIN24";
    public string Audience { get; set; } = "TIN24.Web";
    public string SigningKey { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 15;
}
