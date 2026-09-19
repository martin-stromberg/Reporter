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
    public async Task<IReadOnlyList<ItemListItem>> GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null, bool ascending = false)
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
    public async Task<IReadOnlyList<ItemListItem>> GetSavedForLaterAsync(int page, int pageSize)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var rows = await SelectListItemRows(context.Items
                .AsNoTracking()
                .Where(i => i.IsSavedForLater)
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

    private ItemListItem MapToListItem(ItemListRow row, string? contentHtml, bool hasLocalImage)
    {
        return new ItemListItem
        {
            Id = row.Id,
            FeedId = row.FeedId,
            Title = row.Title,
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
        };
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
