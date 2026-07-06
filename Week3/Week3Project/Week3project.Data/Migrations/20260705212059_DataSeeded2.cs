using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Week3project.Data.Migrations
{
    /// <inheritdoc />
    public partial class DataSeeded2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Cards",
                keyColumn: "Id",
                keyValue: 1,
                column: "Sku",
                value: "YGO-BPRO-EN013-SR");

            migrationBuilder.UpdateData(
                table: "Cards",
                keyColumn: "Id",
                keyValue: 2,
                column: "Sku",
                value: "YGO-SDK-EN001-UR");

            migrationBuilder.UpdateData(
                table: "Cards",
                keyColumn: "Id",
                keyValue: 3,
                column: "Sku",
                value: "YGO-GAOV-ENSP1-UR");

            migrationBuilder.UpdateData(
                table: "Cards",
                keyColumn: "Id",
                keyValue: 4,
                column: "Sku",
                value: "YGO-GAOV-EN000-SR");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Cards",
                keyColumn: "Id",
                keyValue: 1,
                column: "Sku",
                value: "BPRO-EN013-SR");

            migrationBuilder.UpdateData(
                table: "Cards",
                keyColumn: "Id",
                keyValue: 2,
                column: "Sku",
                value: "SDK-EN001-UR");

            migrationBuilder.UpdateData(
                table: "Cards",
                keyColumn: "Id",
                keyValue: 3,
                column: "Sku",
                value: "GAOV-ENSP1-UR");

            migrationBuilder.UpdateData(
                table: "Cards",
                keyColumn: "Id",
                keyValue: 4,
                column: "Sku",
                value: "GAOV-EN000-SR");
        }
    }
}
