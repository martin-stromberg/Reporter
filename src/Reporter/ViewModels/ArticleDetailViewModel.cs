using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;

namespace Reporter.Core.ViewModels;

/// <summary>
/// View model for the article detail reading view.
/// </summary>
public partial class ArticleDetailViewModel : BaseViewModel
{
    private const double WordsPerMinute = 200.0;
    private const int DefaultAutoMarkDelaySeconds = 5;

    private static readonly Regex HtmlTagRegex = new Regex("<[^>]+>", RegexOptions.Singleline | RegexOptions.CultureInvariant);
    private static readonly Regex ScriptTagRegex = new Regex("<script[^>]*>.*?</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex IframeTagRegex = new Regex("<iframe[^>]*>.*?</iframe>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex StyleTagRegex = new Regex("<style[^>]*>.*?</style>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ObjectTagRegex = new Regex("<object[^>]*>.*?</object>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex EmbedTagRegex = new Regex("<embed[^>]*>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex FormTagRegex = new Regex("<form[^>]*>.*?</form>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex DangerousTagsRegex = new Regex("<(link|meta|base|applet|audio|video)[^>]*>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex OnEventRegex = new Regex("on\\w+\\s*=\\s*(?:\"[^\"]*\"|'[^']*'|[^\\s>]+)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex JavaScriptRegex = new Regex("javascript:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly IItemRepository _itemRepository;
    private readonly IFeedRepository _feedRepository;
    private readonly ISettingsRepository _settingsRepository;

    private Item? _item;
    private string _feedName = string.Empty;
    private string _feedIconUrl = string.Empty;
    private string _publishedAtText = string.Empty;
    private string _readingTime = string.Empty;
    private string _htmlSource = string.Empty;
    private string _autoMarkReadLabel = "Auto-Gelesen (5 s)";
    private bool _isAutoMarkRead = true;
    private bool _isAutoMarkReadAvailable = true;
    private int _fontSizeIndex;
    private int _autoMarkReadDelaySeconds = DefaultAutoMarkDelaySeconds;
    private CancellationTokenSource? _autoMarkCts;
    private readonly object _autoMarkLock = new object();

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleDetailViewModel"/> class.
    /// </summary>
    /// <param name="itemRepository">The item repository.</param>
    /// <param name="feedRepository">The feed repository.</param>
    /// <param name="settingsRepository">The settings repository.</param>
    public ArticleDetailViewModel(IItemRepository itemRepository, IFeedRepository feedRepository, ISettingsRepository settingsRepository)
    {
        _itemRepository = itemRepository;
        _feedRepository = feedRepository;
        _settingsRepository = settingsRepository;

        GoBackCommand = new AsyncRelayCommand(GoBackAsync);
        ToggleSavedForLaterCommand = new AsyncRelayCommand(ToggleSavedForLaterAsync);
        ToggleMarkReadCommand = new AsyncRelayCommand(ToggleMarkReadAsync);
        ToggleAutoMarkReadCommand = new RelayCommand(ToggleAutoMarkRead);
        OpenInBrowserCommand = new AsyncRelayCommand(OpenInBrowserAsync);
        ShareCommand = new AsyncRelayCommand(ShareAsync);
        ToggleFontSizeCommand = new AsyncRelayCommand(ToggleFontSizeAsync);
    }

    /// <summary>
    /// Gets the loaded item.
    /// </summary>
    public Item? Item
    {
        get => _item;
        private set
        {
            if (SetProperty(ref _item, value))
            {
                OnPropertyChanged(nameof(BookmarkButtonLabel));
                OnPropertyChanged(nameof(MarkAsReadButtonLabel));
            }
        }
    }

    /// <summary>
    /// Gets the title of the feed the item belongs to.
    /// </summary>
    public string FeedName
    {
        get => _feedName;
        private set => SetProperty(ref _feedName, value);
    }

    /// <summary>
    /// Gets the optional URL of the feed icon.
    /// </summary>
    public string FeedIconUrl
    {
        get => _feedIconUrl;
        private set => SetProperty(ref _feedIconUrl, value);
    }

    /// <summary>
    /// Gets the formatted publication date.
    /// </summary>
    public string PublishedAtText
    {
        get => _publishedAtText;
        private set => SetProperty(ref _publishedAtText, value);
    }

    /// <summary>
    /// Gets the estimated reading time text.
    /// </summary>
    public string ReadingTime
    {
        get => _readingTime;
        private set => SetProperty(ref _readingTime, value);
    }

    /// <summary>
    /// Gets the wrapped HTML source for the WebView.
    /// </summary>
    public string HtmlSource
    {
        get => _htmlSource;
        private set => SetProperty(ref _htmlSource, value);
    }

    /// <summary>
    /// Gets the label for the automatic mark-as-read toggle.
    /// </summary>
    public string AutoMarkReadLabel
    {
        get => _autoMarkReadLabel;
        private set => SetProperty(ref _autoMarkReadLabel, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the article is automatically marked as read.
    /// </summary>
    public bool IsAutoMarkRead
    {
        get => _isAutoMarkRead;
        set
        {
            if (SetProperty(ref _isAutoMarkRead, value))
            {
                OnAutoMarkReadChanged();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the global auto-mark-as-read setting allows
    /// the local toggle to take effect.
    /// </summary>
    public bool IsAutoMarkReadAvailable
    {
        get => _isAutoMarkReadAvailable;
        private set => SetProperty(ref _isAutoMarkReadAvailable, value);
    }

    /// <summary>
    /// Gets or sets the current font size index.
    /// </summary>
    public int FontSizeIndex
    {
        get => _fontSizeIndex;
        set
        {
            if (SetProperty(ref _fontSizeIndex, value))
            {
                OnPropertyChanged(nameof(FontSizeLabel));
                RebuildHtml();
            }
        }
    }

    /// <summary>
    /// Gets the label for the font size toggle.
    /// </summary>
    public string FontSizeLabel => _fontSizeIndex == 0 ? "A" : "A+";

    /// <summary>
    /// Gets the accessibility label for the bookmark action.
    /// </summary>
    public string BookmarkButtonLabel => Item?.IsSavedForLater == true ? "Lesezeichen entfernen" : "Lesezeichen setzen";

    /// <summary>
    /// Gets the accessibility label for the mark-as-read action.
    /// </summary>
    public string MarkAsReadButtonLabel => Item?.IsRead == true ? "Bereits gelesen" : "Als gelesen markieren";

    /// <summary>
    /// Gets the command that navigates back.
    /// </summary>
    public IAsyncRelayCommand GoBackCommand { get; }

    /// <summary>
    /// Gets the command that toggles the saved-for-later state.
    /// </summary>
    public IAsyncRelayCommand ToggleSavedForLaterCommand { get; }

    /// <summary>
    /// Gets the command that marks the article as read.
    /// </summary>
    public IAsyncRelayCommand ToggleMarkReadCommand { get; }

    /// <summary>
    /// Gets the command that toggles the auto-mark-read feature.
    /// </summary>
    public IRelayCommand ToggleAutoMarkReadCommand { get; }

    /// <summary>
    /// Gets the command that opens the article in the external browser.
    /// </summary>
    public IAsyncRelayCommand OpenInBrowserCommand { get; }

    /// <summary>
    /// Gets the command that shares the article.
    /// </summary>
    public IAsyncRelayCommand ShareCommand { get; }

    /// <summary>
    /// Gets the command that toggles the article font size between A and A+.
    /// </summary>
    public IAsyncRelayCommand ToggleFontSizeCommand { get; }

    /// <summary>
    /// Loads the article details and starts the read timer if enabled.
    /// </summary>
    /// <param name="itemId">The article identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task LoadAsync(Guid itemId)
    {
        try
        {
            Settings settings;
            try
            {
                settings = await _settingsRepository.GetAsync();
            }
            catch (Exception settingsEx)
            {
                Debug.WriteLine($"Failed to load settings: {settingsEx}");
                settings = new Settings
                {
                    Id = Settings.DefaultId,
                    RetentionDays = 30,
                    AutoMarkReadMode = SettingsValues.AutoMarkReadOnOpen,
                    AutoMarkReadDelaySeconds = DefaultAutoMarkDelaySeconds,
                    NotificationsEnabled = true,
                    AutoRefreshEnabled = true,
                    RefreshIntervalMinutes = 30,
                };
            }

            IsAutoMarkReadAvailable = SettingsValues.IsAutoMarkReadEnabled(settings.AutoMarkReadMode);
            _autoMarkReadDelaySeconds = settings.AutoMarkReadDelaySeconds >= 0
                ? settings.AutoMarkReadDelaySeconds
                : DefaultAutoMarkDelaySeconds;
            AutoMarkReadLabel = IsAutoMarkReadAvailable
                ? $"Auto-Gelesen ({_autoMarkReadDelaySeconds} s)"
                : AppResources.ArticleAutoMarkReadDisabled;

            var item = await _itemRepository.GetByIdAsync(itemId);
            if (item is null)
            {
                await GoBackAsync();
                return;
            }

            var feed = await _feedRepository.GetByIdAsync(item.FeedId);

            Item = item;
            FeedName = feed?.Title ?? string.Empty;
            FeedIconUrl = string.Empty;
            PublishedAtText = item.PublishedAt?.ToString("g", CultureInfo.CurrentCulture) ?? "—";
            ReadingTime = CalculateReadingTime(item.ContentHtml);
            RebuildHtml();

            if (IsAutoMarkRead && IsAutoMarkReadAvailable && !Item.IsRead)
            {
                _ = MarkReadDelayedAsync(TimeSpan.FromSeconds(_autoMarkReadDelaySeconds));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load article: {ex}");
        }
    }

    /// <summary>
    /// Cancels any pending automatic mark-as-read timer and releases its resources.
    /// </summary>
    public void CancelAutoMarkRead()
    {
        lock (_autoMarkLock)
        {
            _autoMarkCts?.Cancel();
            _autoMarkCts = null;
        }
    }

    private static string CalculateReadingTime(string? contentHtml)
    {
        if (string.IsNullOrWhiteSpace(contentHtml))
        {
            return string.Empty;
        }

        var text = HtmlTagRegex.Replace(contentHtml, string.Empty);
        var wordCount = text.Split(new[] { ' ', '\t', '\n', '\r', '\u00A0' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var minutes = Math.Max(1, (int)Math.Round(wordCount / WordsPerMinute));
        return $"{minutes} Min. Lesezeit";
    }

    /// <summary>
    /// Performs a best-effort regex-based HTML sanitization.
    /// This is not a bullet-proof replacement for a dedicated HTML sanitizer library.
    /// </summary>
    /// <param name="html">The raw HTML to sanitize.</param>
    /// <returns>The sanitized HTML or <c>null</c> if the input is <c>null</c>.</returns>
    private string? SanitizeHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return html;
        }

        html = ScriptTagRegex.Replace(html, string.Empty);
        html = IframeTagRegex.Replace(html, string.Empty);
        html = StyleTagRegex.Replace(html, string.Empty);
        html = ObjectTagRegex.Replace(html, string.Empty);
        html = EmbedTagRegex.Replace(html, string.Empty);
        html = FormTagRegex.Replace(html, string.Empty);
        html = DangerousTagsRegex.Replace(html, string.Empty);
        html = OnEventRegex.Replace(html, string.Empty);
        html = JavaScriptRegex.Replace(html, string.Empty);
        return html;
    }

    private void RebuildHtml()
    {
        var content = SanitizeHtml(Item?.ContentHtml);
        if (string.IsNullOrWhiteSpace(content))
        {
            HtmlSource = string.Empty;
            return;
        }

        var baseSize = _fontSizeIndex == 0 ? 19 : 22;
        var html = $@"<!DOCTYPE html>
<html>
<head>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<meta name='color-scheme' content='light dark'>
<meta http-equiv='Content-Security-Policy' content=""default-src 'none'; style-src 'unsafe-inline'; img-src * data: blob:; font-src 'none'; script-src 'none';"" >
<style>
:root {{ color-scheme: light dark; }}
body {{
    margin: 16px;
    font-family: 'Newsreader', Georgia, 'Times New Roman', serif;
    font-size: {baseSize}px;
    line-height: 1.6;
    color: #0f172a;
    background-color: #ffffff;
}}
@media (prefers-color-scheme: dark) {{
    body {{
        color: #f1f5f9;
        background-color: #0f131c;
    }}
    a {{ color: #4edea3; }}
}}
h1, h2, h3, h4, h5, h6 {{
    font-family: 'Newsreader', Georgia, serif;
    line-height: 1.25;
}}
p {{ margin: 0 0 1em 0; }}
img {{ max-width: 100%; height: auto; border-radius: 8px; }}
a {{ color: #0051d5; }}
blockquote {{
    border-left: 4px solid currentColor;
    padding-left: 16px;
    margin: 1em 0;
    opacity: 0.9;
}}
</style>
</head>
<body>{content}</body>
</html>";

        HtmlSource = html;
    }

    private void OnAutoMarkReadChanged()
    {
        CancelAutoMarkRead();

        if (IsAutoMarkRead && IsAutoMarkReadAvailable && Item is not null && !Item.IsRead)
        {
            _ = MarkReadDelayedAsync(TimeSpan.FromSeconds(_autoMarkReadDelaySeconds));
        }
    }

    private void ToggleAutoMarkRead()
    {
        IsAutoMarkRead = !IsAutoMarkRead;
    }

    private async Task MarkReadDelayedAsync(TimeSpan delay)
    {
        var cts = new CancellationTokenSource();

        lock (_autoMarkLock)
        {
            _autoMarkCts?.Cancel();
            _autoMarkCts = cts;
        }

        try
        {
            await Task.Delay(delay, cts.Token);

            if (cts.IsCancellationRequested || Item is null || Item.IsRead)
            {
                return;
            }

            await _itemRepository.MarkAsReadAsync(Item.Id);
            Item = CreateItemCopy(isRead: true, isSavedForLater: Item.IsSavedForLater, readAt: DateTime.UtcNow);
        }
        catch (OperationCanceledException)
        {
            // Expected when the user cancels auto-mark or navigates away.
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MarkReadDelayedAsync failed: {ex}");
        }
        finally
        {
            lock (_autoMarkLock)
            {
                if (_autoMarkCts == cts)
                {
                    _autoMarkCts = null;
                }
            }

            cts.Dispose();
        }
    }

    private async Task ToggleSavedForLaterAsync()
    {
        if (Item is null)
        {
            return;
        }

        await _itemRepository.ToggleSavedForLaterAsync(Item.Id);
        Item = CreateItemCopy(isRead: Item.IsRead, isSavedForLater: !Item.IsSavedForLater);
    }

    private async Task ToggleMarkReadAsync()
    {
        if (Item is null || Item.IsRead)
        {
            return;
        }

        await _itemRepository.MarkAsReadAsync(Item.Id);
        CancelAutoMarkRead();
        Item = CreateItemCopy(isRead: true, isSavedForLater: Item.IsSavedForLater, readAt: DateTime.UtcNow);
    }

    private async Task OpenInBrowserAsync()
    {
        if (Item?.Link is null)
        {
            return;
        }

        await Browser.OpenAsync(Item.Link, BrowserLaunchMode.SystemPreferred);
    }

    private async Task ShareAsync()
    {
        if (Item?.Link is null)
        {
            return;
        }

        await Share.RequestAsync(new ShareTextRequest
        {
            Title = Item.Title,
            Uri = Item.Link,
        });
    }

    private Task ToggleFontSizeAsync()
    {
        FontSizeIndex = _fontSizeIndex == 0 ? 1 : 0;
        return Task.CompletedTask;
    }

    private async Task GoBackAsync()
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    private Item CreateItemCopy(bool isRead, bool isSavedForLater, DateTime? readAt = null)
    {
        if (Item is null)
        {
            throw new InvalidOperationException("No item loaded.");
        }

        return new Item
        {
            Id = Item.Id,
            FeedId = Item.FeedId,
            Title = Item.Title,
            GuidOrHash = Item.GuidOrHash,
            IsRead = isRead,
            IsSavedForLater = isSavedForLater,
            Link = Item.Link,
            PublishedAt = Item.PublishedAt,
            ContentHtml = Item.ContentHtml,
            ReadAt = readAt,
        };
    }
}
