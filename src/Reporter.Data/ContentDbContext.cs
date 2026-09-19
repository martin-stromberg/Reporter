// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reporter.Data.Entities;

namespace Reporter.Data;

/// <summary>
/// The Entity Framework Core database context for the Reporter content
/// database (<c>reporter-content.db</c>), which holds the re-downloadable
/// article contents separately from the user data in <c>reporter.db</c>.
/// </summary>
public class ContentDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentDbContext"/> class with the specified options.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public ContentDbContext(DbContextOptions<ContentDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the set of <see cref="ItemContent"/> entities.
    /// </summary>
    public DbSet<ItemContent> ItemContents { get; set; } = null!;

    /// <summary>
    /// Configures the model for the SQLite content database.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureItemContent(modelBuilder.Entity<ItemContent>());
    }

    private static void ConfigureItemContent(EntityTypeBuilder<ItemContent> entity)
    {
        entity.ToTable("item_contents");
        entity.HasKey(e => e.ItemId);
        entity.Property(e => e.ItemId).HasColumnName("item_id");
        entity.Property(e => e.ContentHtml).HasColumnName("content_html");
        entity.Property(e => e.ImageData).HasColumnName("image_data");
        entity.Property(e => e.ImageContentType).HasColumnName("image_content_type");
        entity.Property(e => e.ImageUrl).HasColumnName("image_url");
    }
}
