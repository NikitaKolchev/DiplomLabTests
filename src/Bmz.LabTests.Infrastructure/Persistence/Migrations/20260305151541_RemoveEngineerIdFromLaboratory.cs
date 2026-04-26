using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bmz.LabTests.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEngineerIdFromLaboratory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Laboratories_Users_EngineerId",
                table: "Laboratories");

            migrationBuilder.DropIndex(
                name: "IX_Laboratories_EngineerId",
                table: "Laboratories");

            migrationBuilder.DropColumn(
                name: "EngineerId",
                table: "Laboratories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EngineerId",
                table: "Laboratories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Laboratories_EngineerId",
                table: "Laboratories",
                column: "EngineerId",
                unique: true,
                filter: "[EngineerId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Laboratories_Users_EngineerId",
                table: "Laboratories",
                column: "EngineerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
