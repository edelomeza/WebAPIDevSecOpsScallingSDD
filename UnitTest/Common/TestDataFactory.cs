using System;
using ClienteModel = WebAPIDevSecOpsScallingSDD.Models.CliCliente;
using ProductoModel = WebAPIDevSecOpsScallingSDD.Models.ProProducto;
using UsuarioModel = WebAPIDevSecOpsScallingSDD.Models.SegUsuario;
using PedidoModel = WebAPIDevSecOpsScallingSDD.Models.VenPedido;

namespace UnitTest.Common
{
    public static class TestDataFactory
    {
        public const int SeedClienteId = 1;
        public const int SeedProductoId = 1;
        public const int SeedUsuarioId = 1;
        public const int PerfUsuarioId = 2;
        public const int SeedVentaId = 1;
        public const int RaceStockExistencia = 1;
        public const string SeedUsuarioNombre = "seed";
        public const string PerfUsuarioNombre = "perf";

        public static readonly Guid SeedPedidoId = new("11111111-1111-1111-1111-111111111111");
        public static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static ClienteModel CreateCliente(int id = SeedClienteId)
        {
            return new ClienteModel
            {
                id = id,
                strNombreCliente = "Cliente Seed",
                strDireccionCliente = "Calle Seed 1",
                strCorreoElectronico = "cliente.seed@test.local",
                strNumeroTelefono = "5550000001",
                strCreadoPorUsuario = SeedUsuarioNombre,
            };
        }

        public static ProductoModel CreateProducto(int id = SeedProductoId, int existencia = RaceStockExistencia)
        {
            return new ProductoModel
            {
                id = id,
                strNombreProducto = "Producto Seed",
                strDescripcion = "Producto minimo de seed",
                intNumeroExistencia = existencia,
                decPrecio = 99.99m,
                strCreadoPorUsuario = SeedUsuarioNombre,
            };
        }

        public static UsuarioModel CreateUsuario(int id = SeedUsuarioId, string nombre = SeedUsuarioNombre)
        {
            return new UsuarioModel
            {
                id = id,
                strNombre = nombre,
                strPWD = "PLACEHOLDER_HASH_NOT_REAL",
                strCorreoElectronico = nombre + "@test.local",
                dteFechaRegistro = SeedDate,
                bln2FAHabilitado = false,
            };
        }

        public static PedidoModel CreatePedido(Guid? id = null, int clienteId = SeedClienteId)
        {
            return new PedidoModel
            {
                id = id ?? SeedPedidoId,
                idCliCliente = clienteId,
                dteFechaPedido = SeedDate,
                decTotal = 99.99m,
                strEstadoSaga = "Registrado",
            };
        }
    }
}
