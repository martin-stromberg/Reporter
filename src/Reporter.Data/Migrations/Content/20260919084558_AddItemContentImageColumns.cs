// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporter.Data.Migrations.Content
{
    /// <inheritdoc />
    public partial class AddItemContentImageColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_content_type",
                table: "item_contents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "image_data",
                table: "item_contents",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "item_contents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_content_type",
                table: "item_contents");

            migrationBuilder.DropColumn(
                name: "image_data",
                table: "item_contents");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "item_contents");
        }
    }
}
