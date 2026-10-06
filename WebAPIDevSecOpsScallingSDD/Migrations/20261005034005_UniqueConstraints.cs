using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPIDevSecOpsScallingSDD.Migrations
{
    /// <inheritdoc />
    public partial class UniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_VenPedidoPago_strIdTransaccion",
                table: "VenPedidoPago",
                column: "strIdTransaccion",
                unique: true,
                filter: "[strIdTransaccion] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VenPedidoFactura_strFolioFactura",
                table: "VenPedidoFactura",
                column: "strFolioFactura",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VenPedidoPago_strIdTransaccion",
                table: "VenPedidoPago");

            migrationBuilder.DropIndex(
                name: "IX_VenPedidoFactura_strFolioFactura",
                table: "VenPedidoFactura");
        }
    }
}
