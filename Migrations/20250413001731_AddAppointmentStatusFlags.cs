using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitasEPS.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentStatusFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ClinicalNotes",
                table: "Appointments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "Appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "Appointments");

            migrationBuilder.AlterColumn<string>(
                name: "ClinicalNotes",
                table: "Appointments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
