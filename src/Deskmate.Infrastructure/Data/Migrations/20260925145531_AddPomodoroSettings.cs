using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Deskmate.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPomodoroSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "PomodoroFocus",
                table: "UserSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 25, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "PomodoroLongBreak",
                table: "UserSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 15, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "PomodoroSessionsBeforeLongBreak",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "PomodoroShortBreak",
                table: "UserSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 5, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PomodoroFocus",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "PomodoroLongBreak",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "PomodoroSessionsBeforeLongBreak",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "PomodoroShortBreak",
                table: "UserSettings");
        }
    }
}
