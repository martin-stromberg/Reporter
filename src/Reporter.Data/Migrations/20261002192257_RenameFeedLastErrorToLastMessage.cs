// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporter.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameFeedLastErrorToLastMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "last_error_message",
                table: "feeds",
                newName: "last_message");

            migrationBuilder.RenameColumn(
                name: "last_error_kind",
                table: "feeds",
                newName: "last_message_kind");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "last_message_kind",
                table: "feeds",
                newName: "last_error_kind");

            migrationBuilder.RenameColumn(
                name: "last_message",
                table: "feeds",
                newName: "last_error_message");
        }
    }
}
