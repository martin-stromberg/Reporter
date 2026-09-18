// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;

namespace Reporter.Core.Services;

/// <summary>
/// Removes read items that exceeded the configured retention period.
/// </summary>
public class RetentionCleanupService : IRetentionCleanupService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IKeywordFilter _keywordFilter;
    private readonly IItemContentStore _contentStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionCleanupService"/> class.
    /// </summary>
    /// <param name="settingsRepository">The settings repository.</param>
    /// <param name="itemRepository">The item repository.</param>
    /// <param name="keywordFilter">The keyword filter used to match stored items against the keywords.</param>
    /// <param name="contentStore">The store holding the item contents.</param>
    public RetentionCleanupService(
        ISettingsRepository settingsRepository,
        IItemRepository itemRepository,
        IKeywordFilter keywordFilter,
        IItemContentStore contentStore)
    {
        _settingsRepository = settingsRepository;
        _itemRepository = itemRepository;
        _keywordFilter = keywordFilter;
        _contentStore = contentStore;
    }

    /// <inheritdoc />
    public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);

        // Die Waisenbereinigung ist unabhaengig von der Aufbewahrungsfrist
        // und laeuft daher auch bei deaktivierter Retention
        // (RetentionDays <= 0).
        await RemoveOrphanedContentAsync(cancellationToken);

        if (settings.RetentionDays <= 0)
        {
            return 0;
        }

        var cutoff = DateTime.UtcNow.AddDays(-settings.RetentionDays);
        var deleted = await _itemRepository.DeleteExpiredAsync(cutoff, cancellationToken);

        var keywordTexts = await _keywordFilter.GetKeywordTextsAsync();
        if (keywordTexts.Count > 0)
        {
            var candidates = await _itemRepository.GetExpiredKeywordCandidatesAsync(cutoff, cancellationToken);
            var matchedIds = candidates
                .Where(i => _keywordFilter.MatchesAny(i.Title, i.ContentHtml, keywordTexts))
                .Select(i => i.Id)
                .ToList();

            if (matchedIds.Count > 0)
            {
                deleted += await _itemRepository.DeleteRangeAsync(matchedIds, cancellationToken);
            }
        }

        return deleted;
    }

    // Content-Zeilen ohne zugehoeriges Item koennen durch Teilfehler oder die
    // Feed-DB-Kaskade zurueckbleiben und werden hier als Sicherheitsnetz
    // aufgeraeumt; der Rueckgabewert von CleanupAsync zaehlt weiterhin nur
    // geloeschte Items.
    private async Task RemoveOrphanedContentAsync(CancellationToken cancellationToken)
    {
        var contentItemIds = await _contentStore.GetItemIdsAsync(cancellationToken);
        if (contentItemIds.Count == 0)
        {
            return;
        }

        var itemIds = await _itemRepository.GetAllIdsAsync(cancellationToken);
        var existingIds = new HashSet<Guid>(itemIds);
        var orphans = contentItemIds.Where(id => !existingIds.Contains(id)).ToList();
        if (orphans.Count > 0)
        {
            await _contentStore.DeleteRangeAsync(orphans, cancellationToken);
        }
    }
}
