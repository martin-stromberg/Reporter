// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// An <see cref="ICategoryRepository"/> whose members all fail to simulate a
/// repository failure.
/// </summary>
public sealed class ThrowingCategoryRepository : ICategoryRepository
{
    private static readonly InvalidOperationException Failure = new("Simulated repository failure.");

    /// <inheritdoc />
    public Task<IReadOnlyList<Category>> GetAllAsync() => Task.FromException<IReadOnlyList<Category>>(Failure);

    /// <inheritdoc />
    public Task<IReadOnlyList<CategoryWithCount>> GetAllWithFeedCountAsync() => Task.FromException<IReadOnlyList<CategoryWithCount>>(Failure);

    /// <inheritdoc />
    public Task<Category?> GetByIdAsync(Guid id) => Task.FromException<Category?>(Failure);

    /// <inheritdoc />
    public Task AddAsync(Category category) => Task.FromException(Failure);

    /// <inheritdoc />
    public Task UpdateAsync(Category category) => Task.FromException(Failure);

    /// <inheritdoc />
    public Task DeleteAsync(Guid id) => Task.FromException(Failure);
}
