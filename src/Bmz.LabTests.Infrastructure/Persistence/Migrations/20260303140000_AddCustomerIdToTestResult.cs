using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bmz.LabTests.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerIdToTestResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "TestResults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_CustomerId",
                table: "TestResults",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_Customers_CustomerId",
                table: "TestResults",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_Customers_CustomerId",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_CustomerId",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "TestResults");
        }
    }
}
