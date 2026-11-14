using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TarjetasPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Lienzo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanvasJson",
                table: "Cards",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanvasJson",
                table: "Cards");
        }
    }
}
