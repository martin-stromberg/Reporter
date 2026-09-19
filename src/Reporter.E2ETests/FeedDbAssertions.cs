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

    /// <summary>
    /// Polls until the item with the given title (looked up in
    /// <paramref name="databasePath"/>) has a non-empty <c>image_data</c> row in
    /// the separate <c>item_contents</c> table of
    /// <paramref name="contentDatabasePath"/>. The join across the two database
    /// files is resolved in code because they cannot be queried together.
    /// </summary>
    /// <param name="databasePath">The path of the temp <c>reporter.db</c>.</param>
    /// <param name="contentDatabasePath">The path of the temp <c>reporter-content.db</c>.</param>
    /// <param name="title">The item title to look for.</param>
    /// <param name="timeout">An optional timeout overriding the 15 s default.</param>
    /// <returns>Whether a stored image row exists for the item.</returns>
    public static async Task<bool> ItemImageExistsAsync(
        string databasePath,
        string contentDatabasePath,
        string title,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        while (true)
        {
            var itemId = await ReadItemIdAsync(databasePath, title).ConfigureAwait(false);
            if (itemId is not null &&
                await ImageRowExistsAsync(contentDatabasePath, itemId).ConfigureAwait(false))
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

    // Reads the raw id value of the item row with the given title. The value is
    // handed back to SQLite unchanged so the check works regardless of whether
    // the GUID column is stored as TEXT or BLOB.
    private static async Task<object?> ReadItemIdAsync(string databasePath, string title)
    {
        if (!File.Exists(databasePath))
        {
            return null;
        }

        await using var connection = CreateReadOnlyConnection(databasePath);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM items WHERE title = $title";
        command.Parameters.AddWithValue("$title", title);
        return await command.ExecuteScalarAsync().ConfigureAwait(false);
    }

    // Checks the content database for a non-empty image_data row of the item.
    private static async Task<bool> ImageRowExistsAsync(string contentDatabasePath, object itemId)
    {
        if (!File.Exists(contentDatabasePath))
        {
            return false;
        }

        await using var connection = CreateReadOnlyConnection(contentDatabasePath);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM item_contents " +
            "WHERE item_id = $itemId AND image_data IS NOT NULL AND LENGTH(image_data) > 0";
        command.Parameters.AddWithValue("$itemId", itemId);
        var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
        return result is long count && count > 0;
    }

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

        await using var connection = CreateReadOnlyConnection(databasePath);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Parameters.AddWithValue(parameterName, value);
        var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
        return result is long count && count > 0;
    }

    // Pooling is disabled on purpose: a pooled connection keeps the
    // database file open inside the test host after Dispose, which blocks
    // the temp-directory cleanup in the teardown paths.
    private static SqliteConnection CreateReadOnlyConnection(string databasePath)
        => new($"Data Source={databasePath};Mode=ReadOnly;Default Timeout=5;Pooling=False");
}
