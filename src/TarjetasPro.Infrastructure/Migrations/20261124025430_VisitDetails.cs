using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TarjetasPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VisitDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "CardVisits",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "CardVisits",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Device",
                table: "CardVisits",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ip",
                table: "CardVisits",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "CardVisits",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "CardVisits",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardVisits_VisitedAt",
                table: "CardVisits",
                column: "VisitedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CardVisits_VisitedAt",
                table: "CardVisits");

            migrationBuilder.DropColumn(
                name: "City",
                table: "CardVisits");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "CardVisits");

            migrationBuilder.DropColumn(
                name: "Device",
                table: "CardVisits");

            migrationBuilder.DropColumn(
                name: "Ip",
                table: "CardVisits");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "CardVisits");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "CardVisits");
        }
    }
}
