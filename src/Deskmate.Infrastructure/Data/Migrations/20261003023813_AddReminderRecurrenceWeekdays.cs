using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Deskmate.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderRecurrenceWeekdays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecurrenceWeekdays",
                table: "Reminders",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecurrenceWeekdays",
                table: "Reminders");
        }
    }
}
