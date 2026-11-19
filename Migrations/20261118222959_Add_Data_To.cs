using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaProject.Migrations
{
    /// <inheritdoc />
    public partial class Add_Data_To : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketOrders_StripeCheckoutSessionId",
                table: "TicketOrders");

            migrationBuilder.CreateIndex(
                name: "IX_TicketOrders_StripeCheckoutSessionId",
                table: "TicketOrders",
                column: "StripeCheckoutSessionId",
                unique: true,
                filter: "[StripeCheckoutSessionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketOrders_StripeCheckoutSessionId",
                table: "TicketOrders");

            migrationBuilder.CreateIndex(
                name: "IX_TicketOrders_StripeCheckoutSessionId",
                table: "TicketOrders",
                column: "StripeCheckoutSessionId",
                unique: true);
        }
    }
}
