using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TarjetasPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Invitaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Product",
                table: "Templates",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "tarjetas");

            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invitations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invitations_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Guests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaxPasses = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConfirmedPasses = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guests_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Templates_Product",
                table: "Templates",
                column: "Product");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_InvitationId_Status",
                table: "Guests",
                columns: new[] { "InvitationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Guests_Token",
                table: "Guests",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Slug",
                table: "Invitations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_TemplateId",
                table: "Invitations",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_UserId",
                table: "Invitations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Guests");

            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Templates_Product",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "Product",
                table: "Templates");
        }
    }
}
