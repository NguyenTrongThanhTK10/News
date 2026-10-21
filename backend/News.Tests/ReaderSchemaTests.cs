using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using News.Infrastructure;
using Xunit;

namespace News.Tests;

public sealed class ReaderSchemaTests
{
    [Fact]
    public async Task ExistingAccountsAndAnonymousCommentsSurviveRepeatedUpgrade()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new NewsDbContext(new DbContextOptionsBuilder<NewsDbContext>().UseSqlite(connection).Options);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE Users (
                Id TEXT PRIMARY KEY, Email TEXT NOT NULL, Name TEXT NOT NULL,
                PasswordHash TEXT NOT NULL, Role TEXT NOT NULL, RoleDefinitionId TEXT NULL,
                FailedLogins INTEGER NOT NULL, LockedUntil TEXT NULL, Version TEXT NOT NULL
            );
            CREATE TABLE Comments (Id TEXT PRIMARY KEY, ArticleId INTEGER NOT NULL, DisplayName TEXT NOT NULL, Content TEXT NOT NULL, CreatedAt TEXT NOT NULL);
            CREATE TABLE RoleDefinitions (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, BaseRole TEXT NOT NULL);
            INSERT INTO Users VALUES ('11111111-1111-1111-1111-111111111111', 'old@example.com', 'Tài khoản cũ', 'old-hash', 'Author', NULL, 0, NULL, '22222222-2222-2222-2222-222222222222');
            INSERT INTO Comments VALUES ('33333333-3333-3333-3333-333333333333', 1, 'Độc giả cũ', 'Bình luận trước khi có đăng ký.', '2026-10-21 07:00:00');
            """);
        await SqliteReaderSchema.UpgradeAsync(db);
        await SqliteReaderSchema.UpgradeAsync(db);
        var user = Assert.Single(await db.Users.AsNoTracking().ToArrayAsync());
        Assert.Equal("old-hash", user.PasswordHash);
        Assert.Null(user.GoogleSubject);
        var comment = Assert.Single(await db.Comments.AsNoTracking().ToArrayAsync());
        Assert.Equal("Bình luận trước khi có đăng ký.", comment.Content);
        Assert.Null(comment.UserId);
        Assert.Equal("Reader", (await db.RoleDefinitions.SingleAsync()).Id);
    }
}
