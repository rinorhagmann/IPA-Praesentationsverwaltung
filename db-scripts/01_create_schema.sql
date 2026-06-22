IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622075350_InitialCreate'
)
BEGIN
    CREATE TABLE [Rooms] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622075350_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Email] nvarchar(256) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [Role] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CanManageSystem] bit NULL,
        [InitialPasswordSent] bit NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622075350_InitialCreate'
)
BEGIN
    CREATE TABLE [Presentations] (
        [Id] int NOT NULL IDENTITY,
        [Topic] nvarchar(300) NOT NULL,
        [StartsAt] datetime2 NOT NULL,
        [RoomId] int NOT NULL,
        [MaxObservers] int NOT NULL,
        CONSTRAINT [PK_Presentations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Presentations_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622075350_InitialCreate'
)
BEGIN
    CREATE TABLE [Registrations] (
        [Id] int NOT NULL IDENTITY,
        [RegisteredAt] datetime2 NOT NULL,
        [StudentId] int NOT NULL,
        [PresentationId] int NOT NULL,
        CONSTRAINT [PK_Registrations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Registrations_Presentations_PresentationId] FOREIGN KEY ([PresentationId]) REFERENCES [Presentations] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Registrations_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622075350_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Presentations_RoomId] ON [Presentations] ([RoomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622075350_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Registrations_PresentationId] ON [Registrations] ([PresentationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622075350_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Registrations_StudentId_PresentationId] ON [Registrations] ([StudentId], [PresentationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622075350_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Rooms_Name] ON [Rooms] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622075350_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622075350_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260622075350_InitialCreate', N'10.0.0');
END;

COMMIT;
GO

