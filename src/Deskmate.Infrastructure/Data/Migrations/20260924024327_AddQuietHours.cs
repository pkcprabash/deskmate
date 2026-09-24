using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Deskmate.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuietHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "QuietHoursEnd",
                table: "UserSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "QuietHoursStart",
                table: "UserSettings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuietHoursEnd",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "QuietHoursStart",
                table: "UserSettings");
        }
    }
}
