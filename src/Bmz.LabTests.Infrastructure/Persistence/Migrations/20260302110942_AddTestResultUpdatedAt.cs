using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bmz.LabTests.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTestResultUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "TestResults",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "TestResults");
        }
    }
}
