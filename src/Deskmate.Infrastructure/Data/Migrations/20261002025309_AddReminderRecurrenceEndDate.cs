using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Deskmate.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderRecurrenceEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "RecurrenceEndDate",
                table: "Reminders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecurrenceEndDate",
                table: "Reminders");
        }
    }
}
