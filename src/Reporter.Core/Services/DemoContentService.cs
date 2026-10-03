// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Core.Services;

/// <summary>
/// Seeds the demo category "News" with the Apple Newsroom feed on the very
/// first application start.
/// </summary>
public class DemoContentService : IDemoContentService
{
    /// <summary>
    /// The name of the seeded demo category.
    /// </summary>
    public const string DemoCategoryName = "News";

    /// <summary>
    /// The URL of the seeded demo feed.
    /// </summary>
    public const string DemoFeedUrl = "https://www.apple.com/newsroom/rss-feed.rss";

    /// <summary>
    /// The title of the seeded demo feed.
    /// </summary>
    public const string DemoFeedTitle = "Apple Newsroom";

    private readonly FirstRunState _firstRunState;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IFeedRepository _feedRepository;
    private readonly IDebugLogService? _debugLogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DemoContentService"/> class.
    /// </summary>
    /// <param name="firstRunState">The first-run detection result.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="feedRepository">The feed repository.</param>
    /// <param name="debugLogService">The optional session debug log service.</param>
    public DemoContentService(
        FirstRunState firstRunState,
        ICategoryRepository categoryRepository,
        IFeedRepository feedRepository,
        IDebugLogService? debugLogService = null)
    {
        _firstRunState = firstRunState;
        _categoryRepository = categoryRepository;
        _feedRepository = feedRepository;
        _debugLogService = debugLogService;
    }

    /// <inheritdoc />
    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_firstRunState.ShouldSeedDemoContent)
        {
            return;
        }

        if (await _feedRepository.GetByUrlAsync(DemoFeedUrl) is not null)
        {
            return;
        }

        var categories = await _categoryRepository.GetAllAsync();
        var newsCategory = categories.FirstOrDefault(
            c => string.Equals(c.Name, DemoCategoryName, StringComparison.OrdinalIgnoreCase));
        var newsCategoryId = newsCategory?.Id ?? Guid.NewGuid();
        if (newsCategory is null)
        {
            await _categoryRepository.AddAsync(new Category { Id = newsCategoryId, Name = DemoCategoryName });
        }

        // The demo feed seeds with notifications off: on the very first sync
        // every article would count as new and trigger one notification each.
        await _feedRepository.AddAsync(new Feed
        {
            Id = Guid.NewGuid(),
            Url = DemoFeedUrl,
            Title = DemoFeedTitle,
            CategoryId = newsCategoryId,
            LastCheckedAt = null,
            HealthStatus = FeedHealth.Ok,
            HealthLastChange = null,
            NotificationsEnabled = false,
            FaviconUrl = null,
            LastMessageKind = null,
            LastMessage = null,
        });

        if (_debugLogService is not null)
        {
            await _debugLogService.LogAsync(DebugLogCategory.Lifecycle, "Demo content seeded", level: DebugLogLevel.Info);
        }
    }
}
