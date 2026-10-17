// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Net;
using System.Text;
using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the tag extraction and tag backfill of
/// <see cref="FeedSyncService"/>: namespaced <c>tags</c> extension elements and
/// standardized Atom/RSS categories land on the stored <see cref="Item"/>.
/// </summary>
public class FeedSyncServiceTests_Tags : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly FakeItemContentStore _contentStore;
    private readonly FeedRepository _feedRepository;
    private readonly ItemRepository _itemRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSyncServiceTests_Tags"/> class.
    /// </summary>
    public FeedSyncServiceTests_Tags()
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
    /// Verifies that an Atom entry's <c>&lt;app:tags&gt;</c> element is split
    /// into the normalized tag list stored on the item.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_AppTags_StoredOnItem()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.AtomWithTags(
        [
            ("Tagged Item", "https://example.com/1", "id-1", DateTime.UtcNow, "Article content", "SWB, Aufsichtsrat, Stadtbahnen", null, null),
        ]);

        await CreateService(xml).SyncFeedAsync(feedId);

        var item = Assert.Single(await _itemRepository.GetByFeedAsync(feedId));
        Assert.Equal(new[] { "SWB", "Aufsichtsrat", "Stadtbahnen" }, item.Tags);
    }

    /// <summary>
    /// Verifies that an item without a <c>tags</c> element and without
    /// categories is stored with an empty tag list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_NoTags_ItemHasNoTags()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.AtomWithTags(
        [
            ("Plain Item", "https://example.com/1", "id-1", DateTime.UtcNow, "Article content", null, null, null),
        ]);

        await CreateService(xml).SyncFeedAsync(feedId);

        var item = Assert.Single(await _itemRepository.GetByFeedAsync(feedId));
        Assert.Empty(item.Tags);
    }

    /// <summary>
    /// Verifies that whitespace around tag terms is trimmed and empty entries
    /// of the comma-separated list are dropped.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_Tags_TrimsAndDropsEmptyEntries()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.AtomWithTags(
        [
            ("Tagged Item", "https://example.com/1", "id-1", DateTime.UtcNow, "Article content", "SWB, , Stadtbahnen ,", null, null),
        ]);

        await CreateService(xml).SyncFeedAsync(feedId);

        var item = Assert.Single(await _itemRepository.GetByFeedAsync(feedId));
        Assert.Equal(new[] { "SWB", "Stadtbahnen" }, item.Tags);
    }

    /// <summary>
    /// Verifies that a <c>tags</c> element in a foreign namespace is picked up
    /// as well — the extraction matches on the local name only.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_TagsInOtherNamespace_Stored()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.AtomWithTags(
        [
            ("Tagged Item", "https://example.com/1", "id-1", DateTime.UtcNow, "Article content", "Alpha, Beta", "http://example.com/foreign", null),
        ]);

        await CreateService(xml).SyncFeedAsync(feedId);

        var item = Assert.Single(await _itemRepository.GetByFeedAsync(feedId));
        Assert.Equal(new[] { "Alpha", "Beta" }, item.Tags);
    }

    /// <summary>
    /// Verifies that an Atom <c>&lt;category term="…"/&gt;</c> element on an
    /// entry lands as a tag on the stored item.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_AtomCategory_StoredAsTag()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.AtomWithTags(
        [
            ("Categorized Item", "https://example.com/1", "id-1", DateTime.UtcNow, "Article content", null, null, new[] { "Politik" }),
        ]);

        await CreateService(xml).SyncFeedAsync(feedId);

        var item = Assert.Single(await _itemRepository.GetByFeedAsync(feedId));
        Assert.Equal(new[] { "Politik" }, item.Tags);
    }

    /// <summary>
    /// Verifies that an RSS <c>&lt;category&gt;</c> element on an item lands
    /// as a tag on the stored item.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_RssCategory_StoredAsTag()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.RssWithTags(
        [
            ("Categorized Item", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description", null, null, new[] { "Politik" }),
        ]);

        await CreateService(xml).SyncFeedAsync(feedId);

        var item = Assert.Single(await _itemRepository.GetByFeedAsync(feedId));
        Assert.Equal(new[] { "Politik" }, item.Tags);
    }

    /// <summary>
    /// Verifies that a term delivered both via <c>&lt;app:tags&gt;</c> and via
    /// <c>&lt;category&gt;</c> is stored only once and that the order of the
    /// first occurrence is kept.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_TagsAndCategory_DuplicatesRemoved()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.AtomWithTags(
        [
            ("Tagged Item", "https://example.com/1", "id-1", DateTime.UtcNow, "Article content", "Politik, Wirtschaft", null, new[] { "Politik" }),
        ]);

        await CreateService(xml).SyncFeedAsync(feedId);

        var item = Assert.Single(await _itemRepository.GetByFeedAsync(feedId));
        Assert.Equal(new[] { "Politik", "Wirtschaft" }, item.Tags);
    }

    /// <summary>
    /// Verifies that a <c>tags</c> element and categories on feed level are
    /// not stored on the item.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_FeedLevelTags_NotStoredOnItem()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.AtomWithTags(
            [("Plain Item", "https://example.com/1", "id-1", DateTime.UtcNow, "Article content", null, null, null)],
            feedTags: "FeedLevel",
            feedCategories: ["FeedCategory"]);

        await CreateService(xml).SyncFeedAsync(feedId);

        var item = Assert.Single(await _itemRepository.GetByFeedAsync(feedId));
        Assert.Empty(item.Tags);
    }

    /// <summary>
    /// Verifies that a second sync of the same item with tags writes them to
    /// the already stored article that had none (tag backfill).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_ExistingItemWithoutTags_BackfillsTags()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var updated = DateTime.UtcNow;
        var withoutTags = TestFeedXml.AtomWithTags(
        [
            ("Item", "https://example.com/1", "id-1", updated, "Article content", null, null, null),
        ]);
        await CreateService(withoutTags).SyncFeedAsync(feedId);

        var withTags = TestFeedXml.AtomWithTags(
        [
            ("Item", "https://example.com/1", "id-1", updated, "Article content", "SWB, Bahn", null, null),
        ]);
        await CreateService(withTags).SyncFeedAsync(feedId);

        var item = Assert.Single(await _itemRepository.GetByFeedAsync(feedId));
        Assert.Equal(new[] { "SWB", "Bahn" }, item.Tags);
    }

    /// <summary>
    /// Verifies that the tag backfill does not overwrite the tags of an item
    /// that already has some.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_ExistingItemWithTags_KeepsTags()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var updated = DateTime.UtcNow;
        var firstTags = TestFeedXml.AtomWithTags(
        [
            ("Item", "https://example.com/1", "id-1", updated, "Article content", "Alt", null, null),
        ]);
        await CreateService(firstTags).SyncFeedAsync(feedId);

        var secondTags = TestFeedXml.AtomWithTags(
        [
            ("Item", "https://example.com/1", "id-1", updated, "Article content", "Neu", null, null),
        ]);
        await CreateService(secondTags).SyncFeedAsync(feedId);

        var item = Assert.Single(await _itemRepository.GetByFeedAsync(feedId));
        Assert.Equal(new[] { "Alt" }, item.Tags);
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
