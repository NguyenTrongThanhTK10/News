using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using News.Infrastructure;
using Xunit;

namespace News.Tests;

public sealed class MigrationTests
{
    [Fact]
    public void SingleInitialMigrationContainsCurrentSchemaAndMatchesModel()
    {
        using var db = new NewsDbContextFactory().CreateDbContext([]);
        var id = Assert.Single(db.Database.GetMigrations());
        Assert.EndsWith("_InitialNewsSchema", id);
        Assert.False(db.Database.HasPendingModelChanges());

        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations[id], db.Database.ProviderName!);
        var tables = migration.UpOperations.OfType<CreateTableOperation>().ToArray();
        Assert.Equal(
            new[] { "Articles", "Categories", "Comments", "Ratings", "RoleDefinitions", "Sessions", "Users" },
            tables.Select(t => t.Name).OrderBy(name => name).ToArray());
        var users = Assert.Single(tables, t => t.Name == "Users");
        Assert.Contains(users.Columns, c => c.Name == "GoogleSubject" && c.IsNullable);
        Assert.Contains(users.Columns, c => c.Name == "RoleDefinitionId");
        var comments = Assert.Single(tables, t => t.Name == "Comments");
        Assert.Contains(comments.Columns, c => c.Name == "UserId" && c.IsNullable);
        Assert.Contains(comments.ForeignKeys, fk => fk.PrincipalTable == "Users");
        var roles = Assert.Single(migration.UpOperations.OfType<InsertDataOperation>(), operation => operation.Table == "RoleDefinitions");
        Assert.Equal(4, roles.Values.GetLength(0));

        var script = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        foreach (var table in tables) Assert.Contains($"CREATE TABLE [{table.Name}]", script);
        Assert.Contains(id, script);
    }
}
