using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace keeptrack.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderInterval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReminderInterval",
                table: "Tasks",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderInterval",
                table: "Tasks");
        }
    }
}
