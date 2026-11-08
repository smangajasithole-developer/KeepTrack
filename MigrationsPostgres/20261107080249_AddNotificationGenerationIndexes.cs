using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace keeptrack.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class AddNotificationGenerationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OverdueTask",
                table: "Notifications",
                columns: new[] { "UserId", "TaskId" },
                unique: true,
                filter: "\"Type\" = 'OverdueTask' AND \"TaskId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TaskReminderOccurrence",
                table: "Notifications",
                columns: new[] { "UserId", "TaskId", "ScheduledFor" },
                unique: true,
                filter: "\"Type\" = 'TaskReminder' AND \"TaskId\" IS NOT NULL AND \"ScheduledFor\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_OverdueTask",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TaskReminderOccurrence",
                table: "Notifications");
        }
    }
}
