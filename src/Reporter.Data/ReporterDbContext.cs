using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reporter.Data.Entities;

namespace Reporter.Data;

/// <summary>
/// The Entity Framework Core database context for the Reporter application.
/// </summary>
public class ReporterDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReporterDbContext"/> class with the specified options.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public ReporterDbContext(DbContextOptions<ReporterDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the set of <see cref="Feed"/> entities.
    /// </summary>
    public DbSet<Feed> Feeds => Set<Feed>();

    /// <summary>
    /// Gets the set of <see cref="Category"/> entities.
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>
    /// Gets the set of <see cref="Item"/> entities.
    /// </summary>
    public DbSet<Item> Items => Set<Item>();

    /// <summary>
    /// Gets the set of <see cref="Keyword"/> entities.
    /// </summary>
    public DbSet<Keyword> Keywords => Set<Keyword>();

    /// <summary>
    /// Gets the set of <see cref="Settings"/> entities.
    /// </summary>
    public DbSet<Settings> Settings => Set<Settings>();

    /// <summary>
    /// Gets the set of <see cref="SyncLog"/> entities.
    /// </summary>
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    /// <summary>
    /// Configures the model and relationships for the SQLite database.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCategory(modelBuilder.Entity<Category>());
        ConfigureFeed(modelBuilder.Entity<Feed>());
        ConfigureItem(modelBuilder.Entity<Item>());
        ConfigureKeyword(modelBuilder.Entity<Keyword>());
        ConfigureSettings(modelBuilder.Entity<Settings>());
        ConfigureSyncLog(modelBuilder.Entity<SyncLog>());
    }

    private static void ConfigureCategory(EntityTypeBuilder<Category> entity)
    {
        entity.ToTable("categories");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
        entity.HasIndex(e => e.Name).IsUnique();
    }

    private static void ConfigureFeed(EntityTypeBuilder<Feed> entity)
    {
        entity.ToTable("feeds");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.Url).HasColumnName("url").HasMaxLength(2048).IsRequired();
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        entity.Property(e => e.CategoryId).HasColumnName("category_id");
        entity.Property(e => e.LastCheckedAt).HasColumnName("last_checked_at");
        entity.Property(e => e.HealthStatus).HasColumnName("health_status").HasMaxLength(50);
        entity.Property(e => e.HealthLastChange).HasColumnName("health_last_change");

        entity.HasIndex(e => e.Url).IsUnique();
        entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureItem(EntityTypeBuilder<Item> entity)
    {
        entity.ToTable("items");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.FeedId).HasColumnName("feed_id");
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        entity.Property(e => e.Link).HasColumnName("link").HasMaxLength(2048);
        entity.Property(e => e.PublishedAt).HasColumnName("published_at");
        entity.Property(e => e.GuidOrHash).HasColumnName("guid_or_hash").HasMaxLength(500);
        entity.Property(e => e.IsRead).HasColumnName("is_read");
        entity.Property(e => e.IsSavedForLater).HasColumnName("is_saved_for_later");
        entity.Property(e => e.ReadAt).HasColumnName("read_at");
        entity.Property(e => e.ContentHtml).HasColumnName("content_html");

        entity.HasOne(e => e.Feed).WithMany().HasForeignKey(e => e.FeedId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.FeedId, e.GuidOrHash }).IsUnique();
    }

    private static void ConfigureKeyword(EntityTypeBuilder<Keyword> entity)
    {
        entity.ToTable("keywords");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.KeywordText).HasColumnName("keyword_text").HasMaxLength(500).IsRequired();
        entity.HasIndex(e => e.KeywordText).IsUnique();
    }

    private static void ConfigureSettings(EntityTypeBuilder<Settings> entity)
    {
        entity.ToTable("settings");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(e => e.RetentionDays).HasColumnName("retention_days").IsRequired();
        entity.Property(e => e.AutoMarkReadMode).HasColumnName("auto_mark_read_mode").HasMaxLength(50);
        entity.Property(e => e.AutoMarkReadDelaySeconds).HasColumnName("auto_mark_read_delay_seconds").IsRequired();
        entity.Property(e => e.NotificationsEnabled).HasColumnName("notifications_enabled").IsRequired();
        entity.Property(e => e.QuietHoursStart).HasColumnName("quiet_hours_start");
        entity.Property(e => e.QuietHoursEnd).HasColumnName("quiet_hours_end");

        entity.HasData(new Settings());
    }

    private static void ConfigureSyncLog(EntityTypeBuilder<SyncLog> entity)
    {
        entity.ToTable("sync_logs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.FeedId).HasColumnName("feed_id");
        entity.Property(e => e.StartedAt).HasColumnName("started_at");
        entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
        entity.Property(e => e.Message).HasColumnName("message");

        entity.HasOne(e => e.Feed).WithMany().HasForeignKey(e => e.FeedId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }
}
