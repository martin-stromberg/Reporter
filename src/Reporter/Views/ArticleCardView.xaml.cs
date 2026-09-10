using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Reporter.Core.Models;

namespace Reporter.Views;

/// <summary>
/// A reusable article card that can be used across the unread and saved lists.
/// </summary>
public partial class ArticleCardView : ContentView
{
    /// <summary>
    /// Identifies the <see cref="OpenArticleCommand"/> bindable property.
    /// </summary>
    /// <returns>The bindable property.</returns>
    public static readonly BindableProperty OpenArticleCommandProperty = BindableProperty.Create(
        nameof(OpenArticleCommand),
        typeof(ICommand),
        typeof(ArticleCardView),
        new AsyncRelayCommand<ItemListItem?>(OpenArticleAsync));

    /// <summary>
    /// Identifies the <see cref="ToggleSavedCommand"/> bindable property.
    /// </summary>
    /// <returns>The bindable property.</returns>
    public static readonly BindableProperty ToggleSavedCommandProperty = BindableProperty.Create(
        nameof(ToggleSavedCommand),
        typeof(ICommand),
        typeof(ArticleCardView));

    /// <summary>
    /// Identifies the <see cref="MarkReadCommand"/> bindable property.
    /// </summary>
    /// <returns>The bindable property.</returns>
    public static readonly BindableProperty MarkReadCommandProperty = BindableProperty.Create(
        nameof(MarkReadCommand),
        typeof(ICommand),
        typeof(ArticleCardView));

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleCardView"/> class.
    /// </summary>
    public ArticleCardView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the command that opens the article detail page.
    /// </summary>
    public ICommand? OpenArticleCommand
    {
        get => (ICommand?)GetValue(OpenArticleCommandProperty);
        set => SetValue(OpenArticleCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command that toggles the saved-for-later state.
    /// </summary>
    public ICommand? ToggleSavedCommand
    {
        get => (ICommand?)GetValue(ToggleSavedCommandProperty);
        set => SetValue(ToggleSavedCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command that marks the article as read.
    /// </summary>
    public ICommand? MarkReadCommand
    {
        get => (ICommand?)GetValue(MarkReadCommandProperty);
        set => SetValue(MarkReadCommandProperty, value);
    }

    private static async Task OpenArticleAsync(ItemListItem? item)
    {
        if (item is null)
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync($"articledetail?itemId={item.Id}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OpenArticleAsync failed: {ex}");
        }
    }
}
