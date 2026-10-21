using System.Data;
using Microsoft.EntityFrameworkCore;

namespace News.Infrastructure;

// Nâng cấp SQLite Development đang dùng EnsureCreated mà vẫn giữ dữ liệu hiện có.
public static class SqliteAdministrationSchema
{
    public static async Task UpgradeAsync(NewsDbContext db, CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "RoleDefinitions" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_RoleDefinitions" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "BaseRole" TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_RoleDefinitions_Name" ON "RoleDefinitions" ("Name");
            INSERT OR IGNORE INTO "RoleDefinitions" ("Id", "Name", "BaseRole") VALUES
                ('Admin', 'Quản trị viên', 'Admin'), ('Editor', 'Biên tập viên', 'Editor'), ('Author', 'Tác giả', 'Author');
            """, ct);

        var connection = db.Database.GetDbConnection();
        var opened = connection.State != ConnectionState.Open;
        if (opened) await connection.OpenAsync(ct);
        bool hasColumn;
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info('Users')";
            await using var reader = await command.ExecuteReaderAsync(ct);
            hasColumn = false;
            while (await reader.ReadAsync(ct))
                if (reader.GetString(1) == "RoleDefinitionId") hasColumn = true;
        }
        finally { if (opened) await connection.CloseAsync(); }

        if (!hasColumn)
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "Users" ADD COLUMN "RoleDefinitionId" TEXT NULL REFERENCES "RoleDefinitions" ("Id") ON DELETE RESTRICT;
                """, ct);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_Users_RoleDefinitionId" ON "Users" ("RoleDefinitionId");
            UPDATE "Users" SET "RoleDefinitionId" = "Role" WHERE "RoleDefinitionId" IS NULL AND "Role" IN ('Admin', 'Editor', 'Author');
            """, ct);
    }
}
