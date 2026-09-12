// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Services;

/// <summary>
/// Represents the outcome of a feed synchronization run.
/// </summary>
/// <param name="Status">The resulting health status.</param>
/// <param name="NewItems">The number of newly stored items.</param>
/// <param name="Message">An optional status or error message.</param>
/// <returns>A new <see cref="SyncResult"/> instance.</returns>
public record SyncResult(string Status, int NewItems, string? Message = null);
