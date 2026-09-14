// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDebugLogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "debug_log_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    level = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    message = table.Column<string>(type: "TEXT", nullable: true),
                    details = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_debug_log_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_debug_log_entries_timestamp",
                table: "debug_log_entries",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "debug_log_entries");
        }
    }
}
