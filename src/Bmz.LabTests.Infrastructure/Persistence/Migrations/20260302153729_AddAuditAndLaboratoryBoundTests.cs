using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bmz.LabTests.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditAndLaboratoryBoundTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LaboratoryId",
                table: "TestResults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    ActorLogin = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_LaboratoryId",
                table: "TestResults",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TimestampUtc",
                table: "AuditLogs",
                column: "TimestampUtc");

            migrationBuilder.Sql(
                """
                UPDATE tr
                SET tr.LaboratoryId = u.LaboratoryId
                FROM [TestResults] AS tr
                INNER JOIN [Users] AS u ON u.[Id] = tr.[AssistantId]
                WHERE tr.[LaboratoryId] IS NULL AND u.[LaboratoryId] IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [TestResults] WHERE [LaboratoryId] IS NULL)
                BEGIN
                    DECLARE @legacyLabId int = (SELECT TOP(1) [Id] FROM [Laboratories] ORDER BY [Id]);

                    IF @legacyLabId IS NULL
                    BEGIN
                        DECLARE @legacyLabName nvarchar(128) = N'Legacy laboratory';
                        IF EXISTS (SELECT 1 FROM [Laboratories] WHERE [Name] = @legacyLabName)
                            SET @legacyLabName = N'Legacy laboratory ' + CONVERT(nvarchar(8), ABS(CHECKSUM(NEWID())));

                        INSERT INTO [Laboratories]([Name]) VALUES (@legacyLabName);
                        SET @legacyLabId = SCOPE_IDENTITY();
                    END

                    UPDATE [TestResults]
                    SET [LaboratoryId] = @legacyLabId
                    WHERE [LaboratoryId] IS NULL;
                END
                """);

            migrationBuilder.AlterColumn<int>(
                name: "LaboratoryId",
                table: "TestResults",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_Laboratories_LaboratoryId",
                table: "TestResults",
                column: "LaboratoryId",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_Laboratories_LaboratoryId",
                table: "TestResults");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_LaboratoryId",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "LaboratoryId",
                table: "TestResults");
        }
    }
}
