using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Defines read and write access to the singleton <see cref="Settings"/> record.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    /// Gets the singleton settings record asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the settings.</returns>
    Task<Settings> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the specified settings asynchronously, always updating the singleton record.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveAsync(Settings settings);
}
