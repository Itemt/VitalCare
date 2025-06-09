using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitasEPS.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureCascadeDeleteForNotificationsAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Appointments_AppointmentId",
                table: "Notifications");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Notifications"" ALTER COLUMN ""NotificationType"" TYPE integer USING 0;
                ALTER TABLE ""Notifications"" ALTER COLUMN ""NotificationType"" SET NOT NULL;
                ALTER TABLE ""Notifications"" ALTER COLUMN ""NotificationType"" SET DEFAULT 0;
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Appointments_AppointmentId",
                table: "Notifications",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Appointments_AppointmentId",
                table: "Notifications");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Notifications"" ALTER COLUMN ""NotificationType"" TYPE character varying(50);
                ALTER TABLE ""Notifications"" ALTER COLUMN ""NotificationType"" DROP NOT NULL;
                ALTER TABLE ""Notifications"" ALTER COLUMN ""NotificationType"" DROP DEFAULT;
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Appointments_AppointmentId",
                table: "Notifications",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");
        }
    }
}



