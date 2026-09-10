using Microsoft.EntityFrameworkCore;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using CategoryEntity = Reporter.Data.Entities.Category;

namespace Reporter.Data.Repositories;

/// <summary>
/// Provides data access for <see cref="Category"/> domain models.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly IDbContextFactory<ReporterDbContext> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryRepository"/> class.
    /// </summary>
    /// <param name="factory">The database context factory.</param>
    public CategoryRepository(IDbContextFactory<ReporterDbContext> factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryWithCount>> GetAllWithFeedCountAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var query = await context.Categories
            .AsNoTracking()
            .GroupJoin(
                context.Feeds.AsNoTracking(),
                c => c.Id,
                f => f.CategoryId,
                (c, feeds) => new CategoryWithCount
                {
                    Id = c.Id,
                    Name = c.Name,
                    FeedCount = feeds.Count(),
                })
            .OrderBy(c => c.Name)
            .ToListAsync();
        return query;
    }

    /// <inheritdoc />
    public async Task<Category?> GetByIdAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task AddAsync(Category category)
    {
        await using var context = await _factory.CreateDbContextAsync();
        context.Categories.Add(MapToEntity(category));
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Category category)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Categories.FindAsync(category.Id);
        if (entity is null)
        {
            return;
        }

        entity.Name = category.Name;
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Categories.FindAsync(id);
        if (entity is null)
        {
            return;
        }

        context.Categories.Remove(entity);
        await context.SaveChangesAsync();
    }

    private static Category MapToModel(CategoryEntity entity)
    {
        return new Category
        {
            Id = entity.Id,
            Name = entity.Name,
        };
    }

    private static CategoryEntity MapToEntity(Category model)
    {
        return new CategoryEntity
        {
            Id = model.Id,
            Name = model.Name,
        };
    }
}
