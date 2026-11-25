// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Tests;

/// <summary>
/// Best-effort recursive cleanup for temporary test directories. A cleanup
/// failure is reported to the optional log callback but never replaces the
/// actual test result by throwing from teardown.
/// </summary>
internal static class TempDirectoryCleanup
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Deletes <paramref name="path"/> recursively, retrying briefly when the
    /// operating system still keeps a file or directory handle open.
    /// </summary>
    /// <param name="path">The directory to remove.</param>
    /// <param name="log">An optional callback for cleanup diagnostics.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task TryDeleteDirectoryAsync(string path, Action<string>? log = null)
    {
        return TryDeleteDirectoryAsync(path, log, waitBeforeRetry: null);
    }

    internal static async Task TryDeleteDirectoryAsync(
        string path,
        Action<string>? log,
        Func<int, Task>? waitBeforeRetry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var wait = waitBeforeRetry ?? DefaultWaitBeforeRetryAsync;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }

                return;
            }
            catch (Exception ex) when (IsRetryable(ex) && attempt < MaxAttempts)
            {
                log?.Invoke($"Temp cleanup attempt {attempt}/{MaxAttempts} failed for '{path}': {ex.Message}");
                await wait(attempt).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log?.Invoke($"Temp cleanup failed for '{path}' after attempt {attempt}/{MaxAttempts}: {ex.Message}");
                return;
            }
        }
    }

    private static Task DefaultWaitBeforeRetryAsync(int attempt)
    {
        return Task.Delay(RetryDelay);
    }

    private static bool IsRetryable(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException;
    }
}
