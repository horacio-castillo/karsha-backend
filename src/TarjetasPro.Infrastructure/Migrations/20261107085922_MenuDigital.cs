using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TarjetasPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MenuDigital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Product",
                table: "Cards",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "tarjetas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Product",
                table: "Cards");
        }
    }
}
