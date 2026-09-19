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
            .Where(c => itemIds.Contains(c.ItemId) && c.ContentHtml != null)
            .ToDictionaryAsync(c => c.ItemId, c => c.ContentHtml!, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ItemImage?> GetImageAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var row = await context.ItemContents
            .AsNoTracking()
            .Where(c => c.ItemId == itemId && c.ImageData != null)
            .Select(c => new { c.ImageData, c.ImageContentType, c.ImageUrl })
            .FirstOrDefaultAsync(cancellationToken);
        return row?.ImageData is null
            ? null
            : new ItemImage(row.ImageData, row.ImageContentType, row.ImageUrl);
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetImageIdsAsync(IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken = default)
    {
        if (itemIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var ids = await context.ItemContents
            .AsNoTracking()
            .Where(c => itemIds.Contains(c.ItemId) && c.ImageData != null)
            .Select(c => c.ItemId)
            .ToListAsync(cancellationToken);
        return new HashSet<Guid>(ids);
    }

    /// <inheritdoc />
    public async Task SetAsync(Guid itemId, string? contentHtml, ItemImage? image = null, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var entity = await context.ItemContents.FindAsync([itemId], cancellationToken);
        if (entity is null)
        {
            if (string.IsNullOrEmpty(contentHtml) && image is null)
            {
                return;
            }

            entity = new ItemContent { ItemId = itemId };
            context.ItemContents.Add(entity);
        }

        ApplyEntry(entity, contentHtml, image);
        if (string.IsNullOrEmpty(entity.ContentHtml) && entity.ImageData is null)
        {
            context.ItemContents.Remove(entity);
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
            if (existingById.TryGetValue(entry.ItemId, out var entity))
            {
                ApplyEntry(entity, entry.ContentHtml, entry.Image);
                if (string.IsNullOrEmpty(entity.ContentHtml) && entity.ImageData is null)
                {
                    context.ItemContents.Remove(entity);
                }

                continue;
            }

            if (string.IsNullOrEmpty(entry.ContentHtml) && entry.Image is null)
            {
                continue;
            }

            var created = new ItemContent { ItemId = entry.ItemId };
            ApplyEntry(created, entry.ContentHtml, entry.Image);
            context.ItemContents.Add(created);
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

    // Feldweiser Upsert: ein uebergebenes Bild schreibt die Bild-Spalten, ein
    // fehlendes Bild laesst sie unberuehrt; ein leerer Content entfernt den
    // gespeicherten Inhalt nur ohne Bild im selben Eintrag.
    private static void ApplyEntry(ItemContent entity, string? contentHtml, ItemImage? image)
    {
        if (image is not null)
        {
            entity.ImageData = image.Data;
            entity.ImageContentType = image.ContentType;
            entity.ImageUrl = image.Url;
            if (!string.IsNullOrEmpty(contentHtml))
            {
                entity.ContentHtml = contentHtml;
            }

            return;
        }

        entity.ContentHtml = string.IsNullOrEmpty(contentHtml) ? null : contentHtml;
    }
}
