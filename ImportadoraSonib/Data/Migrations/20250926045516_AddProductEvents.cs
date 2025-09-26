using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImportadoraSonib.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductEvents_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEvents_ProductId_EventType",
                table: "ProductEvents",
                columns: new[] { "ProductId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEvents_UserId_EventType",
                table: "ProductEvents",
                columns: new[] { "UserId", "EventType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductEvents");
        }
    }
}
