using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using News.Domain;
using News.Infrastructure;
using Xunit;

namespace News.Tests;

public sealed class SeedTests
{
    [Fact]
    public async Task ProductionSeedCreatesConfiguredStaffWithoutDemoAndHashesPasswords()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new NewsDbContext(new DbContextOptionsBuilder<NewsDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Seed:DemoData"] = "true", // Không được phép seed demo khi allowDemo=false.
            ["Seed:AdminEmail"] = "admin@example.com", ["Seed:AdminPassword"] = "PrivateAdminPassword!2026",
            ["Seed:StaffUsers:0:Email"] = "editor@example.com", ["Seed:StaffUsers:0:Name"] = "Editor",
            ["Seed:StaffUsers:0:Role"] = "Editor", ["Seed:StaffUsers:0:Password"] = "PrivateEditorPassword!2026",
        }).Build();
        var hasher = new PasswordHasher<User>();
        await DatabaseSeeder.SeedAsync(db, hasher, config, false);
        await DatabaseSeeder.SeedAsync(db, hasher, config, false);
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.Empty(await db.Articles.ToListAsync());
        var editor = await db.Users.SingleAsync(u => u.Email == "editor@example.com");
        Assert.Equal(Roles.Editor, editor.Role);
        Assert.NotEqual("PrivateEditorPassword!2026", editor.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(editor, editor.PasswordHash, "PrivateEditorPassword!2026"));
        Assert.DoesNotContain(await db.Users.ToListAsync(), u => u.Email.EndsWith("@tin24.local"));
    }

    [Fact]
    public async Task EmptyProductionDatabaseRequiresExplicitAdminCredentials()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new NewsDbContext(new DbContextOptionsBuilder<NewsDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var config = new ConfigurationBuilder().Build();
        await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseSeeder.SeedAsync(db, new PasswordHasher<User>(), config, false));
        Assert.Empty(await db.Users.ToListAsync());
    }
}
