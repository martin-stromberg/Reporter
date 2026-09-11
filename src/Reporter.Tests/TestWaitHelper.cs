namespace Reporter.Tests;

/// <summary>
/// Provides a polling helper for awaiting fire-and-forget view model persist operations in tests.
/// </summary>
public static class TestWaitHelper
{
    /// <summary>
    /// Polls the specified condition until it returns <c>true</c> or the timeout expires.
    /// </summary>
    /// <param name="condition">The condition to await.</param>
    /// <param name="timeoutMilliseconds">The timeout in milliseconds.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task WaitUntilAsync(Func<bool> condition, int timeoutMilliseconds = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(10);
        }

        Assert.True(condition());
    }

    /// <summary>
    /// Polls the specified asynchronous condition until it returns <c>true</c> or the timeout expires.
    /// </summary>
    /// <param name="condition">The asynchronous condition to await.</param>
    /// <param name="timeoutMilliseconds">The timeout in milliseconds.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task WaitUntilAsync(Func<Task<bool>> condition, int timeoutMilliseconds = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (!await condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(10);
        }

        Assert.True(await condition());
    }
}
