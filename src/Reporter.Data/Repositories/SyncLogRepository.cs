// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using SyncLogEntity = Reporter.Data.Entities.SyncLog;

namespace Reporter.Data.Repositories;

/// <summary>
/// Provides data access for <see cref="SyncLog"/> domain models.
/// </summary>
public class SyncLogRepository : ISyncLogRepository
{
    private readonly IDbContextFactory<ReporterDbContext> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncLogRepository"/> class.
    /// </summary>
    /// <param name="factory">The database context factory.</param>
    public SyncLogRepository(IDbContextFactory<ReporterDbContext> factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncLog>> GetAllAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.SyncLogs
            .AsNoTracking()
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();
        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<SyncLog?> GetByIdAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.SyncLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task AddAsync(SyncLog syncLog)
    {
        await using var context = await _factory.CreateDbContextAsync();
        context.SyncLogs.Add(MapToEntity(syncLog));
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(SyncLog syncLog)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.SyncLogs.FindAsync(syncLog.Id);
        if (entity is null)
        {
            return;
        }

        entity.FeedId = syncLog.FeedId;
        entity.StartedAt = syncLog.StartedAt;
        entity.FinishedAt = syncLog.FinishedAt;
        entity.Status = syncLog.Status;
        entity.Message = syncLog.Message;
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.SyncLogs.FindAsync(id);
        if (entity is null)
        {
            return;
        }

        context.SyncLogs.Remove(entity);
        await context.SaveChangesAsync();
    }

    private static SyncLog MapToModel(SyncLogEntity entity)
    {
        return new SyncLog
        {
            Id = entity.Id,
            FeedId = entity.FeedId,
            StartedAt = entity.StartedAt,
            FinishedAt = entity.FinishedAt,
            Status = entity.Status,
            Message = entity.Message,
        };
    }

    private static SyncLogEntity MapToEntity(SyncLog model)
    {
        return new SyncLogEntity
        {
            Id = model.Id,
            FeedId = model.FeedId,
            StartedAt = model.StartedAt,
            FinishedAt = model.FinishedAt,
            Status = model.Status,
            Message = model.Message,
        };
    }
}
