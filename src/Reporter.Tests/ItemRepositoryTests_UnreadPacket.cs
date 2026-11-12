// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Data.Repositories;
using Entities = Reporter.Data.Entities;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the reading-packet members of the <see cref="ItemRepository"/> class:
/// <see cref="ItemRepository.ResolveUnreadPacketAsync"/>, the packet filter of
/// <see cref="ItemRepository.GetUnreadByDateAsync(int, int, Guid?, bool, UnreadPacket?)"/>,
/// <see cref="ItemRepository.GetUnreadPacketCountAsync"/> and
/// <see cref="ItemRepository.MarkUnreadPacketAsReadAsync"/>.
/// Seeds are expressed in local calendar days and converted to UTC the same way the
/// repository converts them back, so the tests are independent of the machine's time zone.
/// </summary>
public class ItemRepositoryTests_UnreadPacket : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly TestContentDbContextFactory _contentFactory;
    private readonly ItemContentRepository _contentStore;
    private readonly ItemRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemRepositoryTests_UnreadPacket"/> class.
    /// </summary>
    public ItemRepositoryTests_UnreadPacket()
    {
        _factory = new TestDbContextFactory();
        _contentFactory = new TestContentDbContextFactory();
        _contentStore = new ItemContentRepository(_contentFactory);
        _repository = new ItemRepository(_factory, _contentStore);
    }

    /// <summary>
    /// Disposes the test factories.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
        _contentFactory.Dispose();
    }

    // The repository interprets stored PublishedAt values as UTC and buckets them
    // into local calendar days; the seed helpers mirror that convention so the
    // expected days stay exact on any machine time zone.
    private static DateTime Utc(DateTime localTime)
        => DateTime.SpecifyKind(localTime, DateTimeKind.Local).ToUniversalTime();

    private static DateOnly LocalDayOf(DateTime utc)
        => DateOnly.FromDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().Date);

    private static DateTime DayStartUtc(DateOnly day)
        => DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue), DateTimeKind.Local).ToUniversalTime();

    private async Task SeedUnreadAsync(Guid feedId, DateTime localDay, int count, string prefix = "Article")
    {
        for (var i = 0; i < count; i++)
        {
            await _repository.AddAsync(new Item
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

    private async Task SeedUndatedAsync(Guid feedId, int count, string prefix = "Undated")
    {
        for (var i = 0; i < count; i++)
        {
            await _repository.AddAsync(new Item
            {
                Id = Guid.NewGuid(),
                FeedId = feedId,
                Title = $"{prefix} {i + 1}",
                GuidOrHash = $"{prefix}-{i + 1}",
                IsRead = false,
                IsSavedForLater = false,
                PublishedAt = null,
            });
        }
    }

    private async Task<Guid> SeedCategoryAsync(string name)
    {
        var categoryId = Guid.NewGuid();
        await using var context = _factory.CreateDbContext();
        context.Categories.Add(new Entities.Category { Id = categoryId, Name = name });
        await context.SaveChangesAsync();
        return categoryId;
    }

    private async Task<Guid> SeedFeedInCategoryAsync(Guid categoryId, string url)
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

    /// <summary>
    /// Verifies that <see cref="ItemRepository.ResolveUnreadPacketAsync"/> returns
    /// <see langword="null"/> when no unread items exist — both for an empty
    /// database and when only read items are stored.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveUnreadPacketAsync_ReturnsNull_WhenNoUnread()
    {
        Assert.Null(await _repository.ResolveUnreadPacketAsync(ascending: true));
        Assert.Null(await _repository.ResolveUnreadPacketAsync(ascending: false));

        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Read",
            GuidOrHash = "read",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = Utc(new DateTime(2026, 1, 5, 12, 0, 0)),
        });

        Assert.Null(await _repository.ResolveUnreadPacketAsync(ascending: true));
        Assert.Null(await _repository.ResolveUnreadPacketAsync(ascending: false));
    }

    /// <summary>
    /// Verifies that an anchor day with at least <see cref="ItemRepository.MinPacketSize"/>
    /// articles forms a single-day packet: the range bounds equal the local day's
    /// start and end and no follow day is added.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveUnreadPacketAsync_AnchorDayOnly_WhenDayHasEnough()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day1 = new DateTime(2026, 1, 5);
        var day2 = new DateTime(2026, 1, 6);
        await SeedUnreadAsync(feedId, day1, 6, "Day1");
        await SeedUnreadAsync(feedId, day2, 4, "Day2");

        var packet = await _repository.ResolveUnreadPacketAsync(ascending: true);

        Assert.NotNull(packet);
        var anchor = LocalDayOf(Utc(day1.Date.AddHours(12)));
        Assert.Equal(new DateOnly?[] { anchor }, packet.Days);
        Assert.False(packet.IncludeUndated);
        Assert.Equal(DayStartUtc(anchor), packet.FromUtc);
        Assert.Equal(DayStartUtc(anchor.AddDays(1)), packet.ToUtc);
    }

    /// <summary>
    /// Verifies that an anchor day with fewer articles than
    /// <see cref="ItemRepository.MinPacketSize"/> extends the packet to the next
    /// occupied day (asserted via the range end and <c>Days</c>).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveUnreadPacketAsync_ExtendsToNextDay_WhenBelowMin()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day1 = new DateTime(2026, 1, 5);
        var day2 = new DateTime(2026, 1, 6);
        await SeedUnreadAsync(feedId, day1, ItemRepository.MinPacketSize - 1, "Day1");
        await SeedUnreadAsync(feedId, day2, 3, "Day2");

        var packet = await _repository.ResolveUnreadPacketAsync(ascending: true);

        Assert.NotNull(packet);
        var anchor = LocalDayOf(Utc(day1.Date.AddHours(12)));
        var follow = LocalDayOf(Utc(day2.Date.AddHours(12)));
        Assert.Equal(new DateOnly?[] { anchor, follow }, packet.Days);
        Assert.Equal(DayStartUtc(anchor), packet.FromUtc);
        Assert.Equal(DayStartUtc(follow.AddDays(1)), packet.ToUtc);
    }

    /// <summary>
    /// Verifies the exact <see cref="ItemRepository.MinPacketSize"/> threshold:
    /// an anchor day with exactly that many articles does not extend the packet.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveUnreadPacketAsync_DoesNotExtend_WhenAtMin()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day3 = new DateTime(2026, 1, 7);
        var day4 = new DateTime(2026, 1, 8);
        await SeedUnreadAsync(feedId, day3, ItemRepository.MinPacketSize, "Day3");
        await SeedUnreadAsync(feedId, day4, 3, "Day4");

        var packetAtThreshold = await _repository.ResolveUnreadPacketAsync(ascending: true);

        Assert.NotNull(packetAtThreshold);
        var anchor3 = LocalDayOf(Utc(day3.Date.AddHours(12)));
        Assert.Equal(new DateOnly?[] { anchor3 }, packetAtThreshold.Days);
        Assert.Equal(DayStartUtc(anchor3.AddDays(1)), packetAtThreshold.ToUtc);
    }

    /// <summary>
    /// Verifies that the follow day is the next <em>occupied</em> calendar day in
    /// sort direction — empty calendar days in between are skipped.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveUnreadPacketAsync_NextDaySkipsEmptyCalendarDays()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day1 = new DateTime(2026, 1, 5);
        var day3 = new DateTime(2026, 1, 7);
        await SeedUnreadAsync(feedId, day1, 4, "Day1");
        await SeedUnreadAsync(feedId, day3, 3, "Day3");

        var packet = await _repository.ResolveUnreadPacketAsync(ascending: true);

        Assert.NotNull(packet);
        var anchor = LocalDayOf(Utc(day1.Date.AddHours(12)));
        var follow = LocalDayOf(Utc(day3.Date.AddHours(12)));
        Assert.Equal(new DateOnly?[] { anchor, follow }, packet.Days);
        Assert.Equal(DayStartUtc(anchor), packet.FromUtc);
        Assert.Equal(DayStartUtc(follow.AddDays(1)), packet.ToUtc);
    }

    /// <summary>
    /// Verifies that undated items form the pseudo day as first packet in ascending
    /// order: below the threshold the packet extends to the oldest dated day.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveUnreadPacketAsync_UndatedAnchorBelowMin_ExtendsToOldestDay()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day1 = new DateTime(2026, 1, 5);
        await SeedUndatedAsync(feedId, 2, "Undated A");
        await SeedUnreadAsync(feedId, day1, 6, "Day1");

        var packet = await _repository.ResolveUnreadPacketAsync(ascending: true);

        Assert.NotNull(packet);
        var follow = LocalDayOf(Utc(day1.Date.AddHours(12)));
        Assert.True(packet.IncludeUndated);
        Assert.Equal(new DateOnly?[] { null, follow }, packet.Days);
        Assert.Equal(DayStartUtc(follow), packet.FromUtc);
        Assert.Equal(DayStartUtc(follow.AddDays(1)), packet.ToUtc);
    }

    /// <summary>
    /// Verifies that enough undated items make the pseudo day a self-contained
    /// packet in ascending order — no dated day is pulled in and no range bounds
    /// are set.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveUnreadPacketAsync_UndatedAnchorAtMin_StaysUndatedOnly()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day2 = new DateTime(2026, 1, 6);
        await SeedUndatedAsync(feedId, ItemRepository.MinPacketSize + 1, "Undated B");
        await SeedUnreadAsync(feedId, day2, 3, "Day2");

        var undatedOnly = await _repository.ResolveUnreadPacketAsync(ascending: true);

        Assert.NotNull(undatedOnly);
        Assert.True(undatedOnly.IncludeUndated);
        Assert.Equal(new DateOnly?[] { null }, undatedOnly.Days);
        Assert.Null(undatedOnly.FromUtc);
        Assert.Null(undatedOnly.ToUtc);
    }

    /// <summary>
    /// Verifies that in descending order a below-threshold dated anchor without
    /// older dated days merges the undated pseudo day in as the follow day in
    /// sort direction (via <c>IncludeUndated</c>).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveUnreadPacketAsync_DescendingBelowMinAnchor_MergesUndatedPseudoDay()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day1 = new DateTime(2026, 1, 5);
        await SeedUnreadAsync(feedId, day1, 4, "Day1");
        await SeedUndatedAsync(feedId, 2, "Undated A");

        var merged = await _repository.ResolveUnreadPacketAsync(ascending: false);

        Assert.NotNull(merged);
        var anchor = LocalDayOf(Utc(day1.Date.AddHours(12)));
        Assert.True(merged.IncludeUndated);
        Assert.Equal(new DateOnly?[] { anchor, null }, merged.Days);
        Assert.Equal(DayStartUtc(anchor), merged.FromUtc);
        Assert.Equal(DayStartUtc(anchor.AddDays(1)), merged.ToUtc);
    }

    /// <summary>
    /// Verifies that in descending order a dated anchor at the threshold leaves
    /// the undated pseudo day untouched — undated items wait until all dated
    /// packets are worked off.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveUnreadPacketAsync_DescendingAnchorAtMin_LeavesUndatedPseudoDay()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day2 = new DateTime(2026, 1, 6);
        await SeedUnreadAsync(feedId, day2, 6, "Day2");
        await SeedUndatedAsync(feedId, 3, "Undated B");

        var datedOnly = await _repository.ResolveUnreadPacketAsync(ascending: false);

        Assert.NotNull(datedOnly);
        var anchor2 = LocalDayOf(Utc(day2.Date.AddHours(12)));
        Assert.False(datedOnly.IncludeUndated);
        Assert.Equal(new DateOnly?[] { anchor2 }, datedOnly.Days);
    }

    /// <summary>
    /// Verifies that in descending order the undated pseudo day becomes the last
    /// packet once all dated packets are worked off.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveUnreadPacketAsync_Descending_UndatedPseudoDayIsLastPacket()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day2 = new DateTime(2026, 1, 6);
        await SeedUnreadAsync(feedId, day2, 6, "Day2");
        await SeedUndatedAsync(feedId, 3, "Undated B");

        // Once the dated packet is read, the pseudo day becomes the last packet.
        var dated = await _repository.GetUnreadByDateAsync();
        foreach (var item in dated.Where(i => i.PublishedAt is not null))
        {
            await _repository.MarkAsReadAsync(item.Id);
        }

        var lastPacket = await _repository.ResolveUnreadPacketAsync(ascending: false);

        Assert.NotNull(lastPacket);
        Assert.True(lastPacket.IncludeUndated);
        Assert.Equal(new DateOnly?[] { null }, lastPacket.Days);
    }

    /// <summary>
    /// Verifies that the anchor search and the day counts only consider unread
    /// items of the selected category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveUnreadPacketAsync_RespectsCategoryFilter()
    {
        var categoryId = await SeedCategoryAsync("News");
        var feedInCategory = await SeedFeedInCategoryAsync(categoryId, "https://example.com/in-cat");
        var feedOther = await TestDataSeeder.SeedFeedAsync(_factory, "https://example.com/other");
        var day1 = new DateTime(2026, 1, 5);
        var day2 = new DateTime(2026, 1, 6);
        await SeedUnreadAsync(feedOther, day1, 6, "Other");
        await SeedUnreadAsync(feedInCategory, day2, 3, "InCat");

        var packet = await _repository.ResolveUnreadPacketAsync(categoryId, ascending: true);

        Assert.NotNull(packet);
        var anchor = LocalDayOf(Utc(day2.Date.AddHours(12)));
        Assert.Equal(new DateOnly?[] { anchor }, packet.Days);
        Assert.Equal(DayStartUtc(anchor), packet.FromUtc);
        Assert.Equal(DayStartUtc(anchor.AddDays(1)), packet.ToUtc);
    }

    /// <summary>
    /// Verifies that in descending order the anchor day is the newest occupied day.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveUnreadPacketAsync_Descending_AnchorsOnNewestDay()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day1 = new DateTime(2026, 1, 5);
        var day3 = new DateTime(2026, 1, 7);
        await SeedUnreadAsync(feedId, day1, 6, "Day1");
        await SeedUnreadAsync(feedId, day3, 6, "Day3");

        var packet = await _repository.ResolveUnreadPacketAsync(ascending: false);

        Assert.NotNull(packet);
        var newest = LocalDayOf(Utc(day3.Date.AddHours(12)));
        Assert.Equal(new DateOnly?[] { newest }, packet.Days);
        Assert.Equal(DayStartUtc(newest), packet.FromUtc);
        Assert.Equal(DayStartUtc(newest.AddDays(1)), packet.ToUtc);
    }

    /// <summary>
    /// Verifies that in ascending order the anchor day is the oldest occupied day.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveUnreadPacketAsync_Ascending_AnchorsOnOldestDay()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day1 = new DateTime(2026, 1, 5);
        var day3 = new DateTime(2026, 1, 7);
        await SeedUnreadAsync(feedId, day1, 6, "Day1");
        await SeedUnreadAsync(feedId, day3, 6, "Day3");

        var packet = await _repository.ResolveUnreadPacketAsync(ascending: true);

        Assert.NotNull(packet);
        var oldest = LocalDayOf(Utc(day1.Date.AddHours(12)));
        Assert.Equal(new DateOnly?[] { oldest }, packet.Days);
        Assert.Equal(DayStartUtc(oldest), packet.FromUtc);
        Assert.Equal(DayStartUtc(oldest.AddDays(1)), packet.ToUtc);
    }

    /// <summary>
    /// Verifies that the packet filter of the paged unread query applies the
    /// half-open UTC range exactly: an article at 23:59 of the anchor day is
    /// included, one at 00:00 of the next day is not, and undated articles only
    /// match <c>IncludeUndated</c> packets. Paging works inside the packet.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetUnreadByDateAsync_WithPacket_ReturnsOnlyPacketItems()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day1 = new DateTime(2026, 1, 5);
        var day1Start = Utc(day1.Date);
        var day1End = Utc(day1.Date.AddDays(1));

        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "DayStart",
            GuidOrHash = "day-start",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = day1Start.AddMinutes(1),
        });
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "DayEnd",
            GuidOrHash = "day-end",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = day1End.AddMinutes(-1),
        });
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "DayMid",
            GuidOrHash = "day-mid",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = Utc(day1.Date.AddHours(12)),
        });
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "NextDayStart",
            GuidOrHash = "next-day-start",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = day1End,
        });
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "PrevDayEnd",
            GuidOrHash = "prev-day-end",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = day1Start.AddMinutes(-1),
        });
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Undated",
            GuidOrHash = "undated",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = null,
        });

        var packet = new UnreadPacket
        {
            FromUtc = day1Start,
            ToUtc = day1End,
            IncludeUndated = false,
            Days = new DateOnly?[] { LocalDayOf(Utc(day1.Date.AddHours(12))) },
        };

        var firstPage = await _repository.GetUnreadByDateAsync(0, 2, packet: packet);
        var secondPage = await _repository.GetUnreadByDateAsync(1, 2, packet: packet);
        var thirdPage = await _repository.GetUnreadByDateAsync(2, 2, packet: packet);

        Assert.Equal(2, firstPage.Count);
        Assert.Single(secondPage);
        Assert.Empty(thirdPage);
        Assert.Equal(
            new[] { "DayEnd", "DayMid", "DayStart" },
            firstPage.Concat(secondPage).Select(i => i.Title).OrderBy(t => t, StringComparer.Ordinal));
    }

    /// <summary>
    /// Verifies that a <see langword="null"/> packet keeps the previous unbounded
    /// behavior of the paged unread query (regression guard).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetUnreadByDateAsync_WithoutPacket_Unchanged()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 5), 3, "Day1");
        await SeedUnreadAsync(feedId, new DateTime(2026, 1, 6), 3, "Day2");

        var result = await _repository.GetUnreadByDateAsync(0, 20, packet: null);

        Assert.Equal(6, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ItemRepository.MarkUnreadPacketAsReadAsync"/> marks
    /// only the items inside the packet range — articles of other days stay unread.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkUnreadPacketAsReadAsync_MarksOnlyPacketItems()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day1 = new DateTime(2026, 1, 5);
        var day2 = new DateTime(2026, 1, 6);
        await SeedUnreadAsync(feedId, day1, 3, "Day1");
        await SeedUnreadAsync(feedId, day2, 3, "Day2");

        var packet = new UnreadPacket
        {
            FromUtc = Utc(day1.Date),
            ToUtc = Utc(day1.Date.AddDays(1)),
            IncludeUndated = false,
            Days = new DateOnly?[] { LocalDayOf(Utc(day1.Date.AddHours(12))) },
        };

        await _repository.MarkUnreadPacketAsReadAsync(packet);

        var remaining = await _repository.GetUnreadByDateAsync();
        Assert.Equal(3, remaining.Count);
        Assert.All(remaining, item => Assert.StartsWith("Day2", item.Title));
    }

    /// <summary>
    /// Verifies that <see cref="ItemRepository.MarkUnreadPacketAsReadAsync"/> marks
    /// the entire packet range in the database — also articles beyond a loaded
    /// page (more than the page size of 20), not just the items shown in the list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkUnreadPacketAsReadAsync_MarksEntirePacket_BeyondLoadedPage()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day1 = new DateTime(2026, 1, 5);
        await SeedUnreadAsync(feedId, day1, 25, "Day1");

        var packet = await _repository.ResolveUnreadPacketAsync(ascending: true);
        Assert.NotNull(packet);

        await _repository.MarkUnreadPacketAsReadAsync(packet);

        Assert.Equal(0, await _repository.GetUnreadCountAsync());
    }

    /// <summary>
    /// Verifies that <see cref="ItemRepository.MarkUnreadPacketAsReadAsync"/>
    /// respects the category filter and the <c>IncludeUndated</c> part of the
    /// packet predicate: only matching unread items are marked.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkUnreadPacketAsReadAsync_RespectsCategoryAndUndated()
    {
        var categoryId = await SeedCategoryAsync("News");
        var feedInCategory = await SeedFeedInCategoryAsync(categoryId, "https://example.com/in-cat");
        var feedOther = await TestDataSeeder.SeedFeedAsync(_factory, "https://example.com/other");
        var day1 = new DateTime(2026, 1, 5);
        var day3 = new DateTime(2026, 1, 7);
        await SeedUnreadAsync(feedInCategory, day1, 2, "InCat Day1");
        await SeedUnreadAsync(feedInCategory, day3, 2, "InCat Day3");
        await SeedUndatedAsync(feedInCategory, 2, "InCat Undated");
        await SeedUndatedAsync(feedOther, 2, "Other Undated");

        var packet = new UnreadPacket
        {
            FromUtc = Utc(day1.Date),
            ToUtc = Utc(day1.Date.AddDays(1)),
            IncludeUndated = true,
            Days = new DateOnly?[] { LocalDayOf(Utc(day1.Date.AddHours(12))), null },
        };

        await _repository.MarkUnreadPacketAsReadAsync(packet, categoryId);

        var remaining = await _repository.GetUnreadByDateAsync();
        Assert.Equal(4, remaining.Count);
        Assert.Equal(2, remaining.Count(i => i.Title.StartsWith("InCat Day3", StringComparison.Ordinal)));
        Assert.Equal(2, remaining.Count(i => i.Title.StartsWith("Other Undated", StringComparison.Ordinal)));
    }

    /// <summary>
    /// Verifies that <see cref="ItemRepository.GetUnreadPacketCountAsync"/> counts
    /// the entire packet — including the follow day of an extended packet and
    /// articles not yet loaded into any page.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetUnreadPacketCountAsync_ReturnsPacketCount()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var day1 = new DateTime(2026, 1, 5);
        var day2 = new DateTime(2026, 1, 6);
        await SeedUnreadAsync(feedId, day1, 3, "Day1");
        await SeedUnreadAsync(feedId, day2, 4, "Day2");

        var packet = await _repository.ResolveUnreadPacketAsync(ascending: true);
        Assert.NotNull(packet);

        Assert.Equal(7, await _repository.GetUnreadPacketCountAsync(packet));

        var singleDayPacket = new UnreadPacket
        {
            FromUtc = packet.FromUtc,
            ToUtc = DayStartUtc(LocalDayOf(Utc(day1.Date.AddHours(12))).AddDays(1)),
            IncludeUndated = false,
            Days = new DateOnly?[] { LocalDayOf(Utc(day1.Date.AddHours(12))) },
        };
        Assert.Equal(3, await _repository.GetUnreadPacketCountAsync(singleDayPacket));
    }

    /// <summary>
    /// Verifies that a packet carrying only a lower range bound is rejected with
    /// an <see cref="ArgumentException"/> instead of silently degrading into a
    /// wrong predicate on the read, count and mark paths.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task PacketMethods_FromOnlyPacket_ThrowsArgumentException()
    {
        var halfOpenPacket = new UnreadPacket
        {
            FromUtc = Utc(new DateTime(2026, 1, 5)),
            ToUtc = null,
            IncludeUndated = false,
            Days = new DateOnly?[] { LocalDayOf(Utc(new DateTime(2026, 1, 5, 12, 0, 0))) },
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.GetUnreadByDateAsync(0, 20, packet: halfOpenPacket));
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.GetUnreadPacketCountAsync(halfOpenPacket));
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.MarkUnreadPacketAsReadAsync(halfOpenPacket));
    }

    /// <summary>
    /// Verifies that a packet carrying only an upper range bound is rejected with
    /// an <see cref="ArgumentException"/> — even with <c>IncludeUndated</c> set,
    /// which would otherwise silently match undated items only.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task PacketMethods_ToOnlyPacket_ThrowsArgumentException()
    {
        var halfOpenPacket = new UnreadPacket
        {
            FromUtc = null,
            ToUtc = Utc(new DateTime(2026, 1, 6)),
            IncludeUndated = true,
            Days = new DateOnly?[] { null },
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.GetUnreadByDateAsync(0, 20, packet: halfOpenPacket));
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.GetUnreadPacketCountAsync(halfOpenPacket));
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.MarkUnreadPacketAsReadAsync(halfOpenPacket));
    }
}
