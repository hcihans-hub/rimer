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
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [DepartmentId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [RefreshToken] nvarchar(256) NULL,
        [RefreshTokenExpiry] datetime2 NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NOT NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE TABLE [Tickets] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(4000) NOT NULL,
        [Category] nvarchar(20) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Priority] int NOT NULL DEFAULT 1,
        [CreatorId] uniqueidentifier NOT NULL,
        [AssignedToId] uniqueidentifier NULL,
        [DepartmentId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Tickets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tickets_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Tickets_Users_AssignedToId] FOREIGN KEY ([AssignedToId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Tickets_Users_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE TABLE [TicketHistories] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [TicketId] uniqueidentifier NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [OldValue] nvarchar(1000) NULL,
        [NewValue] nvarchar(1000) NULL,
        [ChangedById] uniqueidentifier NOT NULL,
        [ChangedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_TicketHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TicketHistories_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TicketHistories_Users_ChangedById] FOREIGN KEY ([ChangedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Departments_IsActive] ON [Departments] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Departments_Name] ON [Departments] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TicketHistories_ChangedAt] ON [TicketHistories] ([ChangedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TicketHistories_ChangedById] ON [TicketHistories] ([ChangedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TicketHistories_TicketId] ON [TicketHistories] ([TicketId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_AssignedToId] ON [Tickets] ([AssignedToId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_Category] ON [Tickets] ([Category]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_CreatedAt] ON [Tickets] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_CreatorId] ON [Tickets] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_DepartmentId] ON [Tickets] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_Status] ON [Tickets] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_Status_Category_Department] ON [Tickets] ([Status], [Category], [DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_DepartmentId] ON [Users] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_Role] ON [Users] ([Role]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410220750_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260410220750_InitialCreate', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414190724_SoftDelete'
)
BEGIN
    ALTER TABLE [Users] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414190724_SoftDelete'
)
BEGIN
    ALTER TABLE [Users] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414190724_SoftDelete'
)
BEGIN
    ALTER TABLE [Tickets] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414190724_SoftDelete'
)
BEGIN
    ALTER TABLE [Tickets] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414190724_SoftDelete'
)
BEGIN
    ALTER TABLE [TicketHistories] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414190724_SoftDelete'
)
BEGIN
    ALTER TABLE [TicketHistories] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414190724_SoftDelete'
)
BEGIN
    ALTER TABLE [Departments] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414190724_SoftDelete'
)
BEGIN
    ALTER TABLE [Departments] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414190724_SoftDelete'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260414190724_SoftDelete', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414191551_AddAuditLogs'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] uniqueidentifier NOT NULL,
        [TableName] nvarchar(max) NOT NULL,
        [RecordId] nvarchar(max) NOT NULL,
        [Action] nvarchar(max) NOT NULL,
        [OldValues] nvarchar(max) NOT NULL,
        [NewValues] nvarchar(max) NOT NULL,
        [UserId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414191551_AddAuditLogs'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260414191551_AddAuditLogs', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418190936_AuditLogArchivalFields'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLogs]') AND [c].[name] = N'TableName');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [AuditLogs] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [AuditLogs] ALTER COLUMN [TableName] nvarchar(150) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418190936_AuditLogArchivalFields'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLogs]') AND [c].[name] = N'RecordId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [AuditLogs] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [AuditLogs] ALTER COLUMN [RecordId] nvarchar(150) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418190936_AuditLogArchivalFields'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLogs]') AND [c].[name] = N'Action');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [AuditLogs] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [AuditLogs] ALTER COLUMN [Action] nvarchar(20) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418190936_AuditLogArchivalFields'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD [ArchivedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418190936_AuditLogArchivalFields'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD [CorrelationId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418190936_AuditLogArchivalFields'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD [ErrorMessage] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418190936_AuditLogArchivalFields'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418190936_AuditLogArchivalFields'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD [OccurredAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418190936_AuditLogArchivalFields'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD [Processing] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418190936_AuditLogArchivalFields'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD [Sequence] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418190936_AuditLogArchivalFields'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD [Status] nvarchar(20) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418190936_AuditLogArchivalFields'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AuditLogs_CorrelationId] ON [AuditLogs] ([CorrelationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418190936_AuditLogArchivalFields'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_CreatedAt] ON [AuditLogs] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418190936_AuditLogArchivalFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260418190936_AuditLogArchivalFields', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420200808_AddAuditLogPerformanceOptimization'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLogs]') AND [c].[name] = N'Id');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [AuditLogs] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [AuditLogs] ADD DEFAULT (NEWSEQUENTIALID()) FOR [Id];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420200808_AddAuditLogPerformanceOptimization'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Status_CreatedAt] ON [AuditLogs] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420200808_AddAuditLogPerformanceOptimization'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260420200808_AddAuditLogPerformanceOptimization', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502192122_AddReferenceNo'
)
BEGIN
    ALTER TABLE [Tickets] ADD [ReferenceNo] nvarchar(20) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502192122_AddReferenceNo'
)
BEGIN
    UPDATE Tickets SET ReferenceNo = LEFT(CAST(Id AS NVARCHAR(36)), 8)
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502192122_AddReferenceNo'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Tickets_ReferenceNo] ON [Tickets] ([ReferenceNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502192122_AddReferenceNo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260502192122_AddReferenceNo', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502202831_AddWizardFields'
)
BEGIN
    ALTER TABLE [Tickets] ADD [AttachmentUrl] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502202831_AddWizardFields'
)
BEGIN
    ALTER TABLE [Tickets] ADD [InstitutionName] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502202831_AddWizardFields'
)
BEGIN
    ALTER TABLE [Tickets] ADD [TermsAccepted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502202831_AddWizardFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260502202831_AddWizardFields', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504211527_AddUserChartPermissions'
)
BEGIN
    CREATE TABLE [UserChartPermissions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ChartKey] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_UserChartPermissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504211527_AddUserChartPermissions'
)
BEGIN
    CREATE INDEX [IX_UserChartPermissions_UserId] ON [UserChartPermissions] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504211527_AddUserChartPermissions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserChartPermissions_UserId_ChartKey] ON [UserChartPermissions] ([UserId], [ChartKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504211527_AddUserChartPermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260504211527_AddUserChartPermissions', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504213150_ChartKeyToEnum'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260504213150_ChartKeyToEnum', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504221555_AddPaginationAndSystemLogs'
)
BEGIN
    ALTER TABLE [Users] ADD [CreatedBy] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504221555_AddPaginationAndSystemLogs'
)
BEGIN
    ALTER TABLE [Users] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504221555_AddPaginationAndSystemLogs'
)
BEGIN
    ALTER TABLE [UserChartPermissions] ADD [CreatedBy] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504221555_AddPaginationAndSystemLogs'
)
BEGIN
    ALTER TABLE [UserChartPermissions] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504221555_AddPaginationAndSystemLogs'
)
BEGIN
    ALTER TABLE [Tickets] ADD [CreatedBy] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504221555_AddPaginationAndSystemLogs'
)
BEGIN
    ALTER TABLE [Tickets] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504221555_AddPaginationAndSystemLogs'
)
BEGIN
    ALTER TABLE [TicketHistories] ADD [CreatedBy] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504221555_AddPaginationAndSystemLogs'
)
BEGIN
    ALTER TABLE [TicketHistories] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504221555_AddPaginationAndSystemLogs'
)
BEGIN
    ALTER TABLE [Departments] ADD [CreatedBy] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504221555_AddPaginationAndSystemLogs'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD [CreatedBy] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504221555_AddPaginationAndSystemLogs'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504221555_AddPaginationAndSystemLogs'
)
BEGIN
    CREATE TABLE [SystemLogs] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(100) NULL,
        [Action] nvarchar(100) NOT NULL,
        [Entity] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NULL,
        [Timestamp] datetime2 NOT NULL,
        CONSTRAINT [PK_SystemLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504221555_AddPaginationAndSystemLogs'
)
BEGIN
    CREATE INDEX [IX_SystemLogs_Timestamp] ON [SystemLogs] ([Timestamp]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504221555_AddPaginationAndSystemLogs'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260504221555_AddPaginationAndSystemLogs', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504223601_AddSystemLogDetails'
)
BEGIN
    ALTER TABLE [SystemLogs] ADD [Details] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504223601_AddSystemLogDetails'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260504223601_AddSystemLogDetails', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    ALTER TABLE [Tickets] ADD [AssignedDepartmentId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    ALTER TABLE [Tickets] ADD [LastTransferredAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [UserId] uniqueidentifier NOT NULL,
        [Message] nvarchar(500) NOT NULL,
        [IsRead] bit NOT NULL,
        [TicketId] uniqueidentifier NULL,
        [Type] nvarchar(50) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedBy] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    CREATE TABLE [TicketReplies] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [TicketId] uniqueidentifier NOT NULL,
        [AuthorId] uniqueidentifier NOT NULL,
        [Message] nvarchar(4000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedBy] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_TicketReplies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TicketReplies_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TicketReplies_Users_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    CREATE TABLE [TicketTransfers] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [TicketId] uniqueidentifier NOT NULL,
        [FromDepartmentId] uniqueidentifier NOT NULL,
        [ToDepartmentId] uniqueidentifier NOT NULL,
        [Note] nvarchar(1000) NULL,
        [TransferredById] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedBy] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_TicketTransfers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TicketTransfers_Departments_FromDepartmentId] FOREIGN KEY ([FromDepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TicketTransfers_Departments_ToDepartmentId] FOREIGN KEY ([ToDepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TicketTransfers_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    CREATE INDEX [IX_Tickets_AssignedDepartmentId] ON [Tickets] ([AssignedDepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    CREATE INDEX [IX_Tickets_LastTransferredAt] ON [Tickets] ([LastTransferredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    CREATE INDEX [IX_TicketReplies_AuthorId] ON [TicketReplies] ([AuthorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    CREATE INDEX [IX_TicketReplies_TicketId] ON [TicketReplies] ([TicketId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    CREATE INDEX [IX_TicketTransfers_FromDepartmentId] ON [TicketTransfers] ([FromDepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    CREATE INDEX [IX_TicketTransfers_TicketId] ON [TicketTransfers] ([TicketId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    CREATE INDEX [IX_TicketTransfers_ToDepartmentId] ON [TicketTransfers] ([ToDepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    ALTER TABLE [Tickets] ADD CONSTRAINT [FK_Tickets_Departments_AssignedDepartmentId] FOREIGN KEY ([AssignedDepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505173708_AddDepartmentAuthAndNotifications'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260505173708_AddDepartmentAuthAndNotifications', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508171212_UpdateTicketEnumToInt'
)
BEGIN
    UPDATE Tickets SET Status = '0' WHERE Status = 'Open'
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508171212_UpdateTicketEnumToInt'
)
BEGIN
    UPDATE Tickets SET Status = '1' WHERE Status = 'InProgress'
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508171212_UpdateTicketEnumToInt'
)
BEGIN
    UPDATE Tickets SET Status = '2' WHERE Status = 'Closed'
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508171212_UpdateTicketEnumToInt'
)
BEGIN
    UPDATE Tickets SET Category = '0' WHERE Category = 'Complaint'
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508171212_UpdateTicketEnumToInt'
)
BEGIN
    UPDATE Tickets SET Category = '1' WHERE Category = 'Suggestion'
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508171212_UpdateTicketEnumToInt'
)
BEGIN
    UPDATE Tickets SET Category = '2' WHERE Category = 'Request'
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508171212_UpdateTicketEnumToInt'
)
BEGIN
    UPDATE Tickets SET Category = '3' WHERE Category = 'Thanks'
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508171212_UpdateTicketEnumToInt'
)
BEGIN
    UPDATE Tickets SET Category = '4' WHERE Category = 'Question'
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508171212_UpdateTicketEnumToInt'
)
BEGIN
    UPDATE Tickets SET Category = '5' WHERE Category = 'InfoRequest'
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508171212_UpdateTicketEnumToInt'
)
BEGIN
    DROP INDEX [IX_Tickets_Status] ON [Tickets];
    DROP INDEX [IX_Tickets_Status_Category_Department] ON [Tickets];
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tickets]') AND [c].[name] = N'Status');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Tickets] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Tickets] ALTER COLUMN [Status] int NOT NULL;
    CREATE INDEX [IX_Tickets_Status] ON [Tickets] ([Status]);
    CREATE INDEX [IX_Tickets_Status_Category_Department] ON [Tickets] ([Status], [Category], [DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508171212_UpdateTicketEnumToInt'
)
BEGIN
    DROP INDEX [IX_Tickets_Category] ON [Tickets];
    DROP INDEX [IX_Tickets_Status_Category_Department] ON [Tickets];
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tickets]') AND [c].[name] = N'Category');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Tickets] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Tickets] ALTER COLUMN [Category] int NOT NULL;
    CREATE INDEX [IX_Tickets_Category] ON [Tickets] ([Category]);
    CREATE INDEX [IX_Tickets_Status_Category_Department] ON [Tickets] ([Status], [Category], [DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508171212_UpdateTicketEnumToInt'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508171212_UpdateTicketEnumToInt', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512164253_AddDepartmentType'
)
BEGIN
    ALTER TABLE [Departments] ADD [Type] nvarchar(30) NOT NULL DEFAULT N'Other';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512164253_AddDepartmentType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512164253_AddDepartmentType', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512185338_AddExternalTicketSupport'
)
BEGIN
    ALTER TABLE [Users] ADD [ExternalId] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512185338_AddExternalTicketSupport'
)
BEGIN
    ALTER TABLE [Users] ADD [LastSyncedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512185338_AddExternalTicketSupport'
)
BEGIN
    ALTER TABLE [Users] ADD [PersonType] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512185338_AddExternalTicketSupport'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tickets]') AND [c].[name] = N'CreatorId');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Tickets] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Tickets] ALTER COLUMN [CreatorId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512185338_AddExternalTicketSupport'
)
BEGIN
    ALTER TABLE [Tickets] ADD [GuestEmail] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512185338_AddExternalTicketSupport'
)
BEGIN
    ALTER TABLE [Tickets] ADD [GuestName] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512185338_AddExternalTicketSupport'
)
BEGIN
    ALTER TABLE [Tickets] ADD [GuestPhone] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512185338_AddExternalTicketSupport'
)
BEGIN
    ALTER TABLE [Tickets] ADD [IsExternal] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512185338_AddExternalTicketSupport'
)
BEGIN
    ALTER TABLE [Tickets] ADD [TrackingCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512185338_AddExternalTicketSupport'
)
BEGIN
    CREATE INDEX [IX_Tickets_IsExternal] ON [Tickets] ([IsExternal]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512185338_AddExternalTicketSupport'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Tickets_TrackingCode] ON [Tickets] ([TrackingCode]) WHERE [TrackingCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512185338_AddExternalTicketSupport'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512185338_AddExternalTicketSupport', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512205110_AddGuestFieldsToTicket'
)
BEGIN
    ALTER TABLE [Tickets] ADD [ContainsSensitiveData] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512205110_AddGuestFieldsToTicket'
)
BEGIN
    ALTER TABLE [Tickets] ADD [GuestAddress] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512205110_AddGuestFieldsToTicket'
)
BEGIN
    ALTER TABLE [Tickets] ADD [GuestIdentityNumber] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512205110_AddGuestFieldsToTicket'
)
BEGIN
    ALTER TABLE [Tickets] ADD [GuestSurname] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512205110_AddGuestFieldsToTicket'
)
BEGIN
    ALTER TABLE [Tickets] ADD [GuestTitle] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512205110_AddGuestFieldsToTicket'
)
BEGIN
    ALTER TABLE [Tickets] ADD [HidePersonalInfo] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512205110_AddGuestFieldsToTicket'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512205110_AddGuestFieldsToTicket', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514193307_AddUserLastLoginAt'
)
BEGIN
    ALTER TABLE [Users] ADD [LastLoginAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514193307_AddUserLastLoginAt'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260514193307_AddUserLastLoginAt', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514201433_RemoveDepartmentType'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Departments]') AND [c].[name] = N'Type');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Departments] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [Departments] DROP COLUMN [Type];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514201433_RemoveDepartmentType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260514201433_RemoveDepartmentType', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515182318_AdvancedWorkflow'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tickets]') AND [c].[name] = N'Priority');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Tickets] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [Tickets] ADD DEFAULT 2 FOR [Priority];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515182318_AdvancedWorkflow'
)
BEGIN
    ALTER TABLE [Tickets] ADD [InternalStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515182318_AdvancedWorkflow'
)
BEGIN
    CREATE TABLE [TicketReminders] (
        [Id] uniqueidentifier NOT NULL,
        [TicketId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ReminderAt] datetime2 NOT NULL,
        [IsDismissed] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_TicketReminders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TicketReminders_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515182318_AdvancedWorkflow'
)
BEGIN
    CREATE INDEX [IX_TicketReminders_TicketId] ON [TicketReminders] ([TicketId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515182318_AdvancedWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260515182318_AdvancedWorkflow', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260531170559_AddFirstNameLastName'
)
BEGIN
    ALTER TABLE [Users] ADD [FirstName] nvarchar(75) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260531170559_AddFirstNameLastName'
)
BEGIN
    ALTER TABLE [Users] ADD [LastName] nvarchar(75) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260531170559_AddFirstNameLastName'
)
BEGIN

                    UPDATE Users
                    SET 
                        FirstName = CASE 
                            WHEN CHARINDEX(' ', TRIM(FullName)) > 0 
                            THEN SUBSTRING(TRIM(FullName), 1, LEN(TRIM(FullName)) - CHARINDEX(' ', REVERSE(TRIM(FullName))))
                            ELSE TRIM(FullName)
                        END,
                        LastName = CASE 
                            WHEN CHARINDEX(' ', TRIM(FullName)) > 0 
                            THEN SUBSTRING(TRIM(FullName), LEN(TRIM(FullName)) - CHARINDEX(' ', REVERSE(TRIM(FullName))) + 2, LEN(TRIM(FullName)))
                            ELSE ''
                        END
                    WHERE FullName IS NOT NULL AND FullName <> ''
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260531170559_AddFirstNameLastName'
)
BEGIN
    UPDATE Users SET FirstName = '' WHERE FirstName IS NULL
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260531170559_AddFirstNameLastName'
)
BEGIN
    UPDATE Users SET LastName = '' WHERE LastName IS NULL
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260531170559_AddFirstNameLastName'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'FirstName');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var9 + ';');
    EXEC(N'UPDATE [Users] SET [FirstName] = N'''' WHERE [FirstName] IS NULL');
    ALTER TABLE [Users] ALTER COLUMN [FirstName] nvarchar(75) NOT NULL;
    ALTER TABLE [Users] ADD DEFAULT N'' FOR [FirstName];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260531170559_AddFirstNameLastName'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'LastName');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var10 + ';');
    EXEC(N'UPDATE [Users] SET [LastName] = N'''' WHERE [LastName] IS NULL');
    ALTER TABLE [Users] ALTER COLUMN [LastName] nvarchar(75) NOT NULL;
    ALTER TABLE [Users] ADD DEFAULT N'' FOR [LastName];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260531170559_AddFirstNameLastName'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'FullName');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [Users] DROP COLUMN [FullName];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260531170559_AddFirstNameLastName'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260531170559_AddFirstNameLastName', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614140730_AddDepartmentAndNoteToTicketHistory'
)
BEGIN
    ALTER TABLE [TicketHistories] ADD [DepartmentName] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614140730_AddDepartmentAndNoteToTicketHistory'
)
BEGIN
    ALTER TABLE [TicketHistories] ADD [Note] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614140730_AddDepartmentAndNoteToTicketHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614140730_AddDepartmentAndNoteToTicketHistory', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622224627_UpdateTicketReminderIdAndNote'
)
BEGIN
    DROP TABLE [TicketReminders];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622224627_UpdateTicketReminderIdAndNote'
)
BEGIN
    CREATE TABLE [TicketReminders] (
        [Id] int NOT NULL IDENTITY,
        [TicketId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Note] nvarchar(250) NULL,
        [ReminderAt] datetime2 NOT NULL,
        [IsDismissed] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_TicketReminders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TicketReminders_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622224627_UpdateTicketReminderIdAndNote'
)
BEGIN
    CREATE INDEX [IX_TicketReminders_TicketId] ON [TicketReminders] ([TicketId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622224627_UpdateTicketReminderIdAndNote'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260622224627_UpdateTicketReminderIdAndNote', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622230904_AnnouncementTable'
)
BEGIN
    CREATE TABLE [Announcements] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(150) NOT NULL,
        [Content] nvarchar(1000) NOT NULL,
        [PublishedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        [TargetAudience] nvarchar(max) NOT NULL,
        [TargetDepartmentId] uniqueidentifier NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Announcements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Announcements_Departments_TargetDepartmentId] FOREIGN KEY ([TargetDepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622230904_AnnouncementTable'
)
BEGIN
    CREATE INDEX [IX_Announcements_TargetDepartmentId] ON [Announcements] ([TargetDepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622230904_AnnouncementTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260622230904_AnnouncementTable', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    DROP INDEX [IX_Tickets_AssignedDepartmentId] ON [Tickets];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    DROP INDEX [IX_Tickets_AssignedToId] ON [Tickets];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    DROP INDEX [IX_Tickets_Category] ON [Tickets];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    DROP INDEX [IX_Tickets_CreatorId] ON [Tickets];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    DROP INDEX [IX_Tickets_IsExternal] ON [Tickets];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    DROP INDEX [IX_Tickets_Status] ON [Tickets];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    DROP INDEX [IX_Tickets_Status_Category_Department] ON [Tickets];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    DROP INDEX [IX_TicketReplies_TicketId] ON [TicketReplies];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    DROP INDEX [IX_TicketHistories_TicketId] ON [TicketHistories];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    DROP INDEX [IX_Notifications_UserId] ON [Notifications];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    DROP INDEX [IX_Notifications_UserId_IsRead] ON [Notifications];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    CREATE INDEX [IX_Tickets_AssignedDept_CreatedAt] ON [Tickets] ([AssignedDepartmentId], [CreatedAt] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    CREATE INDEX [IX_Tickets_AssignedDept_Status] ON [Tickets] ([AssignedDepartmentId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    CREATE INDEX [IX_Tickets_AssignedToId_Status] ON [Tickets] ([AssignedToId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    CREATE INDEX [IX_Tickets_Category_CreatedAt] ON [Tickets] ([Category], [CreatedAt] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    CREATE INDEX [IX_Tickets_CreatorId_CreatedAt] ON [Tickets] ([CreatorId], [CreatedAt] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    CREATE INDEX [IX_Tickets_DepartmentId_Status] ON [Tickets] ([DepartmentId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    CREATE INDEX [IX_Tickets_Status_CreatedAt] ON [Tickets] ([Status], [CreatedAt] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    CREATE INDEX [IX_TicketReplies_TicketId_CreatedAt] ON [TicketReplies] ([TicketId], [CreatedAt] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    CREATE INDEX [IX_TicketReminders_IsDismissed_ReminderAt] ON [TicketReminders] ([IsDismissed], [ReminderAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    CREATE INDEX [IX_TicketHistories_TicketId_CreatedAt] ON [TicketHistories] ([TicketId], [CreatedAt] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_IsRead_CreatedAt] ON [Notifications] ([UserId], [IsRead], [CreatedAt] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625194522_OptimizeDatabaseIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260625194522_OptimizeDatabaseIndexes', N'10.0.5');
END;

COMMIT;
GO

