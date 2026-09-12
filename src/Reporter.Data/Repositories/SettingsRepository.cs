// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using SettingsEntity = Reporter.Data.Entities.Settings;

namespace Reporter.Data.Repositories;

/// <summary>
/// Provides data access for the singleton <see cref="Settings"/> domain model.
/// </summary>
public class SettingsRepository : ISettingsRepository
{
    private readonly IDbContextFactory<ReporterDbContext> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsRepository"/> class.
    /// </summary>
    /// <param name="factory">The database context factory.</param>
    public SettingsRepository(IDbContextFactory<ReporterDbContext> factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task<Settings> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == SettingsEntity.DefaultId, cancellationToken);

        if (entity is null)
        {
            entity = new SettingsEntity();
            context.Settings.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
        }

        return MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task SaveAsync(Settings settings)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Settings.FindAsync(SettingsEntity.DefaultId);
        if (entity is null)
        {
            entity = new SettingsEntity { Id = SettingsEntity.DefaultId };
            context.Settings.Add(entity);
        }

        entity.RetentionDays = settings.RetentionDays;
        entity.AutoMarkReadMode = settings.AutoMarkReadMode;
        entity.AutoMarkReadDelaySeconds = settings.AutoMarkReadDelaySeconds;
        entity.NotificationsEnabled = settings.NotificationsEnabled;
        entity.QuietHoursStart = settings.QuietHoursStart;
        entity.QuietHoursEnd = settings.QuietHoursEnd;
        entity.AutoRefreshEnabled = settings.AutoRefreshEnabled;
        entity.RefreshIntervalMinutes = settings.RefreshIntervalMinutes;
        entity.Theme = settings.Theme;
        entity.NotificationSummaryEnabled = settings.NotificationSummaryEnabled;

        await context.SaveChangesAsync();
    }

    private static Settings MapToModel(SettingsEntity entity)
    {
        return new Settings
        {
            Id = entity.Id,
            RetentionDays = entity.RetentionDays,
            AutoMarkReadMode = entity.AutoMarkReadMode,
            AutoMarkReadDelaySeconds = entity.AutoMarkReadDelaySeconds,
            NotificationsEnabled = entity.NotificationsEnabled,
            QuietHoursStart = entity.QuietHoursStart,
            QuietHoursEnd = entity.QuietHoursEnd,
            AutoRefreshEnabled = entity.AutoRefreshEnabled,
            RefreshIntervalMinutes = entity.RefreshIntervalMinutes,
            Theme = entity.Theme,
            NotificationSummaryEnabled = entity.NotificationSummaryEnabled,
        };
    }
}
