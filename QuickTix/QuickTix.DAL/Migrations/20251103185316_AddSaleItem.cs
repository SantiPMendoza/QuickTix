using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickTix.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Subscriptions_SubscriptionId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Tickets_TicketId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_SubscriptionId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TicketId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "Sales");

            migrationBuilder.AddColumn<int>(
                name: "SaleId",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SaleId",
                table: "Subscriptions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SaleItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleId = table.Column<int>(type: "int", nullable: false),
                    TicketId = table.Column<int>(type: "int", nullable: true),
                    SubscriptionId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleItems_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaleItems_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleItems_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SaleId",
                table: "Tickets",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_SaleId",
                table: "Subscriptions",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_SaleId",
                table: "SaleItems",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_SubscriptionId",
                table: "SaleItems",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_TicketId",
                table: "SaleItems",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Sales_SaleId",
                table: "Subscriptions",
                column: "SaleId",
                principalTable: "Sales",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Sales_SaleId",
                table: "Tickets",
                column: "SaleId",
                principalTable: "Sales",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Sales_SaleId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Sales_SaleId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "SaleItems");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_SaleId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_SaleId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "SaleId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SaleId",
                table: "Subscriptions");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionId",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TicketId",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SubscriptionId",
                table: "Sales",
                column: "SubscriptionId",
                unique: true,
                filter: "[SubscriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TicketId",
                table: "Sales",
                column: "TicketId",
                unique: true,
                filter: "[TicketId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Subscriptions_SubscriptionId",
                table: "Sales",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Tickets_TicketId",
                table: "Sales",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
