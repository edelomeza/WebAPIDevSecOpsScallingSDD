using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPIDevSecOpsScallingSDD.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CliCliente",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strNombreCliente = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    strDireccionCliente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    strCorreoElectronico = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    strNumeroTelefono = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    strCreadoPorUsuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CliCliente", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "EmpCatTipoEmpleado",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strValor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    strDescripcion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpCatTipoEmpleado", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ProProducto",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strNombreProducto = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    strURLImagen = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    strDescripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    intNumeroExistencia = table.Column<int>(type: "int", nullable: false),
                    decPrecio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    strCreadoPorUsuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProProducto", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "SegUsuario",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strNombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    strPWD = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    strCorreoElectronico = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    dteFechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: true),
                    bln2FAHabilitado = table.Column<bool>(type: "bit", nullable: false),
                    str2FASecreto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SegUsuario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "VenCatEstado",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strValor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    strDescripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenCatEstado", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "VenPedido",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    idCliCliente = table.Column<int>(type: "int", nullable: false),
                    dteFechaPedido = table.Column<DateTime>(type: "datetime2", nullable: false),
                    decTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    strEstadoSaga = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    strMotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LegacyVentaId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenPedido", x => x.id);
                    table.ForeignKey(
                        name: "FK_VenPedido_CliCliente_idCliCliente",
                        column: x => x.idCliCliente,
                        principalTable: "CliCliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmpEmpleado",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strNombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    strAPaterno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    strAMaterno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    strCURP = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true),
                    idEmpCatTipoEmpleado = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpEmpleado", x => x.id);
                    table.ForeignKey(
                        name: "FK_EmpEmpleado_EmpCatTipoEmpleado_idEmpCatTipoEmpleado",
                        column: x => x.idEmpCatTipoEmpleado,
                        principalTable: "EmpCatTipoEmpleado",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "SegRefreshToken",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSegUsuario = table.Column<int>(type: "int", nullable: false),
                    strTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    dteExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    dteCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    dteRevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    strReplacedByTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SegRefreshToken", x => x.id);
                    table.ForeignKey(
                        name: "FK_SegRefreshToken_SegUsuario_idSegUsuario",
                        column: x => x.idSegUsuario,
                        principalTable: "SegUsuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenVenta",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idCliCliente = table.Column<int>(type: "int", nullable: false),
                    idSegUsuario = table.Column<int>(type: "int", nullable: false),
                    idVenCatEstado = table.Column<int>(type: "int", nullable: false),
                    dteFechaHoraCompra = table.Column<DateTime>(type: "datetime2", nullable: true),
                    strClaveVenta = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenVenta", x => x.id);
                    table.ForeignKey(
                        name: "FK_VenVenta_CliCliente_idCliCliente",
                        column: x => x.idCliCliente,
                        principalTable: "CliCliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VenVenta_SegUsuario_idSegUsuario",
                        column: x => x.idSegUsuario,
                        principalTable: "SegUsuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VenVenta_VenCatEstado_idVenCatEstado",
                        column: x => x.idVenCatEstado,
                        principalTable: "VenCatEstado",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenPedidoDetalle",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idVenPedido = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    idProProducto = table.Column<int>(type: "int", nullable: false),
                    intCantidad = table.Column<int>(type: "int", nullable: false),
                    decPrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenPedidoDetalle", x => x.id);
                    table.ForeignKey(
                        name: "FK_VenPedidoDetalle_ProProducto_idProProducto",
                        column: x => x.idProProducto,
                        principalTable: "ProProducto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VenPedidoDetalle_VenPedido_idVenPedido",
                        column: x => x.idVenPedido,
                        principalTable: "VenPedido",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenPedidoFactura",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idVenPedido = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    strFolioFactura = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    strRFC = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    decTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    dteFechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    strEstado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenPedidoFactura", x => x.id);
                    table.ForeignKey(
                        name: "FK_VenPedidoFactura_VenPedido_idVenPedido",
                        column: x => x.idVenPedido,
                        principalTable: "VenPedido",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenPedidoPago",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idVenPedido = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    decMonto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    strMetodoPago = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    strIdTransaccion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    strEstado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    dteFechaPago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenPedidoPago", x => x.id);
                    table.ForeignKey(
                        name: "FK_VenPedidoPago_VenPedido_idVenPedido",
                        column: x => x.idVenPedido,
                        principalTable: "VenPedido",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenVentaDetalle",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idVenVenta = table.Column<int>(type: "int", nullable: false),
                    idProProducto = table.Column<int>(type: "int", nullable: false),
                    intPiezaVenta = table.Column<int>(type: "int", nullable: false),
                    decTotalVenta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenVentaDetalle", x => x.id);
                    table.ForeignKey(
                        name: "FK_VenVentaDetalle_ProProducto_idProProducto",
                        column: x => x.idProProducto,
                        principalTable: "ProProducto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VenVentaDetalle_VenVenta_idVenVenta",
                        column: x => x.idVenVenta,
                        principalTable: "VenVenta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmpEmpleado_idEmpCatTipoEmpleado",
                table: "EmpEmpleado",
                column: "idEmpCatTipoEmpleado");

            migrationBuilder.CreateIndex(
                name: "IX_SegRefreshToken_idSegUsuario",
                table: "SegRefreshToken",
                column: "idSegUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_VenPedido_idCliCliente",
                table: "VenPedido",
                column: "idCliCliente");

            migrationBuilder.CreateIndex(
                name: "IX_VenPedidoDetalle_idProProducto",
                table: "VenPedidoDetalle",
                column: "idProProducto");

            migrationBuilder.CreateIndex(
                name: "IX_VenPedidoDetalle_idVenPedido",
                table: "VenPedidoDetalle",
                column: "idVenPedido");

            migrationBuilder.CreateIndex(
                name: "IX_VenPedidoFactura_idVenPedido",
                table: "VenPedidoFactura",
                column: "idVenPedido");

            migrationBuilder.CreateIndex(
                name: "IX_VenPedidoPago_idVenPedido",
                table: "VenPedidoPago",
                column: "idVenPedido");

            migrationBuilder.CreateIndex(
                name: "IX_VenVenta_idCliCliente",
                table: "VenVenta",
                column: "idCliCliente");

            migrationBuilder.CreateIndex(
                name: "IX_VenVenta_idSegUsuario",
                table: "VenVenta",
                column: "idSegUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_VenVenta_idVenCatEstado",
                table: "VenVenta",
                column: "idVenCatEstado");

            migrationBuilder.CreateIndex(
                name: "IX_VenVentaDetalle_idProProducto",
                table: "VenVentaDetalle",
                column: "idProProducto");

            migrationBuilder.CreateIndex(
                name: "IX_VenVentaDetalle_idVenVenta",
                table: "VenVentaDetalle",
                column: "idVenVenta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmpEmpleado");

            migrationBuilder.DropTable(
                name: "SegRefreshToken");

            migrationBuilder.DropTable(
                name: "VenPedidoDetalle");

            migrationBuilder.DropTable(
                name: "VenPedidoFactura");

            migrationBuilder.DropTable(
                name: "VenPedidoPago");

            migrationBuilder.DropTable(
                name: "VenVentaDetalle");

            migrationBuilder.DropTable(
                name: "EmpCatTipoEmpleado");

            migrationBuilder.DropTable(
                name: "VenPedido");

            migrationBuilder.DropTable(
                name: "ProProducto");

            migrationBuilder.DropTable(
                name: "VenVenta");

            migrationBuilder.DropTable(
                name: "CliCliente");

            migrationBuilder.DropTable(
                name: "SegUsuario");

            migrationBuilder.DropTable(
                name: "VenCatEstado");
        }
    }
}
