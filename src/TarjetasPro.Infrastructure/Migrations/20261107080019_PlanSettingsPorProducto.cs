using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TarjetasPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlanSettingsPorProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Product = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Plan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Tagline = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PriceCents = table.Column<int>(type: "int", nullable: false),
                    Features = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanSettings_Product_Code",
                table: "PlanSettings",
                columns: new[] { "Product", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanSettings");
        }
    }
}
