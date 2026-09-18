// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Data.Entities;

namespace Reporter.Data.Repositories;

/// <summary>
/// Provides data access to the item content store backed by the
/// <see cref="ContentDbContext"/> (<c>reporter-content.db</c>).
/// </summary>
public class ItemContentRepository : IItemContentStore
{
    private readonly IDbContextFactory<ContentDbContext> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemContentRepository"/> class.
    /// </summary>
    /// <param name="factory">The content database context factory.</param>
    public ItemContentRepository(IDbContextFactory<ContentDbContext> factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        return await context.ItemContents
            .AsNoTracking()
            .Where(c => c.ItemId == itemId)
            .Select(c => c.ContentHtml)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> GetRangeAsync(IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken = default)
    {
        if (itemIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        return await context.ItemContents
            .AsNoTracking()
            .Where(c => itemIds.Contains(c.ItemId))
            .ToDictionaryAsync(c => c.ItemId, c => c.ContentHtml!, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetAsync(Guid itemId, string? contentHtml, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var entity = await context.ItemContents.FindAsync([itemId], cancellationToken);
        if (string.IsNullOrEmpty(contentHtml))
        {
            if (entity is not null)
            {
                context.ItemContents.Remove(entity);
                await context.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        if (entity is null)
        {
            context.ItemContents.Add(new ItemContent { ItemId = itemId, ContentHtml = contentHtml });
        }
        else
        {
            entity.ContentHtml = contentHtml;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetRangeAsync(IReadOnlyList<ItemContentEntry> entries, CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0)
        {
            return;
        }

        // Doppelte ItemIds werden vorab auf das letzte Vorkommen reduziert,
        // damit nicht zwei getrackte Entitaeten mit demselben Primaerschluessel
        // entstehen ("letzter Eintrag gewinnt", wie im In-Memory-Fake).
        var deduplicated = entries
            .GroupBy(e => e.ItemId)
            .Select(g => g.Last())
            .ToList();

        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var ids = deduplicated.Select(e => e.ItemId).ToList();
        var existingById = await context.ItemContents
            .Where(c => ids.Contains(c.ItemId))
            .ToDictionaryAsync(c => c.ItemId, cancellationToken);

        foreach (var entry in deduplicated)
        {
            if (string.IsNullOrEmpty(entry.ContentHtml))
            {
                if (existingById.TryGetValue(entry.ItemId, out var toRemove))
                {
                    context.ItemContents.Remove(toRemove);
                }

                continue;
            }

            if (existingById.TryGetValue(entry.ItemId, out var entity))
            {
                entity.ContentHtml = entry.ContentHtml;
            }
            else
            {
                context.ItemContents.Add(new ItemContent { ItemId = entry.ItemId, ContentHtml = entry.ContentHtml });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var entity = await context.ItemContents.FindAsync([itemId], cancellationToken);
        if (entity is null)
        {
            return;
        }

        context.ItemContents.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteRangeAsync(IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken = default)
    {
        if (itemIds.Count == 0)
        {
            return;
        }

        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        await context.ItemContents
            .Where(c => itemIds.Contains(c.ItemId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetItemIdsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        return await context.ItemContents
            .AsNoTracking()
            .Select(c => c.ItemId)
            .ToListAsync(cancellationToken);
    }
}
