using Microsoft.EntityFrameworkCore;

namespace News.Infrastructure;

// Development SQLite databases predate migrations and use EnsureCreated.
// Add only the feedback tables without replacing existing articles or accounts.
public static class SqliteFeedbackSchema
{
    public static Task UpgradeAsync(NewsDbContext db, CancellationToken ct = default) => db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "Comments" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_Comments" PRIMARY KEY,
            "ArticleId" INTEGER NOT NULL,
            "DisplayName" TEXT NOT NULL,
            "Content" TEXT NOT NULL,
            "CreatedAt" TEXT NOT NULL,
            CONSTRAINT "FK_Comments_Articles_ArticleId" FOREIGN KEY ("ArticleId") REFERENCES "Articles" ("Id") ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS "IX_Comments_ArticleId_CreatedAt_Id" ON "Comments" ("ArticleId", "CreatedAt", "Id");
        CREATE TABLE IF NOT EXISTS "Ratings" (
            "ArticleId" INTEGER NOT NULL,
            "VisitorKey" TEXT NOT NULL,
            "Stars" INTEGER NOT NULL,
            "UpdatedAt" TEXT NOT NULL,
            CONSTRAINT "PK_Ratings" PRIMARY KEY ("ArticleId", "VisitorKey"),
            CONSTRAINT "CK_Ratings_Stars" CHECK ("Stars" BETWEEN 1 AND 5),
            CONSTRAINT "FK_Ratings_Articles_ArticleId" FOREIGN KEY ("ArticleId") REFERENCES "Articles" ("Id") ON DELETE CASCADE
        );
        """, ct);
}
