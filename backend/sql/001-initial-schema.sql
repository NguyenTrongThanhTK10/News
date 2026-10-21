IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

-- Migration gop: khoi tao database moi. Database co lich su cu xem docs/SQL_SERVER.md.

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE TABLE [Categories] (
        [Slug] nvarchar(80) NOT NULL,
        [Name] nvarchar(80) NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Slug])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE TABLE [RoleDefinitions] (
        [Id] nvarchar(80) NOT NULL,
        [Name] nvarchar(80) NOT NULL,
        [BaseRole] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_RoleDefinitions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [GoogleSubject] nvarchar(255) NULL,
        [Role] nvarchar(20) NOT NULL,
        [RoleDefinitionId] nvarchar(80) NULL,
        [FailedLogins] int NOT NULL,
        [LockedUntil] datetime2 NULL,
        [Version] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_RoleDefinitions_RoleDefinitionId] FOREIGN KEY ([RoleDefinitionId]) REFERENCES [RoleDefinitions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE TABLE [Articles] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Summary] nvarchar(600) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [CategorySlug] nvarchar(80) NOT NULL,
        [Category] nvarchar(80) NOT NULL,
        [ImageUrl] nvarchar(2000) NOT NULL,
        [PublishedAt] datetime2 NULL,
        [Author] nvarchar(100) NOT NULL,
        [AuthorId] uniqueidentifier NOT NULL,
        [ReadingMinutes] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [ReviewNote] nvarchar(1000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [Version] uniqueidentifier NOT NULL,
        [IsDeleted] bit NOT NULL,
        [SearchText] nvarchar(1200) NOT NULL,
        CONSTRAINT [PK_Articles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Articles_Categories_CategorySlug] FOREIGN KEY ([CategorySlug]) REFERENCES [Categories] ([Slug]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Articles_Users_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE TABLE [Sessions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [RefreshTokenHash] nvarchar(64) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [Version] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Sessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Sessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE TABLE [Comments] (
        [Id] uniqueidentifier NOT NULL,
        [ArticleId] int NOT NULL,
        [UserId] uniqueidentifier NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        [Content] nvarchar(2000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Comments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Comments_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [Articles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Comments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
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
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BaseRole', N'Name') AND [object_id] = OBJECT_ID(N'[RoleDefinitions]'))
        SET IDENTITY_INSERT [RoleDefinitions] ON;
    EXEC(N'INSERT INTO [RoleDefinitions] ([Id], [BaseRole], [Name])
    VALUES (N''Admin'', N''Admin'', N''Quản trị viên''),
    (N''Author'', N''Author'', N''Tác giả''),
    (N''Editor'', N''Editor'', N''Biên tập viên''),
    (N''Reader'', N''Reader'', N''Độc giả'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BaseRole', N'Name') AND [object_id] = OBJECT_ID(N'[RoleDefinitions]'))
        SET IDENTITY_INSERT [RoleDefinitions] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE INDEX [IX_Articles_AuthorId_Status] ON [Articles] ([AuthorId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE INDEX [IX_Articles_CategorySlug] ON [Articles] ([CategorySlug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE INDEX [IX_Articles_IsDeleted_Status_PublishedAt] ON [Articles] ([IsDeleted], [Status], [PublishedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE INDEX [IX_Comments_ArticleId_CreatedAt_Id] ON [Comments] ([ArticleId], [CreatedAt], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE INDEX [IX_Comments_UserId] ON [Comments] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RoleDefinitions_Name] ON [RoleDefinitions] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Sessions_RefreshTokenHash] ON [Sessions] ([RefreshTokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE INDEX [IX_Sessions_UserId] ON [Sessions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_GoogleSubject] ON [Users] ([GoogleSubject]) WHERE [GoogleSubject] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    CREATE INDEX [IX_Users_RoleDefinitionId] ON [Users] ([RoleDefinitionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021080050_InitialNewsSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021080050_InitialNewsSchema', N'8.0.32');
END;
GO

COMMIT;
GO

