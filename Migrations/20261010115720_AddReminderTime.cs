using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace keeptrack.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReminderTime",
                table: "Tasks",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderTime",
                table: "Tasks");
        }
    }
}
