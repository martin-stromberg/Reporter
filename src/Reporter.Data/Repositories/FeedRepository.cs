using Microsoft.EntityFrameworkCore;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using FeedEntity = Reporter.Data.Entities.Feed;

namespace Reporter.Data.Repositories;

/// <summary>
/// Provides data access for <see cref="Feed"/> domain models.
/// </summary>
public class FeedRepository : IFeedRepository
{
    private readonly IDbContextFactory<ReporterDbContext> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedRepository"/> class.
    /// </summary>
    /// <param name="factory">The database context factory.</param>
    public FeedRepository(IDbContextFactory<ReporterDbContext> factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Feed>> GetAllAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.Feeds
            .AsNoTracking()
            .OrderBy(f => f.Title)
            .ToListAsync();
        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<Feed?> GetByIdAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Feeds
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);
        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task AddAsync(Feed feed)
    {
        await using var context = await _factory.CreateDbContextAsync();
        context.Feeds.Add(MapToEntity(feed));
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Feed feed)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Feeds.FindAsync(feed.Id);
        if (entity is null)
        {
            return;
        }

        entity.Url = feed.Url;
        entity.Title = feed.Title;
        entity.CategoryId = feed.CategoryId;
        entity.LastCheckedAt = feed.LastCheckedAt;
        entity.HealthStatus = feed.HealthStatus;
        entity.HealthLastChange = feed.HealthLastChange;
        entity.NotificationsEnabled = feed.NotificationsEnabled;
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Feeds.FindAsync(id);
        if (entity is null)
        {
            return;
        }

        context.Feeds.Remove(entity);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FeedListItem>> GetAllWithDetailsAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var items = context.Items.AsNoTracking();

        var details = await context.Feeds
            .AsNoTracking()
            .OrderBy(f => f.Title)
            .Select(f => new FeedListItem
            {
                Id = f.Id,
                Title = f.Title,
                Url = f.Url,
                CategoryId = f.CategoryId,
                CategoryName = f.Category == null ? null : f.Category.Name,
                LastCheckedAt = f.LastCheckedAt,
                HealthStatus = f.HealthStatus,
                HealthLastChange = f.HealthLastChange,
                NotificationsEnabled = f.NotificationsEnabled,
                UnreadCount = items.Count(i => i.FeedId == f.Id && !i.IsRead),
            })
            .ToListAsync();

        return details;
    }

    /// <inheritdoc />
    public async Task<Feed?> GetByUrlAsync(string url)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Feeds
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Url == url);
        return entity is null ? null : MapToModel(entity);
    }

    private static Feed MapToModel(FeedEntity entity)
    {
        return new Feed
        {
            Id = entity.Id,
            Url = entity.Url,
            Title = entity.Title,
            CategoryId = entity.CategoryId,
            LastCheckedAt = entity.LastCheckedAt,
            HealthStatus = entity.HealthStatus,
            HealthLastChange = entity.HealthLastChange,
            NotificationsEnabled = entity.NotificationsEnabled,
        };
    }

    private static FeedEntity MapToEntity(Feed model)
    {
        return new FeedEntity
        {
            Id = model.Id,
            Url = model.Url,
            Title = model.Title,
            CategoryId = model.CategoryId,
            LastCheckedAt = model.LastCheckedAt,
            HealthStatus = model.HealthStatus,
            HealthLastChange = model.HealthLastChange,
            NotificationsEnabled = model.NotificationsEnabled,
        };
    }
}
