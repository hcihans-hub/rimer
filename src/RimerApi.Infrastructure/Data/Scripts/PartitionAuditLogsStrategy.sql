/*
====================================================================================
PHASE 4: TABLE PARTITIONING STRATEGY (ON -HOLD)
====================================================================================
STATUS: Documented but not implemented. 
ACTIVATE ONLY IF: 
  - AuditLog table grows extremely large (> 50-100GB).
  - Deletion via Hangfire retention job becomes a DB bottleneck (spikes in DTU/CPU).
  - Queries filtered by date become slow despite clustering.

APPROACH: 
We partition the AuditLogs table by [CreatedAt] per Month. 
Instead of costly `DELETE FROM AuditLogs WHERE CreatedAt < 30 Days` commands, 
partitioning allows using `ALTER TABLE ... SWITCH PARTITION` to instantly 
drop millions of old rows with zero lock-escalation and zero transaction log hits.

Below is the execution plan you should run if scale requires it.
====================================================================================
*/

-- 1. Create a Partition Function for monthly boundaries.
-- Assuming production starts in 2026. Data before each date falls into relative bucket.
CREATE PARTITION FUNCTION [PF_AuditLog_CreatedAt] (DATETIME2)
AS RANGE RIGHT FOR VALUES 
(
    '2026-04-01', 
    '2026-05-01', 
    '2026-06-01', 
    '2026-07-01'
    -- (Add more boundaries for future months incrementally via automated SQL Jobs)
);
GO

-- 2. Create a Partition Scheme mapping the function to filegroups.
-- For highest performance, different months can go to different disks. 
-- Here we map them all to [PRIMARY] for simplicity.
CREATE PARTITION SCHEME [PS_AuditLog_CreatedAt]
AS PARTITION [PF_AuditLog_CreatedAt]
ALL TO ([PRIMARY]);
GO

-- 3. Drop existing Clustered Index (since it determines where data lives physically).
-- Assuming EF Core generated a clustered Primary Key 'PK_AuditLogs' on [Id], it must be dropped.
ALTER TABLE [dbo].[AuditLogs] DROP CONSTRAINT [PK_AuditLogs] WITH (ONLINE = ON);
GO

-- 4. Recreate the Clustered Index specifying the Partition Scheme.
-- The partitioning column [CreatedAt] MUST be part of the Clustered Index.
ALTER TABLE [dbo].[AuditLogs] 
ADD CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED 
(
    [Id] ASC,
    [CreatedAt] ASC
) ON [PS_AuditLog_CreatedAt]([CreatedAt]);
GO

/*
====================================================================================
MAINTENANCE (When you need to purge data):
====================================================================================
To instantly delete an entire month of data that is older than 30 days:

1. Create an identical empty staging table.
    CREATE TABLE [dbo].[AuditLogs_Staging] ( ...identical schema... );

2. Switch the oldest partition (e.g., partition 1) to Staging instantly.
    ALTER TABLE [dbo].[AuditLogs] SWITCH PARTITION 1 TO [dbo].[AuditLogs_Staging];

3. Truncate the staging table (0 log operations).
    TRUNCATE TABLE [dbo].[AuditLogs_Staging];
    
4. Merge boundaries for the old partition to clean up metadata.
    ALTER PARTITION FUNCTION [PF_AuditLog_CreatedAt]() MERGE RANGE ('2026-04-01');

====================================================================================
*/
