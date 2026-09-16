// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Reporter.Core.Resources.Strings;
using Reporter.Core.Services;

namespace Reporter.E2ETests;

/// <summary>
/// E2E proof of the first-run demo seed: a dedicated <c>Reporter.exe</c>
/// instance is launched against a fresh temp database via
/// <c>REPORTER_DB_PATH</c> — deliberately without
/// <c>REPORTER_DISABLE_DEMO_SEED</c>, which the shared
/// <see cref="ReporterAppFixture"/> sets for the hermetic smoke tests. The
/// startup sync may hit apple.com for real; the assertions only cover the
/// seeded rows and the rendered feed card, not the sync result. Runs serially
/// inside the <c>E2E</c> collection.
/// </summary>
[Collection(E2ETestCollection.CollectionName)]
public sealed class DemoSeedTests
{
    private readonly ReporterAppFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="DemoSeedTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared app/stub-server fixture.</param>
    public DemoSeedTests(ReporterAppFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// On the very first start against a fresh database the app seeds the
    /// "News" category and the Apple Newsroom demo feed, and the feed renders
    /// as a regular card on the Feeds tab.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    [Trait("Category", "E2E")]
    public async Task FirstStart_SeedsNewsCategoryAndDemoFeed()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"reporter-e2e-demo-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        var databasePath = Path.Combine(tempDirectory, "reporter.db");

        var appPath = ReporterAppFixture.ResolveAppPath();
        var startInfo = new ProcessStartInfo(appPath) { UseShellExecute = false };
        startInfo.Environment["REPORTER_FEEDSEARCH_ENDPOINT"] = _fixture.Server.DirectoryUrl;
        startInfo.Environment["REPORTER_DB_PATH"] = databasePath;
        // The inherited process environment may carry a globally set
        // REPORTER_DISABLE_DEMO_SEED — remove it so the test premise
        // "no suppression flag" is enforced.
        startInfo.Environment.Remove("REPORTER_DISABLE_DEMO_SEED");
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{appPath}'.");

        using var automation = new UIA3Automation();
        try
        {
            Assert.True(
                await FeedDbAssertions.FeedExistsAsync(databasePath, DemoContentService.DemoFeedUrl),
                $"No feed row with URL '{DemoContentService.DemoFeedUrl}' found in {databasePath}.");
            Assert.True(
                await FeedDbAssertions.CategoryExistsAsync(databasePath, DemoContentService.DemoCategoryName),
                $"No category row named '{DemoContentService.DemoCategoryName}' found in {databasePath}.");

            using var app = Application.Attach(process);
            var window = app.GetMainWindow(automation, TimeSpan.FromMinutes(2))
                ?? throw new InvalidOperationException(
                    "Reporter.exe did not show a main window within two minutes. " +
                    "The suite requires an interactive Windows desktop session.");

            SelectFeedsTab(window);

            var card = UiRetry.WaitForCard(
                window,
                DemoContentService.DemoFeedTitle,
                TimeSpan.FromSeconds(10));
            Assert.NotNull(card);
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception)
            {
                // The process may already have exited.
            }

            process.Dispose();

            try
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
            catch (Exception)
            {
                // A leftover temp directory is tolerable; failing teardown is not.
            }
        }
    }

    // Selects the Feeds tab on the given window — the app starts on the Unread
    // page. Uses the shared tab handling of UiRetry: on the narrow window a
    // tab may only be reachable via the NavigationView overflow button.
    private static void SelectFeedsTab(Window window)
    {
        var selected = UiRetry.SelectTab(window, AppResources.TabFeeds);
        Assert.True(selected, $"Tab '{AppResources.TabFeeds}' was not found.");
    }
}
