using Reporter.Core.Interfaces;

namespace Reporter.Services;

/// <summary>
/// Applies the configured appearance theme by setting <see cref="Application.UserAppTheme"/>.
/// </summary>
public class AppThemeService : IAppThemeService
{
    /// <inheritdoc />
    public void ApplyTheme(string? theme)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.UserAppTheme = theme switch
        {
            "light" => AppTheme.Light,
            "dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified,
        };
    }
}
