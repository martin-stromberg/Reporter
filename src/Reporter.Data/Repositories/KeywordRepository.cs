// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using KeywordEntity = Reporter.Data.Entities.Keyword;

namespace Reporter.Data.Repositories;

/// <summary>
/// Provides data access for <see cref="Keyword"/> domain models.
/// </summary>
public class KeywordRepository : IKeywordRepository
{
    private readonly IDbContextFactory<ReporterDbContext> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeywordRepository"/> class.
    /// </summary>
    /// <param name="factory">The database context factory.</param>
    public KeywordRepository(IDbContextFactory<ReporterDbContext> factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Keyword>> GetAllAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.Keywords
            .AsNoTracking()
            .OrderBy(k => k.KeywordText)
            .ToListAsync();
        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Keyword>> GetByFeedAsync(Guid? feedId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.Keywords
            .AsNoTracking()
            .Where(k => k.FeedId == feedId)
            .OrderBy(k => k.KeywordText)
            .ToListAsync();
        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Keyword>> GetEffectiveForFeedAsync(Guid feedId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.Keywords
            .AsNoTracking()
            .Where(k => k.FeedId == null || k.FeedId == feedId)
            .OrderBy(k => k.KeywordText)
            .ToListAsync();
        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Keywords.AnyAsync();
    }

    /// <inheritdoc />
    public async Task<Keyword?> GetByIdAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Keywords
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == id);
        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task AddAsync(Keyword keyword)
    {
        await using var context = await _factory.CreateDbContextAsync();
        context.Keywords.Add(MapToEntity(keyword));
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Keyword keyword)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Keywords.FindAsync(keyword.Id);
        if (entity is null)
        {
            return;
        }

        entity.KeywordText = keyword.KeywordText;
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Keywords.FindAsync(id);
        if (entity is null)
        {
            return;
        }

        context.Keywords.Remove(entity);
        await context.SaveChangesAsync();
    }

    private static Keyword MapToModel(KeywordEntity entity)
    {
        return new Keyword
        {
            Id = entity.Id,
            KeywordText = entity.KeywordText,
            FeedId = entity.FeedId,
        };
    }

    private static KeywordEntity MapToEntity(Keyword model)
    {
        return new KeywordEntity
        {
            Id = model.Id,
            KeywordText = model.KeywordText,
            FeedId = model.FeedId,
        };
    }
}
