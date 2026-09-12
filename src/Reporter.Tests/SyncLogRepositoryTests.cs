// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="SyncLogRepository"/> class.
/// </summary>
public class SyncLogRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SyncLogRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncLogRepositoryTests"/> class.
    /// </summary>
    public SyncLogRepositoryTests()
    {
        _factory = new TestDbContextFactory();
        _repository = new SyncLogRepository(_factory);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that a sync log can be added and retrieved by id.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsSyncLog()
    {
        var syncLog = new SyncLog
        {
            Id = Guid.NewGuid(),
            StartedAt = new DateTime(2026, 1, 1),
            Status = "ok",
            Message = "Success",
        };
        await _repository.AddAsync(syncLog);

        var result = await _repository.GetByIdAsync(syncLog.Id);

        Assert.NotNull(result);
        Assert.Equal("ok", result.Status);
        Assert.Equal("Success", result.Message);
    }

    /// <summary>
    /// Verifies that GetAllAsync returns all added sync logs ordered by StartedAt descending.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAllAsync_ReturnsSyncLogsOrderedByStartedAtDescending()
    {
        await _repository.AddAsync(new SyncLog
        {
            Id = Guid.NewGuid(),
            StartedAt = new DateTime(2026, 1, 1),
            Status = "older",
        });
        await _repository.AddAsync(new SyncLog
        {
            Id = Guid.NewGuid(),
            StartedAt = new DateTime(2026, 1, 2),
            Status = "newer",
        });

        var result = await _repository.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("newer", result[0].Status);
        Assert.Equal("older", result[1].Status);
    }

    /// <summary>
    /// Verifies that UpdateAsync persists changes.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var syncLog = new SyncLog
        {
            Id = Guid.NewGuid(),
            StartedAt = new DateTime(2026, 1, 1),
            Status = "old",
        };
        await _repository.AddAsync(syncLog);

        await _repository.UpdateAsync(new SyncLog
        {
            Id = syncLog.Id,
            StartedAt = new DateTime(2026, 1, 1),
            Status = "new",
            Message = "Updated",
        });
        var result = await _repository.GetByIdAsync(syncLog.Id);

        Assert.NotNull(result);
        Assert.Equal("new", result.Status);
        Assert.Equal("Updated", result.Message);
    }

    /// <summary>
    /// Verifies that DeleteAsync removes the sync log.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAsync_RemovesSyncLog()
    {
        var syncLog = new SyncLog
        {
            Id = Guid.NewGuid(),
            Status = "delete",
        };
        await _repository.AddAsync(syncLog);

        await _repository.DeleteAsync(syncLog.Id);
        var result = await _repository.GetByIdAsync(syncLog.Id);

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetByIdAsync returns null for a non-existing id.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
