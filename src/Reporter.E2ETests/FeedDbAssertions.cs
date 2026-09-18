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
    public static Task<bool> FeedExistsAsync(string databasePath, string url, TimeSpan? timeout = null)
        => PollUntilExistsAsync(
            databasePath,
            timeout,
            path => ExistsCoreAsync(
                path,
                "SELECT COUNT(*) FROM feeds WHERE url = $url",
                "$url",
                url));

    /// <summary>
    /// Polls the <c>categories</c> table until a row with the given name exists
    /// or the timeout elapses. Mirrors the polling structure of
    /// <see cref="FeedExistsAsync"/>.
    /// </summary>
    /// <param name="databasePath">The path of the temp <c>reporter.db</c>.</param>
    /// <param name="name">The category name to look for.</param>
    /// <param name="timeout">An optional timeout overriding the 15 s default.</param>
    /// <returns>Whether a category row with the name exists.</returns>
    public static Task<bool> CategoryExistsAsync(string databasePath, string name, TimeSpan? timeout = null)
        => PollUntilExistsAsync(
            databasePath,
            timeout,
            path => ExistsCoreAsync(
                path,
                "SELECT COUNT(*) FROM categories WHERE name = $name",
                "$name",
                name));

    /// <summary>
    /// Polls the <c>items</c> table until a row with the given title exists or
    /// the timeout elapses. Mirrors the polling structure of
    /// <see cref="FeedExistsAsync"/>.
    /// </summary>
    /// <param name="databasePath">The path of the temp <c>reporter.db</c>.</param>
    /// <param name="title">The item title to look for.</param>
    /// <param name="timeout">An optional timeout overriding the 15 s default.</param>
    /// <returns>Whether an item row with the title exists.</returns>
    public static Task<bool> ItemExistsAsync(string databasePath, string title, TimeSpan? timeout = null)
        => PollUntilExistsAsync(
            databasePath,
            timeout,
            path => ExistsCoreAsync(
                path,
                "SELECT COUNT(*) FROM items WHERE title = $title",
                "$title",
                title));

    // Polls the check until it reports the row or the timeout elapses; the
    // polling absorbs the delay between the UI action and the asynchronous
    // persistence inside the app.
    private static async Task<bool> PollUntilExistsAsync(
        string databasePath,
        TimeSpan? timeout,
        Func<string, Task<bool>> check)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        while (true)
        {
            if (await check(databasePath).ConfigureAwait(false))
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

    // Runs a read-only "does a row match" COUNT query with a single string
    // parameter against the app's database.
    private static async Task<bool> ExistsCoreAsync(
        string databasePath,
        string commandText,
        string parameterName,
        string value)
    {
        if (!File.Exists(databasePath))
        {
            return false;
        }

        // Pooling is disabled on purpose: a pooled connection keeps the
        // database file open inside the test host after Dispose, which blocks
        // the temp-directory cleanup in the teardown paths.
        await using var connection = new SqliteConnection(
            $"Data Source={databasePath};Mode=ReadOnly;Default Timeout=5;Pooling=False");
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Parameters.AddWithValue(parameterName, value);
        var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
        return result is long count && count > 0;
    }
}
