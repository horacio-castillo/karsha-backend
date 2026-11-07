using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TarjetasPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Planes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Plan",
                table: "Payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Sencilla");

            migrationBuilder.AddColumn<string>(
                name: "GalleryJson",
                table: "Cards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Plan",
                table: "Cards",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Sencilla");

            migrationBuilder.AddColumn<string>(
                name: "ThemeJson",
                table: "Cards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Cards",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CardClicks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClickedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardClicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardClicks_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardClicks_CardId_ClickedAt",
                table: "CardClicks",
                columns: new[] { "CardId", "ClickedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardClicks");

            migrationBuilder.DropColumn(
                name: "Plan",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "GalleryJson",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "Plan",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ThemeJson",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Cards");
        }
    }
}
