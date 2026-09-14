// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsStartupRefreshAndSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "refresh_on_startup_enabled",
                table: "settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "unread_sort_order",
                table: "settings",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "settings",
                keyColumn: "id",
                keyValue: new Guid("a1f5c6d2-4b3e-4c8f-9d2a-1b2c3d4e5f6a"),
                columns: new[] { "refresh_on_startup_enabled", "unread_sort_order" },
                values: new object[] { true, "desc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "refresh_on_startup_enabled",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "unread_sort_order",
                table: "settings");
        }
    }
}
