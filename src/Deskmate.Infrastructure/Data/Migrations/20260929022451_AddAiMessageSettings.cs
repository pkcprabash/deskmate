using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Deskmate.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiMessageSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiApiKey",
                table: "UserSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AiMessagesEnabled",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AiModel",
                table: "UserSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "claude-haiku-4-5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiApiKey",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "AiMessagesEnabled",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "AiModel",
                table: "UserSettings");
        }
    }
}
