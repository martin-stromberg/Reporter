using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporter.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "keywords",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    keyword_text = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_keywords", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    retention_days = table.Column<int>(type: "INTEGER", nullable: false),
                    auto_mark_read_mode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    auto_mark_read_delay_seconds = table.Column<int>(type: "INTEGER", nullable: false),
                    notifications_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    quiet_hours_start = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    quiet_hours_end = table.Column<TimeSpan>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feeds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    category_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_checked_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    health_status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    health_last_change = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feeds", x => x.id);
                    table.ForeignKey(
                        name: "FK_feeds_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    feed_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    link = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    published_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    guid_or_hash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    is_read = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_saved_for_later = table.Column<bool>(type: "INTEGER", nullable: false),
                    read_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    content_html = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_items_feeds_feed_id",
                        column: x => x.feed_id,
                        principalTable: "feeds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sync_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    feed_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    started_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    finished_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    message = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_sync_logs_feeds_feed_id",
                        column: x => x.feed_id,
                        principalTable: "feeds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "id", "auto_mark_read_delay_seconds", "auto_mark_read_mode", "notifications_enabled", "quiet_hours_end", "quiet_hours_start", "retention_days" },
                values: new object[] { new Guid("a1f5c6d2-4b3e-4c8f-9d2a-1b2c3d4e5f6a"), 5, "on_scroll", true, null, null, 30 });

            migrationBuilder.CreateIndex(
                name: "IX_categories_name",
                table: "categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_feeds_category_id",
                table: "feeds",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_feeds_url",
                table: "feeds",
                column: "url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_feed_id_guid_or_hash",
                table: "items",
                columns: new[] { "feed_id", "guid_or_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_keywords_keyword_text",
                table: "keywords",
                column: "keyword_text",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sync_logs_feed_id",
                table: "sync_logs",
                column: "feed_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "keywords");

            migrationBuilder.DropTable(
                name: "settings");

            migrationBuilder.DropTable(
                name: "sync_logs");

            migrationBuilder.DropTable(
                name: "feeds");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
