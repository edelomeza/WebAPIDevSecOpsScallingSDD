using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPIDevSecOpsScallingSDD.Migrations
{
    /// <inheritdoc />
#pragma warning disable CA1861 // Andamio de dotnet ef: el array de columnas del índice es fijo.
    public partial class EventosProcesados0601 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VenEventoProcesado",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strNombreEvento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    idPedido = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    dteFechaProcesado = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenEventoProcesado", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VenEventoProcesado_strNombreEvento_idPedido",
                table: "VenEventoProcesado",
                columns: new[] { "strNombreEvento", "idPedido" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VenEventoProcesado");
        }
    }
}
