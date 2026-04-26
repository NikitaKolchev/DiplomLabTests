using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bmz.LabTests.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsRequiredToLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "Limits",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "Limits");
        }
    }
}
