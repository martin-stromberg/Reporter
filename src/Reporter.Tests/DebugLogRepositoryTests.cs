// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="DebugLogRepository"/> class.
/// </summary>
public class DebugLogRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly DebugLogRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugLogRepositoryTests"/> class.
    /// </summary>
    public DebugLogRepositoryTests()
    {
        _factory = new TestDbContextFactory();
        _repository = new DebugLogRepository(_factory);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private static DebugLogEntry CreateEntry(DateTime timestamp, string message, string level = DebugLogLevel.Info)
    {
        return new DebugLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = timestamp,
            Level = level,
            Category = DebugLogCategory.Lifecycle,
            Message = message,
            Details = "details-" + message,
        };
    }

    /// <summary>
    /// Verifies that AddAsync persists all fields of a debug log entry.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddAsync_PersistsAllFields()
    {
        var timestamp = new DateTime(2026, 9, 14, 8, 0, 0, DateTimeKind.Utc);
        var entry = CreateEntry(timestamp, "hello");

        await _repository.AddAsync(entry);

        var entries = await _repository.GetAllAsync();
        var stored = Assert.Single(entries);
        Assert.Equal(entry.Id, stored.Id);
        Assert.Equal(timestamp, stored.Timestamp);
        Assert.Equal(DebugLogLevel.Info, stored.Level);
        Assert.Equal(DebugLogCategory.Lifecycle, stored.Category);
        Assert.Equal("hello", stored.Message);
        Assert.Equal("details-hello", stored.Details);
    }

    /// <summary>
    /// Verifies that GetAllAsync returns the entries ordered by timestamp descending.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAllAsync_OrdersByTimestampDescending()
    {
        var now = DateTime.UtcNow;
        await _repository.AddAsync(CreateEntry(now.AddMinutes(-2), "oldest"));
        await _repository.AddAsync(CreateEntry(now, "newest"));
        await _repository.AddAsync(CreateEntry(now.AddMinutes(-1), "middle"));

        var entries = await _repository.GetAllAsync();

        Assert.Equal(3, entries.Count);
        Assert.Equal("newest", entries[0].Message);
        Assert.Equal("middle", entries[1].Message);
        Assert.Equal("oldest", entries[2].Message);
    }

    /// <summary>
    /// Verifies that GetLatestAsync returns only the newest entries, ordered by timestamp descending.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetLatestAsync_ReturnsNewestEntriesLimited()
    {
        var now = DateTime.UtcNow;
        await _repository.AddAsync(CreateEntry(now.AddMinutes(-2), "oldest"));
        await _repository.AddAsync(CreateEntry(now, "newest"));
        await _repository.AddAsync(CreateEntry(now.AddMinutes(-1), "middle"));

        var entries = await _repository.GetLatestAsync(2);

        Assert.Equal(2, entries.Count);
        Assert.Equal("newest", entries[0].Message);
        Assert.Equal("middle", entries[1].Message);
    }

    /// <summary>
    /// Verifies that DeleteAllExceptErrorsAsync removes every stored entry that is
    /// not an error, so crash reports survive the session reset.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAllExceptErrorsAsync_RemovesNonErrorEntries()
    {
        var now = DateTime.UtcNow;
        await _repository.AddAsync(CreateEntry(now, "info-entry", DebugLogLevel.Info));
        await _repository.AddAsync(CreateEntry(now.AddSeconds(1), "warning-entry", DebugLogLevel.Warning));
        await _repository.AddAsync(CreateEntry(now.AddSeconds(2), "crash-entry", DebugLogLevel.Error));

        await _repository.DeleteAllExceptErrorsAsync();

        var entries = await _repository.GetAllAsync();
        var entry = Assert.Single(entries);
        Assert.Equal("crash-entry", entry.Message);
        Assert.Equal(DebugLogLevel.Error, entry.Level);
    }

    /// <summary>
    /// Verifies that DeleteAllExceptErrorsAsync on an empty table is a no-op.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAllExceptErrorsAsync_EmptyTable_KeepsEmpty()
    {
        await _repository.DeleteAllExceptErrorsAsync();

        var entries = await _repository.GetAllAsync();
        Assert.Empty(entries);
    }

    /// <summary>
    /// Verifies that TrimToLatestAsync keeps only the newest entries.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TrimToLatestAsync_KeepsNewestEntries()
    {
        var now = DateTime.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            await _repository.AddAsync(CreateEntry(now.AddSeconds(i), $"entry-{i}"));
        }

        await _repository.TrimToLatestAsync(2);

        var entries = await _repository.GetAllAsync();
        Assert.Equal(2, entries.Count);
        Assert.Equal("entry-4", entries[0].Message);
        Assert.Equal("entry-3", entries[1].Message);
    }

    /// <summary>
    /// Verifies that TrimToLatestAsync keeps all entries while the count is below the limit.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TrimToLatestAsync_UnderLimit_KeepsAllEntries()
    {
        var now = DateTime.UtcNow;
        await _repository.AddAsync(CreateEntry(now, "one"));
        await _repository.AddAsync(CreateEntry(now.AddSeconds(1), "two"));

        await _repository.TrimToLatestAsync(5);

        var entries = await _repository.GetAllAsync();
        Assert.Equal(2, entries.Count);
    }
}
