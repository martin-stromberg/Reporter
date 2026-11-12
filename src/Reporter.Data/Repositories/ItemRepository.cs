// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Services;
using ItemEntity = Reporter.Data.Entities.Item;

namespace Reporter.Data.Repositories;

/// <summary>
/// Provides data access for <see cref="Item"/> domain models.
/// </summary>
public class ItemRepository : IItemRepository
{
    /// <summary>
    /// The minimum number of unread articles on the anchor day; below this
    /// threshold the packet expands to the next occupied calendar day.
    /// </summary>
    public const int MinPacketSize = 5;

    private readonly IDbContextFactory<ReporterDbContext> _factory;
    private readonly IItemContentStore _contentStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemRepository"/> class.
    /// </summary>
    /// <param name="factory">The database context factory.</param>
    /// <param name="contentStore">The store holding the item contents.</param>
    public ItemRepository(IDbContextFactory<ReporterDbContext> factory, IItemContentStore contentStore)
    {
        _factory = factory;
        _contentStore = contentStore;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Item>> GetAllAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.Items
            .AsNoTracking()
            .OrderByDescending(i => i.PublishedAt)
            .ToListAsync();
        var contents = await GetContentsAsync(entities, e => e.Id);
        return entities.Select(e => MapToModel(e, contents.GetValueOrDefault(e.Id))).ToList();
    }

    /// <inheritdoc />
    public async Task<Item?> GetByIdAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
        if (entity is null)
        {
            return null;
        }

