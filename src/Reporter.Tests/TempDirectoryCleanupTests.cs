// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Tests;

/// <summary>
/// Tests the retry and best-effort behavior of <see cref="TempDirectoryCleanup"/>.
/// These tests intentionally exercise real file-system locks and are therefore
/// classified as OS-interface tests.
/// </summary>
public sealed class TempDirectoryCleanupTests
{
    /// <summary>
    /// A populated temporary directory is removed recursively.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    [OsInterface]
    public async Task TryDeleteDirectoryAsync_ExistingDirectory_RemovesIt()
    {
        var directory = CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory, "test.txt"), "content");

        await TempDirectoryCleanup.TryDeleteDirectoryAsync(directory);

        Assert.False(Directory.Exists(directory));
    }

    /// <summary>
    /// A missing directory is already clean and does not require a retry.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    [OsInterface]
    public async Task TryDeleteDirectoryAsync_MissingDirectory_DoesNotThrow()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"reporter-tests-cleanup-{Guid.NewGuid():N}");
        var logMessages = new List<string>();

        await TempDirectoryCleanup.TryDeleteDirectoryAsync(
            directory,
            logMessages.Add,
            _ => throw new InvalidOperationException("A missing directory must not wait for a retry."));

        Assert.Empty(logMessages);
        Assert.False(Directory.Exists(directory));
    }

    /// <summary>
    /// A file lock released before the next attempt is handled by the retry
    /// path and the directory is removed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    [OsInterface]
    public async Task TryDeleteDirectoryAsync_BrieflyLockedFile_RetriesAndRemoves()
    {
        var directory = CreateTempDirectory();
        var stream = new FileStream(
            Path.Combine(directory, "locked.db"),
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None);
        var retryCount = 0;

        try
        {
            await TempDirectoryCleanup.TryDeleteDirectoryAsync(
                directory,
                log: null,
                waitBeforeRetry: _ =>
                {
                    retryCount++;
                    stream.Dispose();
                    return Task.CompletedTask;
                });
        }
        finally
        {
            stream.Dispose();
        }

        Assert.Equal(1, retryCount);
        Assert.False(Directory.Exists(directory));
    }

    /// <summary>
    /// A persistent lock is reported after the deadline but does not throw,
    /// so teardown cannot mask the actual test result.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    [OsInterface]
    public async Task TryDeleteDirectoryAsync_PersistentLockedFile_LogsAndDoesNotThrow()
    {
        var directory = CreateTempDirectory();
        var stream = new FileStream(
            Path.Combine(directory, "locked.db"),
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None);
        var logMessages = new List<string>();
        var retryCount = 0;

        try
        {
            await TempDirectoryCleanup.TryDeleteDirectoryAsync(
                directory,
                logMessages.Add,
                _ =>
                {
                    retryCount++;
                    return Task.CompletedTask;
                });
        }
        finally
        {
            stream.Dispose();
            await TempDirectoryCleanup.TryDeleteDirectoryAsync(directory);
        }

        Assert.Equal(4, retryCount);
        Assert.NotEmpty(logMessages);
        Assert.Contains(logMessages, message => message.Contains("failed", StringComparison.OrdinalIgnoreCase));
        Assert.False(Directory.Exists(directory));
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"reporter-tests-cleanup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
