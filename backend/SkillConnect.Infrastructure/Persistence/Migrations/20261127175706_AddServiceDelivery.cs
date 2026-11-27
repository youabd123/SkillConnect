using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "ServiceListings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryMode",
                table: "ServiceListings",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ServiceListings_Delivery",
                table: "ServiceListings",
                sql: "([DeliveryMode] IS NULL AND [City] IS NULL) OR ([DeliveryMode] IS NOT NULL AND (([DeliveryMode] IN (1, 3) AND [City] IS NOT NULL) OR ([DeliveryMode] = 2 AND [City] IS NULL)))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ServiceListings_Delivery",
                table: "ServiceListings");

            migrationBuilder.DropColumn(
                name: "City",
                table: "ServiceListings");

            migrationBuilder.DropColumn(
                name: "DeliveryMode",
                table: "ServiceListings");
        }
    }
}
