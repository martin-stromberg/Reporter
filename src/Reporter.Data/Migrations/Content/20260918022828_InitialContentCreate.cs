// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporter.Data.Migrations.Content
{
    /// <inheritdoc />
    public partial class InitialContentCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_contents",
                columns: table => new
                {
                    item_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    content_html = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_contents", x => x.item_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_contents");
        }
    }
}
