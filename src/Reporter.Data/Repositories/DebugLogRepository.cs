// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Services;
using DebugLogEntryEntity = Reporter.Data.Entities.DebugLogEntry;

namespace Reporter.Data.Repositories;

/// <summary>
/// Provides data access for <see cref="DebugLogEntry"/> domain models.
/// </summary>
public class DebugLogRepository : IDebugLogRepository
{
    private readonly IDbContextFactory<ReporterDbContext> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugLogRepository"/> class.
    /// </summary>
    /// <param name="factory">The database context factory.</param>
    public DebugLogRepository(IDbContextFactory<ReporterDbContext> factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DebugLogEntry>> GetAllAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.DebugLogEntries
            .AsNoTracking()
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DebugLogEntry>> GetLatestAsync(int maxEntries)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.DebugLogEntries
            .AsNoTracking()
            .OrderByDescending(e => e.Timestamp)
            .Take(maxEntries)
            .ToListAsync();
        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task AddAsync(DebugLogEntry entry)
    {
        await using var context = await _factory.CreateDbContextAsync();
        context.DebugLogEntries.Add(MapToEntity(entry));
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAllExceptErrorsAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        await context.DebugLogEntries
            .Where(e => e.Level != DebugLogLevel.Error)
            .ExecuteDeleteAsync();
    }

    /// <inheritdoc />
    public async Task TrimToLatestAsync(int maxEntries)
    {
        await using var context = await _factory.CreateDbContextAsync();
        if (await context.DebugLogEntries.CountAsync() <= maxEntries)
        {
            return;
        }

        var keepIds = await context.DebugLogEntries
            .OrderByDescending(e => e.Timestamp)
            .ThenByDescending(e => e.Id)
            .Take(maxEntries)
            .Select(e => e.Id)
            .ToListAsync();
        await context.DebugLogEntries
            .Where(e => !keepIds.Contains(e.Id))
            .ExecuteDeleteAsync();
    }

    private static DebugLogEntry MapToModel(DebugLogEntryEntity entity)
    {
        return new DebugLogEntry
        {
            Id = entity.Id,
            Timestamp = entity.Timestamp,
            Level = entity.Level,
            Category = entity.Category,
            Message = entity.Message,
            Details = entity.Details,
        };
    }

    private static DebugLogEntryEntity MapToEntity(DebugLogEntry model)
    {
        return new DebugLogEntryEntity
        {
            Id = model.Id,
            Timestamp = model.Timestamp,
            Level = model.Level,
            Category = model.Category,
            Message = model.Message,
            Details = model.Details,
        };
    }
}
