using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TarjetasPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Recibos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReceiptNumber",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceiptNumber",
                table: "Payments",
                column: "ReceiptNumber",
                unique: true,
                filter: "[ReceiptNumber] IS NOT NULL");

            // Los pagos ya cobrados también reciben su recibo, en orden de fecha.
            migrationBuilder.Sql(@"
                ;WITH n AS (SELECT Id, ROW_NUMBER() OVER (ORDER BY PaidAt, CreatedAt) AS rn FROM Payments WHERE Status = 'Paid')
                UPDATE p SET ReceiptNumber = n.rn FROM Payments p JOIN n ON n.Id = p.Id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ReceiptNumber",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReceiptNumber",
                table: "Payments");
        }
    }
}
