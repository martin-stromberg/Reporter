// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Data.Sqlite;

namespace Reporter.E2ETests;

/// <summary>
/// Read-only SQLite checks against the isolated <c>reporter.db</c> the app under
/// test was pointed at via <c>REPORTER_DB_PATH</c>. Writes are never performed —
/// the app owns the database exclusively.
/// </summary>
public static class FeedDbAssertions
{
    /// <summary>
    /// Polls the <c>feeds</c> table until a row with the given URL exists or the
    /// timeout elapses. Polling absorbs the delay between the UI action and the
    /// asynchronous persistence inside the app.
    /// </summary>
    /// <param name="databasePath">The path of the temp <c>reporter.db</c>.</param>
    /// <param name="url">The feed URL to look for.</param>
    /// <param name="timeout">An optional timeout overriding the 15 s default.</param>
    /// <returns>Whether a feed row with the URL exists.</returns>
    public static async Task<bool> FeedExistsAsync(string databasePath, string url, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        while (true)
        {
            if (await ExistsCoreAsync(databasePath, url).ConfigureAwait(false))
            {
                return true;
            }

            if (DateTime.UtcNow >= deadline)
            {
                return false;
            }

            await Task.Delay(500).ConfigureAwait(false);
        }
    }

    private static async Task<bool> ExistsCoreAsync(string databasePath, string url)
    {
        if (!File.Exists(databasePath))
        {
            return false;
        }

        await using var connection = new SqliteConnection(
            $"Data Source={databasePath};Mode=ReadOnly;Default Timeout=5");
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM feeds WHERE url = $url";
        command.Parameters.AddWithValue("$url", url);
        var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
        return result is long count && count > 0;
    }
}
