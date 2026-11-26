using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderBusinessName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessName",
                table: "AspNetUsers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessName",
                table: "AspNetUsers");
        }
    }
}
