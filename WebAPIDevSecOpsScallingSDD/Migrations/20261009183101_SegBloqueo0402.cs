using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPIDevSecOpsScallingSDD.Migrations
{
    /// <inheritdoc />
    public partial class SegBloqueo0402 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SegBloqueo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strNombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    intIntentosFallidos = table.Column<int>(type: "int", nullable: false),
                    dteBloqueoHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SegBloqueo", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SegBloqueo_strNombre",
                table: "SegBloqueo",
                column: "strNombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SegBloqueo");
        }
    }
}
