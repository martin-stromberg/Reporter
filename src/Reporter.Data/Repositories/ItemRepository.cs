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

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemRepository"/> class.
    /// </summary>
    /// <param name="factory">The database context factory.</param>
    public ItemRepository(IDbContextFactory<ReporterDbContext> factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Item>> GetAllAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.Items
            .AsNoTracking()
            .OrderByDescending(i => i.PublishedAt)
            .ToListAsync();
        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<Item?> GetByIdAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task AddAsync(Item item)
    {
        await using var context = await _factory.CreateDbContextAsync();
        context.Items.Add(MapToEntity(item));
        await context.SaveChangesAsync();
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
        entity.ContentHtml = item.ContentHtml;
        await context.SaveChangesAsync();
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
        return entities.Select(MapToModel).ToList();
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

        var entities = await SelectListItemRows(ordered
                .Skip(page * pageSize)
                .Take(pageSize))
            .ToListAsync();

        return entities.Select(MapToListItem).ToList();
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
        return entities.Select(MapToModel).ToList();
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
        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ItemListItem>> GetSavedForLaterAsync(int page, int pageSize)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await SelectListItemRows(context.Items
                .AsNoTracking()
                .Where(i => i.IsSavedForLater)
                .OrderByDescending(i => i.PublishedAt)
                .ThenBy(i => i.Id)
                .Skip(page * pageSize)
                .Take(pageSize))
            .ToListAsync();

        return entities.Select(MapToListItem).ToList();
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
    }

    /// <inheritdoc />
    public async Task<int> DeleteExpiredAsync(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        return await context.Items
            .Where(i => i.IsRead && !i.IsSavedForLater && (i.ReadAt ?? i.PublishedAt) < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Item>> GetExpiredKeywordCandidatesAsync(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var entities = await context.Items
            .AsNoTracking()
            .Where(i => i.IsRead && !i.IsSavedForLater && (i.PublishedAt ?? i.ReadAt) < cutoff)
            .ToListAsync(cancellationToken);
        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<int> DeleteRangeAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return 0;
        }

        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        return await context.Items
            .Where(i => ids.Contains(i.Id))
            .ExecuteDeleteAsync(cancellationToken);
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
            ContentHtml = i.ContentHtml,
            FeedTitle = i.Feed.Title,
            FeedFaviconUrl = i.Feed.FaviconUrl,
            CategoryId = i.Feed.CategoryId,
            CategoryName = i.Feed.Category != null ? i.Feed.Category.Name : null,
        });
    }

    private static ItemListItem MapToListItem(ItemListRow row)
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
            ImageUrl = ExtractImageUrl(row.ContentHtml),
            Summary = ExtractSummary(row.ContentHtml),
            ReadingTimeText = ReadingTimeEstimator.EstimateText(row.ContentHtml),
        };
    }

    private static string? ExtractImageUrl(string? contentHtml)
    {
        if (string.IsNullOrWhiteSpace(contentHtml))
        {
            return null;
        }

        var match = Regex.Match(contentHtml, "<img[^>]+src\\s*=\\s*['\"]([^'\"]+)['\"]", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
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

    private static Item MapToModel(ItemEntity entity)
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
            ContentHtml = entity.ContentHtml,
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
            ContentHtml = model.ContentHtml,
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

        public string? ContentHtml { get; set; }

        public string FeedTitle { get; set; } = string.Empty;

        public string? FeedFaviconUrl { get; set; }

        public Guid? CategoryId { get; set; }

        public string? CategoryName { get; set; }
    }
}
