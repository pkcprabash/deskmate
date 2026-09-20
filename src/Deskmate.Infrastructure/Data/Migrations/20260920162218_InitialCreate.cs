using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Deskmate.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", nullable: false),
                    AvatarName = table.Column<string>(type: "TEXT", nullable: false),
                    AvatarPack = table.Column<string>(type: "TEXT", nullable: false),
                    AvatarScale = table.Column<double>(type: "REAL", nullable: false),
                    PositionX = table.Column<double>(type: "REAL", nullable: true),
                    PositionY = table.Column<double>(type: "REAL", nullable: true),
                    WorkStart = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    WorkEnd = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    CurrentFocus = table.Column<string>(type: "TEXT", nullable: false),
                    Tone = table.Column<int>(type: "INTEGER", nullable: false),
                    SleepAfter = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    BreakAfter = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    StartAtLogin = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastGreetingDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    FirstRunCompleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");
        }
    }
}
