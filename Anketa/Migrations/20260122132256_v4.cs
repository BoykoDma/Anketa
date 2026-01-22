using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anketa.Migrations
{
    /// <inheritdoc />
    public partial class v4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Points",
                table: "Questions");

            migrationBuilder.AddColumn<int>(
                name: "TestId1",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_TestId1",
                table: "Questions",
                column: "TestId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Tests_TestId1",
                table: "Questions",
                column: "TestId1",
                principalTable: "Tests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Tests_TestId1",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_TestId1",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "TestId1",
                table: "Questions");

            migrationBuilder.AddColumn<decimal>(
                name: "Points",
                table: "Questions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
