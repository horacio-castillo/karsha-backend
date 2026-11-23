using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TarjetasPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PromoCouponProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlanPromotions_Plan",
                table: "PlanPromotions");

            migrationBuilder.AddColumn<string>(
                name: "Product",
                table: "PlanPromotions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "tarjetas");

            migrationBuilder.AddColumn<string>(
                name: "Product",
                table: "Coupons",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanPromotions_Product_Plan",
                table: "PlanPromotions",
                columns: new[] { "Product", "Plan" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlanPromotions_Product_Plan",
                table: "PlanPromotions");

            migrationBuilder.DropColumn(
                name: "Product",
                table: "PlanPromotions");

            migrationBuilder.DropColumn(
                name: "Product",
                table: "Coupons");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPromotions_Plan",
                table: "PlanPromotions",
                column: "Plan",
                unique: true);
        }
    }
}
