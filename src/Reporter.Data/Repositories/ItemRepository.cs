using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
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
    public async Task<IReadOnlyList<ItemListItem>> GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null)
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

        var entities = await query
            .OrderByDescending(i => i.PublishedAt)
            .ThenBy(i => i.Id)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(i => new
            {
                i.Id,
                i.FeedId,
                i.Title,
                i.Link,
                i.PublishedAt,
                i.IsRead,
                i.IsSavedForLater,
                i.ContentHtml,
                FeedTitle = i.Feed.Title,
                CategoryId = i.Feed.CategoryId,
                CategoryName = i.Feed.Category != null ? i.Feed.Category.Name : null,
            })
            .ToListAsync();

        return entities.Select(e => new ItemListItem
        {
            Id = e.Id,
            FeedId = e.FeedId,
            Title = e.Title,
            Link = e.Link,
            PublishedAt = e.PublishedAt,
            IsRead = e.IsRead,
            IsSavedForLater = e.IsSavedForLater,
            FeedTitle = e.FeedTitle,
            CategoryId = e.CategoryId,
            CategoryName = e.CategoryName,
            ImageUrl = ExtractImageUrl(e.ContentHtml),
            Summary = ExtractSummary(e.ContentHtml),
        }).ToList();
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
    public async Task<IReadOnlyList<Item>> GetSavedForLaterAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.Items
            .AsNoTracking()
            .Where(i => i.IsSavedForLater)
            .OrderByDescending(i => i.PublishedAt)
            .ToListAsync();
        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<Item?> GetByGuidOrHashAsync(Guid feedId, string guidOrHash)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.FeedId == feedId && i.GuidOrHash == guidOrHash);
        return entity is null ? null : MapToModel(entity);
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
}
