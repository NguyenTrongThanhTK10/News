using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using News.Api;
using News.Application;
using News.Domain;
using News.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddDbContext<NewsDbContext>(options =>
{
    var provider = builder.Configuration["Database:Provider"] ?? "SqlServer";
    if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
            throw new InvalidOperationException("SQLite chỉ dành cho Development/Testing. Production cần SQL Server.");
        options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=news-dev.db");
    }
    else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        options.UseSqlServer(builder.Configuration.GetConnectionString("NewsDatabase")
            ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:NewsDatabase."), sql => sql.EnableRetryOnFailure());
    else throw new InvalidOperationException("Database:Provider phải là SqlServer hoặc Sqlite.");
});
builder.Services.AddScoped<IArticleRepository, EfArticleRepository>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<ArticleManagementService>();
builder.Services.AddScoped<IArticleFeedbackRepository, EfArticleFeedbackRepository>();
builder.Services.AddScoped<ArticleFeedbackService>();
builder.Services.AddSingleton<IHtmlContentSanitizer, HtmlContentSanitizer>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddOptions<JwtOptions>().Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(value => Encoding.UTF8.GetByteCount(value.SigningKey) >= 32,
        "Cấu hình Jwt:SigningKey tối thiểu 32 byte bằng user-secrets hoặc biến môi trường.")
    .Validate(value => value.AccessTokenMinutes is >= 1 and <= 60, "Jwt:AccessTokenMinutes cần từ 1 đến 60.")
    .ValidateOnStart();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(15),
        RoleClaimType = "role", NameClaimType = "name", ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var principal = context.Principal!;
            if (!Guid.TryParse(principal.FindFirstValue("sub"), out var userId)
                || !Guid.TryParse(principal.FindFirstValue("sid"), out var sessionId))
            { context.Fail("Phiên không hợp lệ."); return; }
            var user = await context.HttpContext.RequestServices.GetRequiredService<IAuthService>()
                .ValidateSessionAsync(sessionId, userId, context.HttpContext.RequestAborted);
            if (user is null || user.Role != principal.FindFirstValue("role")) context.Fail("Phiên đã hết hạn hoặc bị thu hồi.");
        },
    };
});
builder.Services.AddAuthorization();
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(origins).AllowAnyHeader().WithMethods("GET", "POST", "PUT", "DELETE").AllowCredentials()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("feedback", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Chỉ tự khởi tạo DB khi người vận hành bật rõ ràng; Production dùng migration script.
if (app.Configuration.GetValue<bool>("Database:Initialize"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<NewsDbContext>();
    if (db.Database.IsSqlite())
    {
        await db.Database.EnsureCreatedAsync();
        await SqliteFeedbackSchema.UpgradeAsync(db);
    }
    else await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>(),
        app.Configuration, app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"));
}
app.Run();
public partial class Program;