        var contentHtml = await _contentStore.GetAsync(id);
        var image = await _contentStore.GetImageAsync(id);
        return MapToModel(entity, contentHtml, image);
    }

    /// <inheritdoc />
    public async Task AddAsync(Item item)
    {
        await using var context = await _factory.CreateDbContextAsync();
        context.Items.Add(MapToEntity(item));
        await context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(item.ContentHtml) || item.Image is not null)
        {
            await _contentStore.SetAsync(item.Id, item.ContentHtml, item.Image);
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Item item)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Items.FindAsync(item.Id);
        if (entity is null)
        {
            return;
        }

        entity.FeedId = item.FeedId;
        entity.Title = item.Title;
        entity.Link = item.Link;
        entity.PublishedAt = item.PublishedAt;
        entity.GuidOrHash = item.GuidOrHash;
        entity.IsRead = item.IsRead;
        entity.IsSavedForLater = item.IsSavedForLater;
        entity.ReadAt = item.ReadAt;
        entity.Tags = SerializeTags(item.Tags);
        await context.SaveChangesAsync();

        await _contentStore.SetAsync(item.Id, item.ContentHtml);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Items.FindAsync(id);
        if (entity is null)
        {
            return;
        }

        context.Items.Remove(entity);
        await context.SaveChangesAsync();
        await _contentStore.DeleteAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Item>> GetUnreadByDateAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.Items
            .AsNoTracking()
            .Where(i => !i.IsRead)
            .OrderByDescending(i => i.PublishedAt)
            .ToListAsync();
        var contents = await GetContentsAsync(entities, e => e.Id);
        return entities.Select(e => MapToModel(e, contents.GetValueOrDefault(e.Id))).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ItemListItem>> GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null, bool ascending = false, UnreadPacket? packet = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var query = context.Items
            .AsNoTracking()
            .Where(i => !i.IsRead);

        if (categoryId.HasValue)
        {
            var feedIds = context.Feeds
                .AsNoTracking()
                .Where(f => f.CategoryId == categoryId.Value)
                .Select(f => f.Id);
            query = query.Where(i => feedIds.Contains(i.FeedId));
        }

        if (packet is not null)
        {
            query = query.Where(PacketMatches(packet));
        }

        var ordered = ascending
            ? query.OrderBy(i => i.PublishedAt).ThenByDescending(i => i.Id)
            : query.OrderByDescending(i => i.PublishedAt).ThenBy(i => i.Id);

        var rows = await SelectListItemRows(ordered
                .Skip(page * pageSize)
                .Take(pageSize))
            .ToListAsync();

        var contents = await GetContentsAsync(rows, r => r.Id);
        var imageItemIds = await _contentStore.GetImageIdsAsync(rows.Select(r => r.Id).ToList());
        return rows.Select(r => MapToListItem(r, contents.GetValueOrDefault(r.Id), imageItemIds.Contains(r.Id))).ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetSavedCountAsync(Guid? categoryId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await SavedItemsQuery(context, categoryId).CountAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(Guid? categoryId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var query = context.Items
            .AsNoTracking()
            .Where(i => !i.IsRead);

        if (categoryId.HasValue)
        {
            var feedIds = context.Feeds
                .AsNoTracking()
                .Where(f => f.CategoryId == categoryId.Value)
                .Select(f => f.Id);
            query = query.Where(i => feedIds.Contains(i.FeedId));
        }

        return await query.CountAsync();
    }

    /// <inheritdoc />
    public async Task MarkAllAsReadAsync(Guid? categoryId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var query = context.Items.Where(i => !i.IsRead);

        if (categoryId.HasValue)
        {
            var feedIds = context.Feeds
                .Where(f => f.CategoryId == categoryId.Value)
                .Select(f => f.Id);
            query = query.Where(i => feedIds.Contains(i.FeedId));
        }

        await query.ExecuteUpdateAsync(setters => setters
            .SetProperty(i => i.IsRead, true)
            .SetProperty(i => i.ReadAt, DateTime.UtcNow));
    }

    /// <inheritdoc />
    public async Task<UnreadPacket?> ResolveUnreadPacketAsync(Guid? categoryId = null, bool ascending = false)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var query = context.Items
            .AsNoTracking()
            .Where(i => !i.IsRead);

        if (categoryId.HasValue)
        {
            var feedIds = context.Feeds
                .AsNoTracking()
                .Where(f => f.CategoryId == categoryId.Value)
                .Select(f => f.Id);
            query = query.Where(i => feedIds.Contains(i.FeedId));
        }

        if (!await query.AnyAsync())
        {
            return null;
        }

        // The anchor is the first element in the active sort order — the same
        // ordering the paged list query uses, so the packet always covers what
        // the list shows first.
        var anchorPublishedAt = ascending
            ? await query
                .OrderBy(i => i.PublishedAt)
                .ThenByDescending(i => i.Id)
                .Select(i => i.PublishedAt)
                .FirstAsync()
            : await query
                .OrderByDescending(i => i.PublishedAt)
                .ThenBy(i => i.Id)
                .Select(i => i.PublishedAt)
                .FirstAsync();

        if (anchorPublishedAt is null)
        {
            return await ResolveUndatedAnchorPacketAsync(query, ascending);
        }

        return await ResolveDatedAnchorPacketAsync(query, anchorPublishedAt.Value, ascending);
    }

    // Undated anchor: the pseudo day sorts first in ascending and last in
    // descending order — a descending undated anchor therefore means no dated
    // unread items exist at all.
    private async Task<UnreadPacket> ResolveUndatedAnchorPacketAsync(IQueryable<ItemEntity> query, bool ascending)
    {
        var days = new List<DateOnly?> { null };
        DateTime? fromUtc = null;
        DateTime? toUtc = null;

        var undatedCount = await query.CountAsync(i => i.PublishedAt == null);
        if (ascending && undatedCount < MinPacketSize)
        {
            var followPublishedAt = await query
                .Where(i => i.PublishedAt != null)
                .OrderBy(i => i.PublishedAt)
                .ThenByDescending(i => i.Id)
                .Select(i => i.PublishedAt)
                .FirstOrDefaultAsync();
            if (followPublishedAt.HasValue)
            {
                var followDay = LocalDayOf(followPublishedAt.Value);
                days.Add(followDay);
                (fromUtc, toUtc) = DayBoundsUtc(followDay);
            }
        }

        return new UnreadPacket
        {
            FromUtc = fromUtc,
            ToUtc = toUtc,
            IncludeUndated = true,
            Days = days,
        };
    }

    // Dated anchor: the anchor day extends to the next occupied day in sort
    // direction when it holds fewer than MinPacketSize unread articles — in
    // descending order the undated pseudo day counts as that follow day.
    private async Task<UnreadPacket> ResolveDatedAnchorPacketAsync(IQueryable<ItemEntity> query, DateTime anchorPublishedAt, bool ascending)
    {
        var anchorDay = LocalDayOf(anchorPublishedAt);
        var (fromUtc, toUtc) = DayBoundsUtc(anchorDay);
        var days = new List<DateOnly?> { anchorDay };
        var includeUndated = false;

        var anchorCount = await query.CountAsync(i => i.PublishedAt >= fromUtc && i.PublishedAt < toUtc);
        if (anchorCount < MinPacketSize)
        {
            DateTime? followPublishedAt;
            if (ascending)
            {
                followPublishedAt = await query
                    .Where(i => i.PublishedAt >= toUtc)
                    .OrderBy(i => i.PublishedAt)
                    .ThenByDescending(i => i.Id)
                    .Select(i => i.PublishedAt)
                    .FirstOrDefaultAsync();
                if (followPublishedAt.HasValue)
                {
                    var followDay = LocalDayOf(followPublishedAt.Value);
                    days.Add(followDay);
                    toUtc = DayBoundsUtc(followDay).ToUtc;
                }
            }
            else
            {
                followPublishedAt = await query
                    .Where(i => i.PublishedAt < fromUtc)
                    .OrderByDescending(i => i.PublishedAt)
                    .ThenBy(i => i.Id)
                    .Select(i => i.PublishedAt)
                    .FirstOrDefaultAsync();
                if (followPublishedAt.HasValue)
                {
                    var followDay = LocalDayOf(followPublishedAt.Value);
                    days.Add(followDay);
                    fromUtc = DayBoundsUtc(followDay).FromUtc;
                }
                else if (await query.AnyAsync(i => i.PublishedAt == null))
                {
                    days.Add(null);
                    includeUndated = true;
                }
            }
        }

        return new UnreadPacket
        {
            FromUtc = fromUtc,
            ToUtc = toUtc,
            IncludeUndated = includeUndated,
            Days = days,
        };
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadPacketCountAsync(UnreadPacket packet, Guid? categoryId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var query = context.Items
            .AsNoTracking()
            .Where(i => !i.IsRead)
            .Where(PacketMatches(packet));

        if (categoryId.HasValue)
        {
            var feedIds = context.Feeds
                .AsNoTracking()
                .Where(f => f.CategoryId == categoryId.Value)
                .Select(f => f.Id);
            query = query.Where(i => feedIds.Contains(i.FeedId));
        }

        return await query.CountAsync();
    }

    /// <inheritdoc />
    public async Task MarkUnreadPacketAsReadAsync(UnreadPacket packet, Guid? categoryId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var query = context.Items
            .Where(i => !i.IsRead)
            .Where(PacketMatches(packet));

        if (categoryId.HasValue)
        {
            var feedIds = context.Feeds
                .Where(f => f.CategoryId == categoryId.Value)
                .Select(f => f.Id);
            query = query.Where(i => feedIds.Contains(i.FeedId));
        }

        await query.ExecuteUpdateAsync(setters => setters
            .SetProperty(i => i.IsRead, true)
            .SetProperty(i => i.ReadAt, DateTime.UtcNow));
    }

    /// <inheritdoc />
    public async Task ToggleSavedForLaterAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Items.FindAsync(id);
        if (entity is null)
        {
            return;
        }

        entity.IsSavedForLater = !entity.IsSavedForLater;
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task MarkAsReadAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Items.FindAsync(id);
        if (entity is null)
        {
            return;
        }

        entity.IsRead = true;
        entity.ReadAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Item>> GetByFeedAsync(Guid feedId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.Items
            .AsNoTracking()
            .Where(i => i.FeedId == feedId)
            .OrderByDescending(i => i.PublishedAt)
            .ToListAsync();
        var contents = await GetContentsAsync(entities, e => e.Id);
        return entities.Select(e => MapToModel(e, contents.GetValueOrDefault(e.Id))).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ItemListItem>> GetByFeedAsync(
        Guid feedId,
        int page,
        int pageSize,
        string? searchTerm = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var query = context.Items
            .AsNoTracking()
            .Where(i => i.FeedId == feedId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(i => i.Title.Contains(term) || (i.Tags != null && i.Tags.Contains(term)));
        }

        var rows = await SelectListItemRows(query
                .OrderByDescending(i => i.PublishedAt)
                .ThenBy(i => i.Id)
                .Skip(page * pageSize)
                .Take(pageSize))
            .ToListAsync();

        var contents = await GetContentsAsync(rows, r => r.Id);
        var imageItemIds = await _contentStore.GetImageIdsAsync(rows.Select(r => r.Id).ToList());
        return rows.Select(r => MapToListItem(r, contents.GetValueOrDefault(r.Id), imageItemIds.Contains(r.Id))).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Item>> GetByCategoryAsync(Guid categoryId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var feedIds = context.Feeds
            .Where(f => f.CategoryId == categoryId)
            .Select(f => f.Id);
        var entities = await context.Items
            .AsNoTracking()
            .Where(i => feedIds.Contains(i.FeedId))
            .OrderByDescending(i => i.PublishedAt)
            .ToListAsync();
        var contents = await GetContentsAsync(entities, e => e.Id);
        return entities.Select(e => MapToModel(e, contents.GetValueOrDefault(e.Id))).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ItemListItem>> GetSavedForLaterAsync(int page, int pageSize, Guid? categoryId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var rows = await SelectListItemRows(SavedItemsQuery(context, categoryId)
                .OrderByDescending(i => i.PublishedAt)
                .ThenBy(i => i.Id)
                .Skip(page * pageSize)
                .Take(pageSize))
            .ToListAsync();

        var contents = await GetContentsAsync(rows, r => r.Id);
        var imageItemIds = await _contentStore.GetImageIdsAsync(rows.Select(r => r.Id).ToList());
        return rows.Select(r => MapToListItem(r, contents.GetValueOrDefault(r.Id), imageItemIds.Contains(r.Id))).ToList();
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IReadOnlyList<Item> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        await using var context = await _factory.CreateDbContextAsync();
        context.Items.AddRange(items.Select(MapToEntity));
        await context.SaveChangesAsync();

        var contentEntries = items
            .Where(i => !string.IsNullOrEmpty(i.ContentHtml) || i.Image is not null)
            .Select(i => new ItemContentEntry(i.Id, i.ContentHtml, i.Image))
            .ToList();
        await _contentStore.SetRangeAsync(contentEntries);
    }

    /// <inheritdoc />
    public async Task UpdateTagsAsync(IReadOnlyDictionary<Guid, IReadOnlyList<string>> tagsByItemId)
    {
        if (tagsByItemId.Count == 0)
        {
            return;
        }

        await using var context = await _factory.CreateDbContextAsync();
        foreach (var (itemId, tags) in tagsByItemId)
        {
            var serialized = SerializeTags(tags);
            await context.Items
                .Where(i => i.Id == itemId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.Tags, serialized));
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteExpiredAsync(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var ids = await context.Items
            .Where(i => i.IsRead && !i.IsSavedForLater && (i.ReadAt ?? i.PublishedAt) < cutoff)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);
        if (ids.Count == 0)
        {
            return 0;
        }

        var deleted = await context.Items
            .Where(i => ids.Contains(i.Id))
            .ExecuteDeleteAsync(cancellationToken);
        await _contentStore.DeleteRangeAsync(ids, cancellationToken);
        return deleted;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Item>> GetExpiredKeywordCandidatesAsync(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var entities = await context.Items
            .AsNoTracking()
            .Where(i => i.IsRead && !i.IsSavedForLater && (i.PublishedAt ?? i.ReadAt) < cutoff)
            .ToListAsync(cancellationToken);
        var contents = await GetContentsAsync(entities, e => e.Id, cancellationToken);
        return entities.Select(e => MapToModel(e, contents.GetValueOrDefault(e.Id))).ToList();
    }

    /// <inheritdoc />
    public async Task<int> DeleteRangeAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return 0;
        }

        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var deleted = await context.Items
            .Where(i => ids.Contains(i.Id))
            .ExecuteDeleteAsync(cancellationToken);
        await _contentStore.DeleteRangeAsync(ids, cancellationToken);
        return deleted;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetAllIdsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        return await context.Items
            .AsNoTracking()
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);
    }

    // Shared packet predicate for the read, count and mark paths so "mark all
    // read" covers exactly the range the packet makes theoretically visible:
    // the half-open UTC window, or the undated pseudo day, or both. A packet
    // without range bounds covers undated items only; a half-open packet is
    // invalid and is rejected instead of silently degrading into a wrong
    // predicate for the destructive mark path.
    private static System.Linq.Expressions.Expression<Func<ItemEntity, bool>> PacketMatches(UnreadPacket packet)
    {
        var from = packet.FromUtc;
        var to = packet.ToUtc;
        if (from.HasValue != to.HasValue)
        {
            throw new ArgumentException(
                $"An unread packet must set either both range bounds or none ({nameof(UnreadPacket.FromUtc)}/{nameof(UnreadPacket.ToUtc)}).",
                nameof(packet));
        }

        if (from.HasValue && to.HasValue)
        {
            return packet.IncludeUndated
                ? i => (i.PublishedAt >= from && i.PublishedAt < to) || i.PublishedAt == null
                : i => i.PublishedAt >= from && i.PublishedAt < to;
        }

        return packet.IncludeUndated
            ? i => i.PublishedAt == null
            : i => false;
    }

    // Local calendar day of a UTC timestamp: stored PublishedAt values are UTC,
    // while the packet days are defined by the device's local time.
    private static DateOnly LocalDayOf(DateTime utc)
        => DateOnly.FromDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().Date);

    // UTC bounds of a local calendar day: [day start, next day start).
    private static (DateTime FromUtc, DateTime ToUtc) DayBoundsUtc(DateOnly day)
    {
        var from = DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue), DateTimeKind.Local).ToUniversalTime();
        var to = DateTime.SpecifyKind(day.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Local).ToUniversalTime();
        return (from, to);
    }

    /// <summary>
    /// Projects item entities including feed and category data into flat list rows.
    /// Shared by the unread and saved-for-later paged queries.
    /// </summary>
    /// <param name="query">The item query to project.</param>
    /// <returns>A queryable of flat list rows.</returns>
    private static IQueryable<ItemListRow> SelectListItemRows(IQueryable<ItemEntity> query)
    {
        return query.Select(i => new ItemListRow
        {
            Id = i.Id,
            FeedId = i.FeedId,
            Title = i.Title,
            Link = i.Link,
            PublishedAt = i.PublishedAt,
            IsRead = i.IsRead,
            IsSavedForLater = i.IsSavedForLater,
            FeedTitle = i.Feed.Title,
            FeedFaviconUrl = i.Feed.FaviconUrl,
            CategoryId = i.Feed.CategoryId,
            CategoryName = i.Feed.Category != null ? i.Feed.Category.Name : null,
        });
    }

    private static IQueryable<ItemEntity> SavedItemsQuery(ReporterDbContext context, Guid? categoryId)
    {
        var query = context.Items
            .AsNoTracking()
            .Where(i => i.IsSavedForLater);

        return categoryId.HasValue
            ? query.Where(i => i.Feed.CategoryId == categoryId.Value)
            : query;
    }

    private ItemListItem MapToListItem(ItemListRow row, string? contentHtml, bool hasLocalImage)
    {
        return new ItemListItem
        {
            Id = row.Id,
            FeedId = row.FeedId,
            Title = row.Title.Trim(),
            Link = row.Link,
            PublishedAt = row.PublishedAt,
            IsRead = row.IsRead,
            IsSavedForLater = row.IsSavedForLater,
            FeedTitle = row.FeedTitle,
            FeedFaviconUrl = row.FeedFaviconUrl,
            CategoryId = row.CategoryId,
            CategoryName = row.CategoryName,
            ImageUrl = ExtractImageUrl(contentHtml),
            LocalImageLoader = hasLocalImage ? ct => LoadImageStreamAsync(row.Id, ct) : null,
            Summary = ExtractSummary(contentHtml),
            ReadingTimeText = ReadingTimeEstimator.EstimateText(contentHtml),
        };
    }

    // The image blob is only read when the card actually renders the
    // thumbnail, so paged list projections never pull the full image data.
    private async Task<Stream> LoadImageStreamAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var image = await _contentStore.GetImageAsync(itemId, cancellationToken).ConfigureAwait(false);
        return image is null ? Stream.Null : new MemoryStream(image.Data, writable: false);
    }

    private static string? ExtractImageUrl(string? contentHtml)
    {
        return ItemImageService.ExtractFirstImageUrl(contentHtml);
    }

    private static string? ExtractSummary(string? contentHtml)
    {
        if (string.IsNullOrWhiteSpace(contentHtml))
        {
            return null;
        }

        var plain = Regex.Replace(contentHtml, "<.*?>", string.Empty);
        plain = WebUtility.HtmlDecode(plain);
        plain = plain.Replace('\n', ' ').Replace('\r', ' ').Trim();

        if (string.IsNullOrWhiteSpace(plain))
        {
            return null;
        }

        const int MaxLength = 120;
        return plain.Length <= MaxLength ? plain : plain[..MaxLength] + "…";
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetContentsAsync<T>(IReadOnlyList<T> items, Func<T, Guid> idSelector, CancellationToken cancellationToken = default)
    {
        return await _contentStore.GetRangeAsync(items.Select(idSelector).ToList(), cancellationToken);
    }

    private static Item MapToModel(ItemEntity entity, string? contentHtml, ItemImage? image = null)
    {
        return new Item
        {
            Id = entity.Id,
            FeedId = entity.FeedId,
            Title = entity.Title,
            Link = entity.Link,
            PublishedAt = entity.PublishedAt,
            GuidOrHash = entity.GuidOrHash,
            IsRead = entity.IsRead,
            IsSavedForLater = entity.IsSavedForLater,
            ReadAt = entity.ReadAt,
            ContentHtml = contentHtml,
            Image = image,
            Tags = DeserializeTags(entity.Tags),
        };
    }

    private static ItemEntity MapToEntity(Item model)
    {
        return new ItemEntity
        {
            Id = model.Id,
            FeedId = model.FeedId,
            Title = model.Title,
            Link = model.Link,
            PublishedAt = model.PublishedAt,
            GuidOrHash = model.GuidOrHash,
            IsRead = model.IsRead,
            IsSavedForLater = model.IsSavedForLater,
            ReadAt = model.ReadAt,
            Tags = SerializeTags(model.Tags),
        };
    }

    // The tag list is stored in a single text column with the ASCII unit
    // separator (U+001F) between terms: a comma delimiter would not round-trip
    // losslessly because a feed category name may itself contain a comma, and
    // U+001F is not a valid XML 1.0 character, so no feed-derived tag can ever
    // contain it — the mapping stays injective without any escaping. The
    // delimiter cannot be typed into the search box either, so the
    // Contains-based substring search keeps its semantics. An empty list maps
    // to null so tag-less rows stay distinguishable and compact.
    private const char TagSeparator = '\x1F';

    private static string? SerializeTags(IReadOnlyList<string> tags)
    {
        return tags.Count == 0 ? null : string.Join(TagSeparator, tags);
    }

    private static IReadOnlyList<string> DeserializeTags(string? tags)
    {
        return string.IsNullOrWhiteSpace(tags)
            ? []
            : tags.Split(TagSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Flat database row used by the paged <see cref="ItemListItem"/> projections.
    /// </summary>
    private sealed class ItemListRow
    {
        public Guid Id { get; set; }

        public Guid FeedId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Link { get; set; }

        public DateTime? PublishedAt { get; set; }

        public bool IsRead { get; set; }

        public bool IsSavedForLater { get; set; }

        public string FeedTitle { get; set; } = string.Empty;

        public string? FeedFaviconUrl { get; set; }

        public Guid? CategoryId { get; set; }

        public string? CategoryName { get; set; }
    }
}
