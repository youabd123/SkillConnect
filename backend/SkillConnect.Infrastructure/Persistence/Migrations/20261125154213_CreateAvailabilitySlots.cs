using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateAvailabilitySlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvailabilitySlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilitySlots", x => x.Id);
                    table.CheckConstraint("CK_AvailabilitySlots_EndsAfterStart", "[EndsAt] > [StartsAt]");
                    table.ForeignKey(
                        name: "FK_AvailabilitySlots_ServiceListings_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "ServiceListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlots_ServiceId_StartsAt",
                table: "AvailabilitySlots",
                columns: new[] { "ServiceId", "StartsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailabilitySlots");
        }
    }
}
