namespace UnitTest.Common
{
    public class TestDataFactoryTests
    {
        [Fact]
        public void SeedConstantsMatchDatabaseSeeder()
        {
            Assert.Equal(1, TestDataFactory.SeedClienteId);
            Assert.Equal(1, TestDataFactory.SeedProductoId);
            Assert.Equal(1, TestDataFactory.SeedUsuarioId);
            Assert.Equal(2, TestDataFactory.PerfUsuarioId);
            Assert.Equal(1, TestDataFactory.SeedVentaId);
            Assert.Equal(1, TestDataFactory.RaceStockExistencia);
            Assert.Equal("11111111-1111-1111-1111-111111111111", TestDataFactory.SeedPedidoId.ToString());
        }

        [Fact]
        public void BuildersProduceDeterministicSeedEntities()
        {
            var first = TestDataFactory.CreateCliente();
            var second = TestDataFactory.CreateCliente();

            Assert.Equal(first.id, second.id);
            Assert.Equal(first.strCorreoElectronico, second.strCorreoElectronico);
            Assert.NotSame(first, second);

            var producto = TestDataFactory.CreateProducto();
            Assert.Equal(TestDataFactory.SeedProductoId, producto.id);
            Assert.Equal(TestDataFactory.RaceStockExistencia, producto.intNumeroExistencia);

            var pedido = TestDataFactory.CreatePedido();
            Assert.Equal(TestDataFactory.SeedPedidoId, pedido.id);
            Assert.Equal("Registrado", pedido.strEstadoSaga);

            var perf = TestDataFactory.CreateUsuario(TestDataFactory.PerfUsuarioId, TestDataFactory.PerfUsuarioNombre);
            Assert.Equal("perf@test.local", perf.strCorreoElectronico);
        }
    }
}
