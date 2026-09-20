// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKeywordFeedId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_keywords_keyword_text",
                table: "keywords");

            migrationBuilder.AddColumn<Guid>(
                name: "feed_id",
                table: "keywords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_keywords_feed_id_keyword_text",
                table: "keywords",
                columns: new[] { "feed_id", "keyword_text" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_keywords_keyword_text",
                table: "keywords",
                column: "keyword_text",
                unique: true,
                filter: "feed_id IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_keywords_feeds_feed_id",
                table: "keywords",
                column: "feed_id",
                principalTable: "feeds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_keywords_feeds_feed_id",
                table: "keywords");

            migrationBuilder.DropIndex(
                name: "IX_keywords_feed_id_keyword_text",
                table: "keywords");

            migrationBuilder.DropIndex(
                name: "IX_keywords_keyword_text",
                table: "keywords");

            migrationBuilder.DropColumn(
                name: "feed_id",
                table: "keywords");

            migrationBuilder.CreateIndex(
                name: "IX_keywords_keyword_text",
                table: "keywords",
                column: "keyword_text",
                unique: true);
        }
    }
}
