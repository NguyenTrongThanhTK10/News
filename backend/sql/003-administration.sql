-- Legacy: chi nang cap database dang dung lich su migration cu.
-- Khong chay sau script 001 moi. Xem docs/SQL_SERVER.md.

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021073439_AddAdministration'
)
BEGIN
    ALTER TABLE [Users] ADD [RoleDefinitionId] nvarchar(80) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021073439_AddAdministration'
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
    WHERE [MigrationId] = N'20261021073439_AddAdministration'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BaseRole', N'Name') AND [object_id] = OBJECT_ID(N'[RoleDefinitions]'))
        SET IDENTITY_INSERT [RoleDefinitions] ON;
    EXEC(N'INSERT INTO [RoleDefinitions] ([Id], [BaseRole], [Name])
    VALUES (N''Admin'', N''Admin'', N''Quản trị viên''),
    (N''Author'', N''Author'', N''Tác giả''),
    (N''Editor'', N''Editor'', N''Biên tập viên'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BaseRole', N'Name') AND [object_id] = OBJECT_ID(N'[RoleDefinitions]'))
        SET IDENTITY_INSERT [RoleDefinitions] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021073439_AddAdministration'
)
BEGIN
    UPDATE [Users] SET [RoleDefinitionId] = [Role] WHERE [Role] IN ('Admin', 'Editor', 'Author')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021073439_AddAdministration'
)
BEGIN
    CREATE INDEX [IX_Users_RoleDefinitionId] ON [Users] ([RoleDefinitionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021073439_AddAdministration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RoleDefinitions_Name] ON [RoleDefinitions] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021073439_AddAdministration'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_RoleDefinitions_RoleDefinitionId] FOREIGN KEY ([RoleDefinitionId]) REFERENCES [RoleDefinitions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021073439_AddAdministration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021073439_AddAdministration', N'8.0.32');
END;
GO

COMMIT;
GO

