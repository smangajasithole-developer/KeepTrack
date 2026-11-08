using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace keeptrack.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationScheduledFor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledFor",
                table: "Notifications",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledFor",
                table: "Notifications");
        }
    }
}
