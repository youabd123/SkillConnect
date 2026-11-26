using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_SlotId",
                table: "Bookings");

            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWithdrawn",
                table: "AvailabilitySlots",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "Revision",
                table: "AvailabilitySlots",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SlotId",
                table: "Bookings",
                column: "SlotId",
                unique: true,
                filter: "[IsCancelled] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_SlotId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsWithdrawn",
                table: "AvailabilitySlots");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "AvailabilitySlots");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SlotId",
                table: "Bookings",
                column: "SlotId",
                unique: true);
        }
    }
}
