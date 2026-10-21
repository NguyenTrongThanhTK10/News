using System.Data;
using Microsoft.EntityFrameworkCore;

namespace News.Infrastructure;

public static class SqliteReaderSchema
{
    public static async Task UpgradeAsync(NewsDbContext db, CancellationToken ct = default)
    {
        if (!await HasColumnAsync(db, "Users", "GoogleSubject", ct))
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Users ADD COLUMN GoogleSubject TEXT NULL;", ct);
        if (!await HasColumnAsync(db, "Comments", "UserId", ct))
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Comments ADD COLUMN UserId TEXT NULL REFERENCES Users (Id) ON DELETE RESTRICT;", ct);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_GoogleSubject ON Users (GoogleSubject) WHERE GoogleSubject IS NOT NULL;
            CREATE INDEX IF NOT EXISTS IX_Comments_UserId ON Comments (UserId);
            INSERT OR IGNORE INTO RoleDefinitions (Id, Name, BaseRole) VALUES ('Reader', 'Độc giả', 'Reader');
            """, ct);
    }

    private static async Task<bool> HasColumnAsync(NewsDbContext db, string table, string column, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var opened = connection.State != ConnectionState.Open;
        if (opened) await connection.OpenAsync(ct);
        try
        {
            await using var command = connection.CreateCommand();
            // Tên bảng chỉ đến từ hằng số trong lớp này.
            command.CommandText = $"PRAGMA table_info('{table}')";
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                if (reader.GetString(1) == column) return true;
            return false;
        }
        finally { if (opened) await connection.CloseAsync(); }
    }
}
