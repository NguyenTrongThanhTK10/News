using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using News.Infrastructure;
using Xunit;

namespace News.Tests;

public sealed class AdministrationSchemaTests
{
    [Fact]
    public async Task UpgradePreservesExistingUsersAndCanRunRepeatedly()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new NewsDbContext(new DbContextOptionsBuilder<NewsDbContext>().UseSqlite(connection).Options);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE "Users" (
                "Id" TEXT PRIMARY KEY, "Email" TEXT NOT NULL, "Name" TEXT NOT NULL,
                "PasswordHash" TEXT NOT NULL, "Role" TEXT NOT NULL, "FailedLogins" INTEGER NOT NULL,
                "LockedUntil" TEXT NULL, "Version" TEXT NOT NULL
            );
            INSERT INTO "Users" VALUES (
                '11111111-1111-1111-1111-111111111111', 'existing@example.com', 'Tài khoản cũ',
                'existing-hash', 'Editor', 2, NULL, '22222222-2222-2222-2222-222222222222'
            );
            """);
        await SqliteAdministrationSchema.UpgradeAsync(db);
        await SqliteAdministrationSchema.UpgradeAsync(db);
        var user = Assert.Single(await db.Users.AsNoTracking().Select(u => new { u.PasswordHash, u.Name, u.FailedLogins, u.RoleDefinitionId, u.Version }).ToArrayAsync());
        Assert.Equal("existing-hash", user.PasswordHash);
        Assert.Equal("Tài khoản cũ", user.Name);
        Assert.Equal(2, user.FailedLogins);
        Assert.Equal("Editor", user.RoleDefinitionId);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), user.Version);
        Assert.Equal(3, await db.RoleDefinitions.CountAsync());
    }
}
