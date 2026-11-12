// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Net;
using System.Text;
using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the title whitespace normalization of synced and listed articles
/// (<see cref="FeedSyncService"/> on ingest, <see cref="ItemRepository"/> for stored items).
/// </summary>
public class FeedSyncServiceTests_TitleNormalization : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly FakeItemContentStore _contentStore;
    private readonly FeedRepository _feedRepository;
    private readonly ItemRepository _itemRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSyncServiceTests_TitleNormalization"/> class.
    /// </summary>
    public FeedSyncServiceTests_TitleNormalization()
    {
        _factory = new TestDbContextFactory();
        _contentStore = new FakeItemContentStore();
        _feedRepository = new FeedRepository(_factory, _contentStore);
        _itemRepository = new ItemRepository(_factory, _contentStore);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that leading and trailing whitespace (e.g. a trailing line break) is removed on ingest.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_TitleWithSurroundingWhitespace_StoresTrimmedTitle()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.Rss([("\n  Padded Title\n", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description")]);

        await CreateService(xml).SyncFeedAsync(feedId);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Equal("Padded Title", Assert.Single(items).Title);
    }

    /// <summary>
    /// Verifies that already stored titles with a trailing line break are listed trimmed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetUnreadByDateAsync_PagedStoredTitleWithLineBreak_ReturnsTrimmedTitle()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        await _itemRepository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Legacy Title\n",
            GuidOrHash = "legacy",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow,
        });

        var result = await _itemRepository.GetUnreadByDateAsync(0, 20);

        Assert.Equal("Legacy Title", Assert.Single(result).Title);
    }

    private FeedSyncService CreateService(string content)
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/rss+xml"),
        });
        var keywordFilter = new KeywordFilter(new KeywordRepository(_factory), new KeywordMatcher());
        return new FeedSyncService(
            _feedRepository,
            _itemRepository,
            new SyncLogRepository(_factory),
            new HttpClient(handler),
            new FakeNotificationService(),
            new FakeNetworkStatusService(),
            keywordFilter,
            new FakeFeedIconService(),
            new FakeItemImageService(),
            _contentStore);
    }
}
