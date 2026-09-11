using Reporter.Core.Interfaces;

namespace Reporter.Core.Services;

/// <summary>
/// Removes read items that exceeded the configured retention period.
/// </summary>
public class RetentionCleanupService : IRetentionCleanupService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IItemRepository _itemRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionCleanupService"/> class.
    /// </summary>
    /// <param name="settingsRepository">The settings repository.</param>
    /// <param name="itemRepository">The item repository.</param>
    public RetentionCleanupService(ISettingsRepository settingsRepository, IItemRepository itemRepository)
    {
        _settingsRepository = settingsRepository;
        _itemRepository = itemRepository;
    }

    /// <inheritdoc />
    public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);
        if (settings.RetentionDays <= 0)
        {
            return 0;
        }

        var cutoff = DateTime.UtcNow.AddDays(-settings.RetentionDays);
        return await _itemRepository.DeleteExpiredAsync(cutoff, cancellationToken);
    }
}
