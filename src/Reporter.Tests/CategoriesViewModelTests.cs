// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="CategoriesViewModel"/> class.
/// </summary>
public class CategoriesViewModelTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly CategoryRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesViewModelTests"/> class.
    /// </summary>
    public CategoriesViewModelTests()
    {
        _factory = new TestDbContextFactory();
        _repository = new CategoryRepository(_factory);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private CategoriesViewModel CreateViewModel()
    {
        return new CategoriesViewModel(_repository);
    }

    /// <summary>
    /// Verifies that the load command populates the categories with feed counts.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_PopulatesCategoriesWithFeedCounts()
    {
        await _repository.AddAsync(new Category { Id = Guid.NewGuid(), Name = "News" });

        using var context = _factory.CreateDbContext();
        context.Feeds.Add(new Data.Entities.Feed { Id = Guid.NewGuid(), Url = "https://example.com/feed", Title = "Feed", CategoryId = (await _repository.GetAllAsync()).First().Id });
        await context.SaveChangesAsync();

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Categories);
        Assert.Equal("News", viewModel.Categories[0].Name);
        Assert.Equal(1, viewModel.Categories[0].FeedCount);
    }

    /// <summary>
    /// Verifies that saving an empty category name sets a validation error and does not add a category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveCommand_WithEmptyName_SetsValidationErrorAndDoesNotAdd()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.NewCategoryName = "   ";
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Empty(viewModel.Categories);
    }

    /// <summary>
    /// Verifies that saving a duplicate name sets a validation error and does not add a category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveCommand_WithDuplicateName_SetsValidationError()
    {
        await _repository.AddAsync(new Category { Id = Guid.NewGuid(), Name = "News" });

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.NewCategoryName = "news";
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Single(viewModel.Categories);
    }

    /// <summary>
    /// Verifies that the save command adds a new category and reloads the list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveCommand_AddsNewCategory()
    {
        var viewModel = CreateViewModel();

        viewModel.NewCategoryName = "News";
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasError);
        Assert.Single(viewModel.Categories);
        Assert.Equal("News", viewModel.Categories[0].Name);
        Assert.Equal(0, viewModel.Categories[0].FeedCount);
    }

    /// <summary>
    /// Verifies that the save command updates an existing category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveCommand_UpdatesExistingCategory()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "Old" };
        await _repository.AddAsync(category);

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.EditCommand.ExecuteAsync(viewModel.Categories[0]);

        viewModel.NewCategoryName = "New";
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasError);
        Assert.Single(viewModel.Categories);
        Assert.Equal("New", viewModel.Categories[0].Name);
    }

    /// <summary>
    /// Verifies that the delete command removes a category and reloads the list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteCommand_RemovesCategoryAndReloadsList()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "ToDelete" };
        await _repository.AddAsync(category);

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.DeleteCommand.ExecuteAsync(viewModel.Categories[0]);

        Assert.Empty(viewModel.Categories);
    }
}
