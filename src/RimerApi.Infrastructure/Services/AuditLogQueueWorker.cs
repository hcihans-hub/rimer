using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RimerApi.Application.DTOs.AuditLog;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Interfaces;
using RimerApi.Infrastructure.Data;
using System.Diagnostics.Metrics;
using System.Data;
using Hangfire;

namespace RimerApi.Infrastructure.Services;

/// <summary>
/// Hangfire worker class physically invoked in background to save AuditLogs.
/// </summary>
public class AuditLogQueueWorker
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditLogQueueWorker> _logger;

    private static readonly Meter _meter = new("RimerApi.AuditLogging", "1.0.0");
    private static readonly Counter<int> _successCounter = _meter.CreateCounter<int>("audit_log_processed_success_total", description: "Successfully processed audit logs.");
    private static readonly Counter<int> _failureCounter = _meter.CreateCounter<int>("audit_log_processed_failure_total", description: "Failed processing audit logs.");
    private static readonly Histogram<int> _batchSizeHistogram = _meter.CreateHistogram<int>("audit_log_batch_size", description: "Distribution of processed audit log batch sizes.");
    private static readonly Histogram<double> _durationHistogram = _meter.CreateHistogram<double>("audit_log_processing_duration_ms", unit: "ms", description: "Duration of audit log batch processing.");

    public AuditLogQueueWorker(IServiceScopeFactory scopeFactory, ILogger<AuditLogQueueWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ProcessBatchAsync(IEnumerable<AuditLogMessageDto> logs)
    {
        var startTime = System.Diagnostics.Stopwatch.GetTimestamp();
        var logsArray = logs as AuditLogMessageDto[] ?? logs.ToArray();
        if (!logsArray.Any()) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var auditOptions = scope.ServiceProvider.GetService<Microsoft.Extensions.Options.IOptions<RimerApi.Application.Common.AuditOptions>>()?.Value;
            int batchSize = auditOptions?.BatchSize ?? 100;

            // STRICT WRITE PATH ISOLATION: Recovery logic removed from hot path.

            var dataTable = PrepareDataTable();
            
            foreach (var chunk in logsArray.Chunk(batchSize))
            {
                _batchSizeHistogram.Record(chunk.Length);
                await InsertBatchAsync(dbContext, dataTable, chunk);
                await Task.Delay(50);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fully process AuditLog batch.");
            throw; // Let Hangfire handle retry/failure state
        }
        finally
        {
            var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
            _durationHistogram.Record(elapsedMs);
        }
    }

    private async Task InsertBatchAsync(ApplicationDbContext dbContext, DataTable dataTable, AuditLogMessageDto[] chunk)
    {
        try
        {
            dataTable.Rows.Clear();
            foreach (var dto in chunk)
            {
                dataTable.Rows.Add(
                    dto.CorrelationId,
                    dto.TableName,
                    dto.RecordId,
                    dto.Action,
                    dto.Status,
                    dto.ErrorMessage ?? (object)DBNull.Value,
                    dto.OldValues ?? (object)DBNull.Value,
                    dto.NewValues ?? (object)DBNull.Value,
                    dto.UserId ?? (object)DBNull.Value,
                    dto.Timestamp,
                    dto.OccurredAt,
                    dto.Sequence,
                    false,
                    DBNull.Value
                );
            }

            var sql = @"
                INSERT INTO AuditLogs (TableName, RecordId, Action, Status, ErrorMessage, OldValues, NewValues, UserId, CreatedAt, UpdatedAt, CorrelationId, OccurredAt, Sequence, Processing, IsArchived, ArchivedAt, IsDeleted)
                SELECT t.TableName, t.RecordId, t.Action, t.Status, t.ErrorMessage, t.OldValues, t.NewValues, t.UserId, t.CreatedAt, t.CreatedAt, t.CorrelationId, t.OccurredAt, t.Sequence, 0, t.IsArchived, t.ArchivedAt, 0
                FROM @tvp t;";

            var param = new Microsoft.Data.SqlClient.SqlParameter("@tvp", SqlDbType.Structured)
            {
                TypeName = "dbo.AuditLogType",
                Value = dataTable
            };

            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var insertedCount = await dbContext.Database.ExecuteSqlRawAsync(sql, param);
                await transaction.CommitAsync();
                _successCounter.Add(insertedCount);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
        {
            // Unique Index Violation (CorrelationId) - Idempotency reached
            _logger.LogWarning("Duplicate CorrelationId detected in batch. Skipping chunk to maintain idempotency.");
        }
        catch (Exception ex)
        {
            _failureCounter.Add(chunk.Length);
            _logger.LogError(ex, "AuditBatch chunk failure in raw SQL TVP processing.");
            throw;
        }
    }

    private DataTable PrepareDataTable()
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("CorrelationId", typeof(Guid));
        dataTable.Columns.Add("TableName", typeof(string));
        dataTable.Columns.Add("RecordId", typeof(string));
        dataTable.Columns.Add("Action", typeof(string));
        dataTable.Columns.Add("Status", typeof(string));
        dataTable.Columns.Add("ErrorMessage", typeof(string));
        dataTable.Columns.Add("OldValues", typeof(string));
        dataTable.Columns.Add("NewValues", typeof(string));
        dataTable.Columns.Add("UserId", typeof(Guid));
        dataTable.Columns.Add("CreatedAt", typeof(DateTime));
        dataTable.Columns.Add("OccurredAt", typeof(DateTime));
        dataTable.Columns.Add("Sequence", typeof(int));
        dataTable.Columns.Add("IsArchived", typeof(bool));
        dataTable.Columns.Add("ArchivedAt", typeof(DateTime));
        return dataTable;
    }
}
