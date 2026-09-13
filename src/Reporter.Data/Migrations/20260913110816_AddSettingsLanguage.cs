// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "settings",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "settings",
                keyColumn: "id",
                keyValue: new Guid("a1f5c6d2-4b3e-4c8f-9d2a-1b2c3d4e5f6a"),
                column: "language",
                value: "system");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "language",
                table: "settings");
        }
    }
}
