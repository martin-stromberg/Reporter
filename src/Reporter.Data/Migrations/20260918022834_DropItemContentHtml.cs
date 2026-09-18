// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporter.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropItemContentHtml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "content_html",
                table: "items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content_html",
                table: "items",
                type: "TEXT",
                nullable: true);
        }
    }
}
