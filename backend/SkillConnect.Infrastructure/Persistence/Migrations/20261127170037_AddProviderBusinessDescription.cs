using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderBusinessDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessDescription",
                table: "AspNetUsers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessDescription",
                table: "AspNetUsers");
        }
    }
}
