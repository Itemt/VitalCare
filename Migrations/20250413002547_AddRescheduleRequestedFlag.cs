using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitasEPS.Migrations
{
    /// <inheritdoc />
    public partial class AddRescheduleRequestedFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RescheduleRequested",
                table: "Appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RescheduleRequested",
                table: "Appointments");
        }
    }
}
