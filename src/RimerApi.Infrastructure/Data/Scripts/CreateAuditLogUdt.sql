-- Run this SQL script exactly to create the UDT before the application runs.
IF TYPE_ID(N'dbo.AuditLogType') IS NULL
BEGIN
   CREATE TYPE [dbo].[AuditLogType] AS TABLE (
    [CorrelationId] UNIQUEIDENTIFIER NOT NULL,
    [TableName] NVARCHAR(150) NOT NULL,
    [RecordId] NVARCHAR(150) NOT NULL,
    [Action] NVARCHAR(20) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL,
    [ErrorMessage] NVARCHAR(2000) NULL,
    [OldValues] NVARCHAR(4000) NULL,
    [NewValues] NVARCHAR(4000) NULL,
    [UserId] UNIQUEIDENTIFIER NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    [OccurredAt] DATETIME2 NOT NULL,
    [Sequence] INT NOT NULL,
    [IsArchived] BIT NOT NULL DEFAULT 0,
    [ArchivedAt] DATETIME2 NULL
);
END
GO
