// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;
using Entities = Reporter.Data.Entities;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the reading-packet behavior of the <see cref="UnreadViewModel"/> class:
/// packet resolution on fresh loads, paging inside the packet, packet-scoped
/// "mark all read" and the packet-based counters.
/// Seeds are expressed in local calendar days and converted to UTC the same way the
/// repository converts them back, so the tests are independent of the machine's time zone.
/// </summary>
public class UnreadViewModelTests_Packet : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly ItemRepository _itemRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly SettingsRepository _settingsRepository;
    private readonly UnreadViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnreadViewModelTests_Packet"/> class.
    /// </summary>
    public UnreadViewModelTests_Packet()
    {
        _factory = new TestDbContextFactory();
        _itemRepository = new ItemRepository(_factory, new FakeItemContentStore());
        _categoryRepository = new CategoryRepository(_factory);
        _settingsRepository = new SettingsRepository(_factory);
        _viewModel = new UnreadViewModel(
            _itemRepository,
            _categoryRepository,
            new FakeFeedSyncService(),
            _settingsRepository,
            new FakeNetworkStatusService());
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private static DateTime Utc(DateTime localTime)
        => DateTime.SpecifyKind(localTime, DateTimeKind.Local).ToUniversalTime();

    private async Task<Guid> SeedFeedAsync(string url = "https://example.com/feed", Guid? categoryId = null)
    {
        var feedId = Guid.NewGuid();
        await using var context = _factory.CreateDbContext();
        context.Feeds.Add(new Entities.Feed
        {
            Id = feedId,
            Url = url,
            Title = url,
            CategoryId = categoryId,
        });
        await context.SaveChangesAsync();
        return feedId;
    }

    private async Task<Guid> SeedCategoryAsync(string name)
    {
        var categoryId = Guid.NewGuid();
        await using var context = _factory.CreateDbContext();
        context.Categories.Add(new Entities.Category { Id = categoryId, Name = name });
        await context.SaveChangesAsync();
        return categoryId;
    }

    private async Task SeedUnreadAsync(Guid feedId, DateTime localDay, int count, string prefix)
    {
        for (var i = 0; i < count; i++)
        {
            await _itemRepository.AddAsync(new Item
            {
                Id = Guid.NewGuid(),
                FeedId = feedId,
                Title = $"{prefix} {i + 1}",
                GuidOrHash = $"{prefix}-{i + 1}",
                IsRead = false,
                IsSavedForLater = false,
                PublishedAt = Utc(localDay.Date.AddHours(12).AddMinutes(i)),
            });
        }
    }

    /// <summary>
    /// Verifies that a fresh load shows only the articles of the anchor day's
    /// packet when the anchor day holds enough articles on its own.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_LoadsOnlyAnchorDayArticles()
    {
        var feedId = await SeedFeedAsync();
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 5), 6, "Day1");
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 6), 6, "Day2");

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(6, _viewModel.Articles.Count);
        Assert.All(_viewModel.Articles, a => Assert.StartsWith("Day2", a.Title));
    }

    /// <summary>
    /// Verifies that a fresh load extends the packet to the next occupied day
    /// when the anchor day holds fewer than five articles.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_ExtendsPacket_WhenAnchorBelowMin()
    {
        var feedId = await SeedFeedAsync();
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 5), 3, "Day1");
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 6), 4, "Day2");

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(7, _viewModel.Articles.Count);
        Assert.Equal(3, _viewModel.Articles.Count(a => a.Title.StartsWith("Day1", StringComparison.Ordinal)));
        Assert.Equal(4, _viewModel.Articles.Count(a => a.Title.StartsWith("Day2", StringComparison.Ordinal)));
    }

    /// <summary>
    /// Verifies that marking single articles as read inside a loaded packet does
    /// not pull the follow day in — the below-five decision belongs to fresh
    /// loads only.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkReadCommand_Removal_DoesNotExtendPacket()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending);
        var feedId = await SeedFeedAsync();
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 5), 6, "Day1");
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 6), 4, "Day2");
        await _viewModel.LoadCommand.ExecuteAsync(null);
        Assert.Equal(6, _viewModel.Articles.Count);

        await _viewModel.MarkReadCommand.ExecuteAsync(_viewModel.Articles[0]);
        await _viewModel.MarkReadCommand.ExecuteAsync(_viewModel.Articles[0]);

        Assert.Equal(4, _viewModel.Articles.Count);
        Assert.All(_viewModel.Articles, a => Assert.StartsWith("Day1", a.Title));
    }

    /// <summary>
    /// Verifies that removing the last visible article by marking it as read
    /// triggers a fresh load that resolves and shows the next packet.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkReadCommand_EmptyingList_LoadsNextPacket()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending);
        var feedId = await SeedFeedAsync();
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 5), 6, "Day1");
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 6), 4, "Day2");
        await _viewModel.LoadCommand.ExecuteAsync(null);

        for (var i = 0; i < 6; i++)
        {
            await _viewModel.MarkReadCommand.ExecuteAsync(_viewModel.Articles[0]);
        }

        Assert.Equal(4, _viewModel.Articles.Count);
        Assert.All(_viewModel.Articles, a => Assert.StartsWith("Day2", a.Title));
    }

    /// <summary>
    /// Verifies that "mark all read" marks only the current packet: articles of
    /// other days stay unread and become the next packet.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkAllReadCommand_MarksOnlyCurrentPacket()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending);
        var feedId = await SeedFeedAsync();
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 5), 6, "Day1");
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 6), 4, "Day2");
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.MarkAllReadCommand.ExecuteAsync(null);

        var remaining = await _itemRepository.GetUnreadByDateAsync();
        Assert.Equal(4, remaining.Count);
        Assert.All(remaining, item => Assert.StartsWith("Day2", item.Title));
        Assert.Equal(4, _viewModel.Articles.Count);
        Assert.All(_viewModel.Articles, a => Assert.StartsWith("Day2", a.Title));
    }

    /// <summary>
    /// Verifies that "mark all read" on an extended packet marks the whole
    /// theoretically visible packet — anchor day plus follow day.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkAllReadCommand_WithExtendedPacket_MarksWholePacket()
    {
        var feedId = await SeedFeedAsync();
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 5), 3, "Day1");
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 6), 4, "Day2");
        await _viewModel.LoadCommand.ExecuteAsync(null);
        Assert.Equal(7, _viewModel.Articles.Count);

        await _viewModel.MarkAllReadCommand.ExecuteAsync(null);

        Assert.Equal(0, await _itemRepository.GetUnreadCountAsync());
        Assert.Empty(_viewModel.Articles);
    }

    /// <summary>
    /// Verifies that switching the category chip resolves a new packet inside
    /// the selected category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SelectCategoryCommand_ResolvesNewPacket()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending);
        var categoryId = await SeedCategoryAsync("News");
        var feedA = await SeedFeedAsync("https://example.com/feed-a");
        var feedB = await SeedFeedAsync("https://example.com/feed-b", categoryId);
        await SeedUnreadAsync(feedA, new DateTime(2026, 1, 5), 6, "CatA");
        await SeedUnreadAsync(feedB, new DateTime(2026, 1, 6), 6, "CatB");
        await _viewModel.LoadCommand.ExecuteAsync(null);
        Assert.All(_viewModel.Articles, a => Assert.StartsWith("CatA", a.Title));

        var chip = _viewModel.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
        Assert.NotNull(chip);
        await _viewModel.SelectCategoryCommand.ExecuteAsync(chip);

        Assert.Equal(6, _viewModel.Articles.Count);
        Assert.All(_viewModel.Articles, a => Assert.StartsWith("CatB", a.Title));
    }

    /// <summary>
    /// Verifies that paging stays inside the packet: an anchor day larger than
    /// the page size is paged to its end and then stops (<c>HasMore</c> turns
    /// false); the follow day is not pulled in by scrolling.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadMoreCommand_PagesWithinPacket_StopsAtEnd()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending);
        var feedId = await SeedFeedAsync();
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 5), 25, "Day1");
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 6), 4, "Day2");
        await _viewModel.LoadCommand.ExecuteAsync(null);
        Assert.Equal(20, _viewModel.Articles.Count);
        Assert.True(_viewModel.HasMore);

        await _viewModel.LoadMoreCommand.ExecuteAsync(null);

        Assert.Equal(25, _viewModel.Articles.Count);
        Assert.False(_viewModel.HasMore);
        Assert.All(_viewModel.Articles, a => Assert.StartsWith("Day1", a.Title));
    }

    /// <summary>
    /// Verifies the edge case of a packet ending exactly on a full page:
    /// <c>HasMore</c> stays true until the next (empty) page load proves the
    /// packet end — the follow day is not pulled in.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadMoreCommand_PacketEndsOnFullPage_StopsAfterEmptyPage()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending);
        var feedId = await SeedFeedAsync();
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 7), 20, "Day3");
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 8), 4, "Day4");
        await _viewModel.LoadCommand.ExecuteAsync(null);
        Assert.Equal(20, _viewModel.Articles.Count);
        Assert.True(_viewModel.HasMore);

        await _viewModel.LoadMoreCommand.ExecuteAsync(null);

        Assert.Equal(20, _viewModel.Articles.Count);
        Assert.False(_viewModel.HasMore);
        Assert.All(_viewModel.Articles, a => Assert.StartsWith("Day3", a.Title));
    }

    /// <summary>
    /// Verifies that the card position indicator uses the packet total as its
    /// denominator — the card stack ends at the packet end, so a position beyond
    /// the packet must never be shown — and that it decrements synchronously
    /// when a single article is marked as read.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CardPositionText_UsesPacketTotalAsDenominator()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending);
        var feedId = await SeedFeedAsync();
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 5), 3, "Day1");
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 6), 4, "Day2");
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 7), 35, "Day3");
        await _viewModel.LoadCommand.ExecuteAsync(null);
        Assert.Equal(42, _viewModel.UnreadCount);
        Assert.Equal(7, _viewModel.Articles.Count);

        var format = AppResources.UnreadCardPositionFormat;
        Assert.Equal(string.Format(format, 1, 7), _viewModel.CardPositionText);

        await _viewModel.MarkReadCommand.ExecuteAsync(_viewModel.Articles[0]);

        Assert.Equal(string.Format(format, 1, 6), _viewModel.CardPositionText);
    }

    /// <summary>
    /// Verifies that the mark-all confirmation text reflects the packet scope:
    /// the total packet count — including articles beyond the loaded page —
    /// and the packet's day label.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkAllReadConfirmationText_ReflectsPacketScope()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending);
        var feedId = await SeedFeedAsync();
        var day1 = new DateTime(2026, 1, 5);
        await SeedUnreadAsync(feedId, day1, 25, "Day1");
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 6), 4, "Day2");
        await _viewModel.LoadCommand.ExecuteAsync(null);
        Assert.Equal(20, _viewModel.Articles.Count);

        var dayLabel = DateOnly.FromDateTime(day1).ToString("d", CultureInfo.CurrentCulture);
        var expected = string.Format(
            CultureInfo.CurrentCulture,
            AppResources.MarkAllReadPacketTitleFormat,
            25,
            dayLabel);
        Assert.Equal(expected, _viewModel.MarkAllReadConfirmationText);
    }

    /// <summary>
    /// Verifies that the confirmation text decrements synchronously with single
    /// mark-as-read actions and raises a property change notification, so the
    /// dialog never shows a stale packet count.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkReadCommand_DecrementsPacketConfirmationText()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending);
        var feedId = await SeedFeedAsync();
        var day1 = new DateTime(2026, 1, 5);
        var day2 = new DateTime(2026, 1, 6);
        await SeedUnreadAsync(feedId, day1, 3, "Day1");
        await SeedUnreadAsync(feedId, day2, 4, "Day2");
        await _viewModel.LoadCommand.ExecuteAsync(null);
        Assert.Equal(7, _viewModel.Articles.Count);

        var dayLabel = string.Join(
            " + ",
            new[] { day1, day2 }.Select(d => DateOnly.FromDateTime(d).ToString("d", CultureInfo.CurrentCulture)));
        var notified = 0;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(UnreadViewModel.MarkAllReadConfirmationText))
            {
                notified++;
            }
        };

        await _viewModel.MarkReadCommand.ExecuteAsync(_viewModel.Articles[0]);
        await _viewModel.MarkReadCommand.ExecuteAsync(_viewModel.Articles[0]);

        var expected = string.Format(
            CultureInfo.CurrentCulture,
            AppResources.MarkAllReadPacketTitleFormat,
            5,
            dayLabel);
        Assert.Equal(expected, _viewModel.MarkAllReadConfirmationText);
        Assert.True(notified >= 2, $"Expected at least 2 change notifications, got {notified}.");
    }

    /// <summary>
    /// Verifies the day-label composition inside the confirmation text for an
    /// extended packet: both short dates joined by " + ".
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkAllReadConfirmationText_JoinsExtendedDayLabels()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending);
        var feedId = await SeedFeedAsync();
        var day1 = new DateTime(2026, 1, 5);
        var day2 = new DateTime(2026, 1, 6);
        await SeedUnreadAsync(feedId, day1, 3, "Day1");
        await SeedUnreadAsync(feedId, day2, 4, "Day2");
        await _viewModel.LoadCommand.ExecuteAsync(null);

        var label1 = DateOnly.FromDateTime(day1).ToString("d", CultureInfo.CurrentCulture);
        var label2 = DateOnly.FromDateTime(day2).ToString("d", CultureInfo.CurrentCulture);
        Assert.Contains($"{label1} + {label2}", _viewModel.MarkAllReadConfirmationText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies the day-label composition inside the confirmation text for an
    /// undated-only packet: the no-date label is shown.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkAllReadConfirmationText_UsesNoDateLabel_ForUndatedOnlyPacket()
    {
        var feedId = await SeedFeedAsync();
        for (var i = 0; i < 6; i++)
        {
            await _itemRepository.AddAsync(new Item
            {
                Id = Guid.NewGuid(),
                FeedId = feedId,
                Title = $"Undated {i + 1}",
                GuidOrHash = $"undated-{i + 1}",
                IsRead = false,
                IsSavedForLater = false,
                PublishedAt = null,
            });
        }

        await _viewModel.LoadCommand.ExecuteAsync(null);

        var undatedExpected = string.Format(
            CultureInfo.CurrentCulture,
            AppResources.MarkAllReadPacketTitleFormat,
            6,
            AppResources.UnreadNoDateLabel);
        Assert.Equal(undatedExpected, _viewModel.MarkAllReadConfirmationText);
    }

    /// <summary>
    /// Verifies the day-label composition inside the confirmation text for an
    /// undated anchor with a dated follow day in ascending order: "no date +
    /// date".
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkAllReadConfirmationText_ComposesNoDatePlusDateLabel()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending);
        var feedId = await SeedFeedAsync();
        var day3 = new DateTime(2026, 1, 7);
        await _itemRepository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Undated X",
            GuidOrHash = "undated-x",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = null,
        });
        await SeedUnreadAsync(feedId, day3, 6, "Day3");
        await _viewModel.LoadCommand.ExecuteAsync(null);

        var label3 = DateOnly.FromDateTime(day3).ToString("d", CultureInfo.CurrentCulture);
        var mixedExpected = string.Format(
            CultureInfo.CurrentCulture,
            AppResources.MarkAllReadPacketTitleFormat,
            7,
            $"{AppResources.UnreadNoDateLabel} + {label3}");
        Assert.Equal(mixedExpected, _viewModel.MarkAllReadConfirmationText);
    }

    /// <summary>
    /// Verifies that a single-article packet uses the singular title format, so
    /// the English confirmation never reads "1 articles".
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkAllReadConfirmationText_UsesSingular_ForSingleArticlePacket()
    {
        var feedId = await SeedFeedAsync();
        await _itemRepository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Undated 1",
            GuidOrHash = "undated-1",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = null,
        });
        await _viewModel.LoadCommand.ExecuteAsync(null);

        var expected = string.Format(
            CultureInfo.CurrentCulture,
            AppResources.MarkAllReadPacketTitleSingularFormat,
            1,
            AppResources.UnreadNoDateLabel);
        Assert.Equal(expected, _viewModel.MarkAllReadConfirmationText);
    }

    /// <summary>
    /// Verifies that "mark all read" while a fresh load is in flight is not
    /// dropped: the reload queued behind the running load resolves the follow
    /// packet instead of leaving the freshly marked day on screen.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkAllReadCommand_DuringInFlightLoad_StillReloadsNextPacket()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending);
        var feedId = await SeedFeedAsync();
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 5), 6, "Day1");
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 6), 4, "Day2");

        var repository = new HookedItemRepository(_itemRepository);
        var viewModel = new UnreadViewModel(
            repository,
            _categoryRepository,
            new FakeFeedSyncService(),
            _settingsRepository,
            new FakeNetworkStatusService());
        await viewModel.LoadCommand.ExecuteAsync(null);
        Assert.Equal(6, viewModel.Articles.Count);

        // The hook fires while the second load is inside its paged query — i.e.
        // with IsLoading still true — and marks the resolved Day1 packet. The
        // in-flight load then returns empty (all marked), and the queued
        // reload must follow up with the Day2 packet.
        repository.Hook = () => viewModel.MarkAllReadCommand.ExecuteAsync(null);
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(4, viewModel.Articles.Count);
        Assert.All(viewModel.Articles, a => Assert.StartsWith("Day2", a.Title));
        Assert.False(viewModel.IsLoading);
    }

    /// <summary>
    /// Verifies that emptying the list via single mark-as-read actions while a
    /// fresh load is in flight still triggers the mandatory reload: the queued
    /// reload resolves and shows the next packet.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkReadCommand_EmptyingList_DuringInFlightLoad_StillReloadsNextPacket()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending);
        var feedId = await SeedFeedAsync();
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 5), 6, "Day1");
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 6), 4, "Day2");

        var repository = new HookedItemRepository(_itemRepository);
        var viewModel = new UnreadViewModel(
            repository,
            _categoryRepository,
            new FakeFeedSyncService(),
            _settingsRepository,
            new FakeNetworkStatusService());
        await viewModel.LoadCommand.ExecuteAsync(null);
        Assert.Equal(6, viewModel.Articles.Count);

        repository.Hook = async () =>
        {
            for (var i = 0; i < 6; i++)
            {
                await viewModel.MarkReadCommand.ExecuteAsync(viewModel.Articles[0]);
            }
        };
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(4, viewModel.Articles.Count);
        Assert.All(viewModel.Articles, a => Assert.StartsWith("Day2", a.Title));
        Assert.False(viewModel.IsLoading);
    }

    /// <summary>
    /// Verifies that a category tap while a fresh load is in flight is not
    /// lost: the selection is remembered and the queued reload applies the new
    /// category filter once the running load finishes.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SelectCategoryCommand_DuringInFlightLoad_AppliesSelection()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending);
        var categoryId = await SeedCategoryAsync("News");
        var feedA = await SeedFeedAsync("https://example.com/feed-a");
        var feedB = await SeedFeedAsync("https://example.com/feed-b", categoryId);
        await SeedUnreadAsync(feedA, new DateTime(2026, 1, 5), 6, "CatA");
        await SeedUnreadAsync(feedB, new DateTime(2026, 1, 6), 6, "CatB");

        var repository = new HookedItemRepository(_itemRepository);
        var viewModel = new UnreadViewModel(
            repository,
            _categoryRepository,
            new FakeFeedSyncService(),
            _settingsRepository,
            new FakeNetworkStatusService());
        await viewModel.LoadCommand.ExecuteAsync(null);
        Assert.All(viewModel.Articles, a => Assert.StartsWith("CatA", a.Title));

        var chip = viewModel.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
        Assert.NotNull(chip);
        repository.Hook = () => viewModel.SelectCategoryCommand.ExecuteAsync(chip);
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(categoryId, viewModel.SelectedCategory?.CategoryId);
        Assert.Equal(6, viewModel.Articles.Count);
        Assert.All(viewModel.Articles, a => Assert.StartsWith("CatB", a.Title));
        Assert.False(viewModel.IsLoading);
    }

    /// <summary>
    /// A <see cref="DelegatingItemRepository"/> that runs a callback inside the
    /// next paged unread query — while the calling load is still in flight —
    /// so tests can simulate a user action that arrives mid-load without
    /// relying on real concurrency.
    /// </summary>
    private sealed class HookedItemRepository : DelegatingItemRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HookedItemRepository"/> class.
        /// </summary>
        /// <param name="inner">The repository to delegate to.</param>
        public HookedItemRepository(IItemRepository inner)
            : base(inner)
        {
        }

        /// <summary>
        /// Gets or sets the callback invoked once inside the next paged unread query.
        /// </summary>
        public Func<Task>? Hook { get; set; }

        /// <inheritdoc />
        public override async Task<IReadOnlyList<ItemListItem>> GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null, bool ascending = false, UnreadPacket? packet = null)
        {
            var hook = Hook;
            if (hook is not null)
            {
                Hook = null;
                await hook();
            }

            return await base.GetUnreadByDateAsync(page, pageSize, categoryId, ascending, packet);
        }
    }
}
