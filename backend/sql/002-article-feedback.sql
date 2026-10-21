-- Legacy: chi nang cap database dang dung lich su migration cu.
-- Khong chay sau script 001 moi. Xem docs/SQL_SERVER.md.

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021065646_AddArticleFeedback'
)
BEGIN
    CREATE TABLE [Comments] (
        [Id] uniqueidentifier NOT NULL,
        [ArticleId] int NOT NULL,
        [DisplayName] nvarchar(80) NOT NULL,
        [Content] nvarchar(2000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Comments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Comments_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [Articles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021065646_AddArticleFeedback'
)
BEGIN
    CREATE TABLE [Ratings] (
        [ArticleId] int NOT NULL,
        [VisitorKey] nvarchar(64) NOT NULL,
        [Stars] int NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Ratings] PRIMARY KEY ([ArticleId], [VisitorKey]),
        CONSTRAINT [CK_Ratings_Stars] CHECK ([Stars] BETWEEN 1 AND 5),
        CONSTRAINT [FK_Ratings_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [Articles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021065646_AddArticleFeedback'
)
BEGIN
    CREATE INDEX [IX_Comments_ArticleId_CreatedAt_Id] ON [Comments] ([ArticleId], [CreatedAt], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021065646_AddArticleFeedback'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021065646_AddArticleFeedback', N'8.0.32');
END;
GO

COMMIT;
GO

