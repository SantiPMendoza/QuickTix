using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickTix.DAL.Migrations
{
    /// <inheritdoc />
    public partial class CreateVenuePriceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VenueSubscriptionPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueSubscriptionPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueSubscriptionPrices_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenueTicketPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Context = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueTicketPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueTicketPrices_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VenueSubscriptionPrices_VenueId_Category_Duration",
                table: "VenueSubscriptionPrices",
                columns: new[] { "VenueId", "Category", "Duration" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VenueTicketPrices_VenueId_Type_Context",
                table: "VenueTicketPrices",
                columns: new[] { "VenueId", "Type", "Context" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VenueSubscriptionPrices");

            migrationBuilder.DropTable(
                name: "VenueTicketPrices");
        }
    }
}
