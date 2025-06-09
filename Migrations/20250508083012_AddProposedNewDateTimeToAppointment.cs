using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitasEPS.Migrations
{
    /// <inheritdoc />
    public partial class AddProposedNewDateTimeToAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProposedNewDateTime",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProposedNewDateTime",
                table: "Appointments");
        }
    }
}



