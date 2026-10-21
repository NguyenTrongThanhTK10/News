-- Legacy: chi nang cap database dang dung lich su migration cu.
-- Khong chay sau script 001 moi. Xem docs/SQL_SERVER.md.

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021074653_AddReaderAccounts'
)
BEGIN
    ALTER TABLE [Users] ADD [GoogleSubject] nvarchar(255) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021074653_AddReaderAccounts'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Comments]') AND [c].[name] = N'DisplayName');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Comments] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Comments] ALTER COLUMN [DisplayName] nvarchar(100) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021074653_AddReaderAccounts'
)
BEGIN
    ALTER TABLE [Comments] ADD [UserId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021074653_AddReaderAccounts'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BaseRole', N'Name') AND [object_id] = OBJECT_ID(N'[RoleDefinitions]'))
        SET IDENTITY_INSERT [RoleDefinitions] ON;
    EXEC(N'INSERT INTO [RoleDefinitions] ([Id], [BaseRole], [Name])
    VALUES (N''Reader'', N''Reader'', N''Độc giả'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BaseRole', N'Name') AND [object_id] = OBJECT_ID(N'[RoleDefinitions]'))
        SET IDENTITY_INSERT [RoleDefinitions] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021074653_AddReaderAccounts'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_GoogleSubject] ON [Users] ([GoogleSubject]) WHERE [GoogleSubject] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021074653_AddReaderAccounts'
)
BEGIN
    CREATE INDEX [IX_Comments_UserId] ON [Comments] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021074653_AddReaderAccounts'
)
BEGIN
    ALTER TABLE [Comments] ADD CONSTRAINT [FK_Comments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021074653_AddReaderAccounts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021074653_AddReaderAccounts', N'8.0.32');
END;
GO

COMMIT;
GO

